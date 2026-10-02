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

            // R-031: la nave del posto N parte dal punto di partenza N.
            foreach (PlayerState player in state.Players)
            {
                player.Position = state.Map.SpawnPoints[player.Id];
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

                AskStep keepMissions = AskKeepMissions(ctx, player);
                if (keepMissions != null)
                {
                    yield return keepMissions;
                    ApplyKeepMissions(ctx, player, keepMissions.Choice<KeepMissionsOption>());
                }

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

            // R-038
            TutorialOptions tutorial = ctx.Setup.Tutorial;
            state.Wind = tutorial != null && tutorial.InitialWind.HasValue
                ? tutorial.InitialWind.Value
                : (Heading)ctx.Random.Range(0, HeadingExtensions.Count);
            ctx.Emit(new WindChangedEvent(state.Wind));

            if (tutorial != null && tutorial.InitialZones != null)
            {
                foreach (ZoneSetup zone in tutorial.InitialZones)
                {
                    state.ZoneStates[zone.Zone] = zone.State;
                    ctx.Emit(new ZoneChangedEvent(zone.Zone, zone.State));
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

        // ---- R-034: missioni ----

        private static AskStep AskKeepMissions(GameContext ctx, PlayerState player)
        {
            var missions = player.InTransit.OfType<MissionCard>().ToList();
            if (missions.Count == 0) return null;

            int minKept = Math.Min(ctx.Config.startingMissionsMinKept, missions.Count);
            var options = new List<DecisionOption>();
            for (int mask = 1; mask < (1 << missions.Count); mask++)
            {
                var kept = new List<MissionCard>();
                for (int i = 0; i < missions.Count; i++)
                    if ((mask & (1 << i)) != 0) kept.Add(missions[i]);
                if (kept.Count >= minKept) options.Add(new KeepMissionsOption(kept));
            }

            return ctx.Ask(DecisionKind.KeepMissions, player.Id, true, options, missions.Cast<Card>().ToList());
        }

        private static void ApplyKeepMissions(GameContext ctx, PlayerState player, KeepMissionsOption option)
        {
            var drawn = player.InTransit.OfType<MissionCard>().ToList();
            var discarded = new List<Card>();
            foreach (MissionCard card in drawn)
            {
                player.InTransit.Remove(card);
                if (option.Kept.Contains(card))
                {
                    player.Missions.Add(card);
                }
                else
                {
                    ctx.State.Corsair.Discard(card);
                    discarded.Add(card);
                }
            }

            // TODO R-130: 02_regole.md non dice se le missioni scartate sono visibili agli altri. Per ora restano segrete
            // (gli altri vedono solo quante); se diventano pubbliche basta passare cards pubbliche all'evento.
            if (discarded.Count > 0) ctx.Emit(new CardsDiscardedEvent(player.Id, DeckKind.Corsair, discarded));
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
