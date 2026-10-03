using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>Ricevere carte crew (R-020) e tenere missioni pescate (R-034, R-093): flussi condivisi.</summary>
    internal static class CrewFlow
    {
        /// <summary>
        /// R-020: la carta (già in transito presso il giocatore) va in uno slot vuoto a sua scelta; senza slot vuoti
        /// sostituisce una carta presente (che va negli scarti: è una perdita, R-021) oppure si scarta.
        /// Un Nostromo che è già stato sopra coperta può andare solo sopra (R-016).
        /// </summary>
        public static IEnumerable<FlowStep> Receive(GameContext ctx, PlayerState player, CrewCard card)
        {
            var allowed = Enumerable.Range(0, player.Crew.Count)
                .Where(slot => player.Crew.IsAbove(slot) || ctx.CanGoBelow(card)).ToList();
            var options = new List<DecisionOption>();
            foreach (int slot in allowed)
                if (player.Crew[slot] == null) options.Add(new ReceiveCrewOption(ReceiveOutcome.Placed, slot));
            if (options.Count == 0)
            {
                foreach (int slot in allowed)
                    options.Add(new ReceiveCrewOption(ReceiveOutcome.Replaced, slot));
                options.Add(new ReceiveCrewOption(ReceiveOutcome.Discarded, -1));
            }

            AskStep ask = ctx.Ask(DecisionKind.ReceiveCrew, player.Id, true, options, new Card[] { card });
            yield return ask;
            ReceiveCrewOption choice = ask.Choice<ReceiveCrewOption>();

            player.InTransit.Remove(card);
            if (choice.Outcome == ReceiveOutcome.Discarded)
            {
                ctx.State.Crew.Discard(card);
                ctx.Emit(new CrewReceivedEvent(player.Id, ReceiveOutcome.Discarded, -1, false, card));
                yield break;
            }

            bool above = player.Crew.IsAbove(choice.Slot);
            if (choice.Outcome == ReceiveOutcome.Replaced)
            {
                CrewCard old = player.Crew.Take(choice.Slot);
                ctx.State.Crew.Discard(old);
                bool atSea = ctx.State.Map.KindAt(player.Position) == CellKind.Sea;
                ctx.Emit(new CrewLostEvent(player.Id, choice.Slot, above, CrewLossCause.Replaced, atSea, old));
            }

            ctx.Emit(new CrewReceivedEvent(player.Id, choice.Outcome, choice.Slot, above, card));
            ctx.PutCrew(player, choice.Slot, card);
        }

        /// <summary>
        /// R-034, R-093: tra le missioni in transito il giocatore ne tiene almeno <paramref name="minKept"/> (meno se ne ha
        /// pescate meno); le altre escono dal gioco coperte (R-009, R-130).
        /// </summary>
        public static IEnumerable<FlowStep> KeepMissions(GameContext ctx, PlayerState player, int minKept)
        {
            var missions = player.InTransit.OfType<MissionCard>().ToList();
            if (missions.Count == 0) yield break;

            int min = Math.Min(minKept, missions.Count);
            var options = new List<DecisionOption>();
            for (int mask = 1; mask < (1 << missions.Count); mask++)
            {
                var kept = new List<MissionCard>();
                for (int i = 0; i < missions.Count; i++)
                    if ((mask & (1 << i)) != 0) kept.Add(missions[i]);
                if (kept.Count >= min) options.Add(new KeepMissionsOption(kept));
            }

            AskStep ask = ctx.Ask(DecisionKind.KeepMissions, player.Id, true, options, missions.Cast<Card>().ToList());
            yield return ask;
            KeepMissionsOption option = ask.Choice<KeepMissionsOption>();

            var discarded = new List<Card>();
            foreach (MissionCard card in missions)
            {
                player.InTransit.Remove(card);
                if (option.Kept.Contains(card))
                {
                    Missions.Track(ctx.State, player, card); // R-131: conta da qui
                }
                else
                {
                    ctx.State.Corsair.Discard(card);
                    discarded.Add(card);
                }
            }

            // Le scartate escono dal gioco coperte (pila degli scarti Corsaro, che non si rimescola): gli altri vedono quante.
            if (discarded.Count > 0) ctx.Emit(new CardsDiscardedEvent(player.Id, DeckKind.Corsair, discarded));
        }
    }
}
