using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Setup;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>Preparazione della partita (R-030–R-038), prima del primo round.</summary>
    internal static class SetupFlow
    {
        public static IEnumerable<FlowStep> Run(GameContext ctx)
        {
            GameState state = ctx.State;
            RulesConfig cfg = ctx.Config;

            ctx.Emit(new GameStartedEvent(state.Players.Select(p => p.Name).ToArray(), ctx.Setup.Seed));

            // R-030
            ShuffleDecks(ctx);

            // R-031: la nave del posto k parte dal k-esimo punto del preset per questo numero di giocatori.
            IReadOnlyList<Coord> spawnPoints = state.Map.SpawnPointsFor(state.PlayerCount);
            foreach (PlayerState player in state.Players)
            {
                player.Position = spawnPoints[player.Id];
                ctx.Emit(new ShipPlacedEvent(player.Id, player.Position));
            }

            // R-032, R-033, R-034, R-035: pescate e monete per tutti, nell'ordine delle regole.
            foreach (PlayerState player in state.Players)
                ctx.DrawToTransit(player, DeckKind.Crew, cfg.startingCrewCards);
            foreach (PlayerState player in state.Players)
                ctx.DrawToHand(player, cfg.startingPirateCards);
            foreach (PlayerState player in state.Players)
                ctx.DrawToTransit(player, DeckKind.Corsair, cfg.startingMissionsDrawn);
            foreach (PlayerState player in state.Players)
                ctx.ChangeCoins(player, cfg.startingCoins, CoinReason.Setup);

            // Le scelte segrete di ogni giocatore sono consecutive, così in hot-seat il dispositivo gira una volta sola.
            var offers = new int[state.PlayerCount];
            foreach (PlayerState player in state.Players)
            {
                AskStep placeCrew = AskPlaceCrew(ctx, player);
                if (placeCrew != null)
                {
                    yield return placeCrew;
                    ApplyPlaceCrew(ctx, player, placeCrew.Choice<PlaceCrewOption>());
                }

                yield return Flow.Call(CrewFlow.KeepMissions(ctx, player, cfg.startingMissionsMinKept)); // R-034

                AskStep offer = AskOffer(ctx, player);
                yield return offer;
                offers[player.Id] = offer.Choice<OfferOption>().Amount;
                ctx.Emit(new OfferMadeEvent(player.Id, offers[player.Id]));
            }

            // R-036: le offerte si rivelano insieme e le monete vanno al Tesoro.
            ctx.Emit(new OffersRevealedEvent(offers));
            int offered = 0;
            foreach (PlayerState player in state.Players)
            {
                int amount = offers[player.Id];
                if (amount == 0) continue;
                ctx.ChangeCoins(player, -amount, CoinReason.GartyaOffer);
                offered += amount;
            }

            if (offered > 0) ctx.ChangeTreasure(offered);

            // R-037
            state.TurnOrderList.AddRange(TurnOrder.FromOffers(ctx, offers));
            ctx.Emit(new TurnOrderSetEvent(1, state.TurnOrderList.ToArray()));

            // R-038: d8 → la lancetta fa r scatti in senso orario da Nord (8 = Nord). Le zone partono dal livello del layout
            // (05 §3: nuvole 0, spicchi 5), già in GameState.
            TutorialOptions tutorial = ctx.Setup.Tutorial;
            state.Wind = tutorial != null && tutorial.InitialWind.HasValue
                ? tutorial.InitialWind.Value
                : Heading.N.Rotate(ctx.RollD8(-1, DiceReason.InitialWind));
            ctx.Emit(new WindChangedEvent(state.Wind));

            if (tutorial != null && tutorial.InitialZones != null)
            {
                foreach (ZoneSetup zone in tutorial.InitialZones)
                {
                    int index = state.Map.ZoneIndex(zone.ZoneId);
                    int from = state.ZoneLevels[index];
                    state.ZoneLevels[index] = zone.Level;
                    ctx.Emit(new ZoneChangedEvent(index, zone.ZoneId, from, zone.Level));
                }
            }
        }

        // ---- R-030: mazzi ----

        private static void ShuffleDecks(GameContext ctx)
        {
            GameState state = ctx.State;
            TutorialOptions tutorial = ctx.Setup.Tutorial;

            state.Crew.Shuffle(ctx.Random);
            ctx.Emit(new DeckShuffledEvent(DeckKind.Crew, state.Crew.DrawCount, false));
            state.Pirate.Shuffle(ctx.Random);
            ctx.Emit(new DeckShuffledEvent(DeckKind.Pirate, state.Pirate.DrawCount, false));
            state.Corsair.Shuffle(ctx.Random);
            ctx.Emit(new DeckShuffledEvent(DeckKind.Corsair, state.Corsair.DrawCount, false));

            // Tutorial: dopo aver mescolato (così il resto della partita consuma la casualità come sempre)
            // si portano in cima le carte prestabilite.
            if (tutorial == null) return;
            PutOnTop(state.Crew, tutorial.CrewDeckTop, (card, spec) => spec.Matches(card));
            PutOnTop(state.Pirate, tutorial.PirateDeckTop, (card, id) => card.Id == id);
            PutOnTop(state.Corsair, tutorial.CorsairDeckTop, (card, id) => card.Id == id);
        }

        private static void PutOnTop<TCard, TSpec>(DeckState<TCard> deck, IReadOnlyList<TSpec> specs,
            Func<TCard, TSpec, bool> matches) where TCard : Card
        {
            if (specs == null || specs.Count == 0) return;

            var chosen = new List<TCard>();
            foreach (TSpec spec in specs)
            {
                TCard found = deck.DrawPile.FirstOrDefault(card => !chosen.Contains(card) && matches(card, spec));
                if (found == null)
                    throw new ArgumentException("Il mazzo non contiene abbastanza copie di " + spec + " per l'ordine prestabilito.");
                chosen.Add(found);
            }

            deck.MoveToTop(chosen);
        }

        // ---- R-032: crew iniziali sotto coperta ----

        private static AskStep AskPlaceCrew(GameContext ctx, PlayerState player)
        {
            var crew = player.InTransit.OfType<CrewCard>().ToList();
            if (crew.Count == 0) return null;

            var options = new List<DecisionOption>();
            Assign(crew, player.Crew.BelowSlots().ToList(), 0, new bool[player.Crew.BelowCount], new List<CrewPlacement>(), options);
            return ctx.Ask(DecisionKind.PlaceStartingCrew, player.Id, true, options, crew.Cast<Card>().ToList());
        }

        /// <summary>Genera tutti i modi di mettere ogni carta in uno slot diverso.</summary>
        private static void Assign(List<CrewCard> cards, List<int> slots, int index, bool[] used,
            List<CrewPlacement> current, List<DecisionOption> result)
        {
            if (index == cards.Count)
            {
                result.Add(new PlaceCrewOption(current.ToArray()));
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (used[i]) continue;
                used[i] = true;
                current.Add(new CrewPlacement(cards[index], slots[i]));
                Assign(cards, slots, index + 1, used, current, result);
                current.RemoveAt(current.Count - 1);
                used[i] = false;
            }
        }

        private static void ApplyPlaceCrew(GameContext ctx, PlayerState player, PlaceCrewOption option)
        {
            foreach (CrewPlacement placement in option.Placements)
            {
                player.InTransit.Remove(placement.Card);
                player.Crew.Set(placement.Slot, placement.Card);
                ctx.Emit(new CrewSlotChangedEvent(player.Id, placement.Slot, player.Crew.IsAbove(placement.Slot), placement.Card));
            }
        }

        // ---- R-036: offerta a Gartya ----

        private static AskStep AskOffer(GameContext ctx, PlayerState player)
        {
            var options = new List<DecisionOption>();
            for (int amount = 0; amount <= player.Coins; amount++) options.Add(new OfferOption(amount));
            return ctx.Ask(DecisionKind.GartyaOffer, player.Id, true, options);
        }
    }
}
