using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Events;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>Ordine di turno. Per ora il round 1 (R-037); R-050/R-051 (dal round 2) si aggiungono qui con la spec 0002.</summary>
    internal static class TurnOrder
    {
        /// <summary>
        /// Round 1 (R-037): per offerta decrescente. Pareggi: tiro di d8, il più alto precede; se il tiro pareggia
        /// si ritira tra i pari. Tira per primo chi ha l'id più basso, così la sequenza di tiri è deterministica.
        /// </summary>
        public static List<int> FromOffers(GameContext ctx, IReadOnlyList<int> offers)
        {
            var distinct = new SortedSet<int>(offers);
            var order = new List<int>();

            foreach (int amount in Descending(distinct))
            {
                var tied = new List<int>();
                for (int id = 0; id < offers.Count; id++)
                    if (offers[id] == amount) tied.Add(id);
                order.AddRange(BreakTies(ctx, tied));
            }

            return order;
        }

        /// <summary>Ordina per tiro decrescente un gruppo in parità, ritirando tra chi pareggia ancora.</summary>
        private static List<int> BreakTies(GameContext ctx, List<int> tied)
        {
            if (tied.Count == 1) return tied;

            var rolls = new int[tied.Count];
            for (int i = 0; i < tied.Count; i++)
                rolls[i] = ctx.RollD8(tied[i], DiceReason.TurnOrderTiebreak);

            var distinct = new SortedSet<int>(rolls);
            var order = new List<int>();
            foreach (int roll in Descending(distinct))
            {
                var sameRoll = new List<int>();
                for (int i = 0; i < tied.Count; i++)
                    if (rolls[i] == roll) sameRoll.Add(tied[i]);
                order.AddRange(BreakTies(ctx, sameRoll));
            }

            return order;
        }

        private static IEnumerable<int> Descending(SortedSet<int> values)
        {
            foreach (int value in values.Reverse()) yield return value;
        }
    }
}
