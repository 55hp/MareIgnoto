using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// Perdite di carte (R-021) condivise da meteo, Abbordaggio e, con la spec 0003, dalle carte Pirateria.
    /// Ogni perdita di una crew sopra coperta passa da <see cref="LoseCrew"/>, così il Medico (R-023) vale ovunque.
    /// </summary>
    internal static class Losses
    {
        /// <summary>
        /// Il giocatore perde la carta crew dello slot (R-021): va negli scarti Crew. Se la carta è sopra coperta, non è
        /// il Nostromo e c'è un Medico sotto coperta, il giocatore può usarlo (R-023): il Medico sale al posto della carta
        /// minacciata, che scende nel suo slot, e la perdita è annullata.
        /// </summary>
        public static IEnumerable<FlowStep> LoseCrew(GameContext ctx, PlayerState player, int slot, CrewLossCause cause)
        {
            CrewCard card = player.Crew[slot];
            if (card == null) yield break;

            bool above = player.Crew.IsAbove(slot);
            if (above && card.Kind != CrewCardId.Nostromo)
            {
                var medici = player.Crew.BelowSlots().Where(s => player.Crew[s] != null && player.Crew[s].Kind == CrewCardId.Medico).ToList();
                if (medici.Count > 0)
                {
                    var options = new List<DecisionOption> { new MedicoOption(-1) };
                    options.AddRange(medici.Select(s => new MedicoOption(s)));
                    AskStep ask = ctx.Ask(DecisionKind.UseMedico, player.Id, true, options, new Card[] { card });
                    yield return ask;

                    MedicoOption choice = ask.Choice<MedicoOption>();
                    if (choice.Use)
                    {
                        CrewCard medico = player.Crew.Take(choice.MedicoSlot);
                        player.Crew.Set(slot, medico);
                        player.Crew.Set(choice.MedicoSlot, card);
                        ctx.Emit(new MedicoUsedEvent(player.Id, choice.MedicoSlot, slot, medico));
                        ctx.Emit(new CrewSlotChangedEvent(player.Id, slot, true, medico));
                        ctx.Emit(new CrewSlotChangedEvent(player.Id, choice.MedicoSlot, false, card));
                        yield break;
                    }
                }
            }

            player.Crew.Take(slot);
            ctx.State.Crew.Discard(card);
            bool atSea = ctx.State.Map.KindAt(player.Position) == CellKind.Sea;
            ctx.Emit(new CrewLostEvent(player.Id, slot, above, cause, atSea, card));
            ctx.Emit(new CrewSlotChangedEvent(player.Id, slot, above, null));
        }

        /// <summary>Le carte lasciano la mano e vanno negli scarti Pirateria.</summary>
        public static void LosePirateCards(GameContext ctx, PlayerState player, IReadOnlyList<PirateCard> cards)
        {
            if (cards.Count == 0) return;
            foreach (PirateCard card in cards)
            {
                player.Hand.Remove(card);
                ctx.State.Pirate.Discard(card);
            }

            ctx.Emit(new CardsDiscardedEvent(player.Id, DeckKind.Pirate, cards.Cast<Card>().ToList()));
        }

        /// <summary>
        /// I modi distinti di scegliere <paramref name="count"/> carte dalla mano: carte con lo stesso id sono equivalenti,
        /// quindi si offre una sola combinazione per ogni multinsieme di id (con carte concrete della mano).
        /// </summary>
        public static List<IReadOnlyList<PirateCard>> DistinctHandChoices(IReadOnlyList<PirateCard> hand, int count)
        {
            var result = new List<IReadOnlyList<PirateCard>>();
            var byId = hand.GroupBy(c => c.Id).OrderBy(g => g.Key).Select(g => g.ToList()).ToList();
            Collect(byId, 0, count, new List<PirateCard>(), result);
            return result;
        }

        private static void Collect(List<List<PirateCard>> byId, int group, int left, List<PirateCard> current,
            List<IReadOnlyList<PirateCard>> result)
        {
            if (left == 0)
            {
                result.Add(current.ToArray());
                return;
            }

            if (group == byId.Count) return;

            List<PirateCard> cards = byId[group];
            for (int take = System.Math.Min(left, cards.Count); take >= 0; take--)
            {
                for (int i = 0; i < take; i++) current.Add(cards[i]);
                Collect(byId, group + 1, left - take, current, result);
                current.RemoveRange(current.Count - take, take);
            }
        }

        /// <summary>Gli slot occupati, sopra e sotto coperta, in ordine.</summary>
        public static IEnumerable<int> OccupiedSlots(PlayerState player)
        {
            for (int slot = 0; slot < player.Crew.Count; slot++)
                if (player.Crew[slot] != null) yield return slot;
        }
    }
}
