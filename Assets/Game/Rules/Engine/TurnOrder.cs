using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>Ordine di turno: round 1 per offerta (R-037), dal round 2 per ciurma sopra coperta (R-050, R-051).</summary>
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

        /// <summary>
        /// Dal round 2 (R-050): somma crescente dei valori (R-014) delle carte sopra coperta. Pareggi (R-051): meno monete,
        /// poi meno carte Pirateria in mano, poi tiro di dado come R-037.
        /// </summary>
        public static List<int> FromCrew(GameContext ctx)
        {
            var keyed = ctx.State.Players
                .Select(p => new { p.Id, Key = (Sum: CrewSum(p, ctx), p.Coins, Hand: p.Hand.Count) })
                .ToList();

            var order = new List<int>();
            foreach (var group in keyed.GroupBy(k => k.Key).OrderBy(g => g.Key.Sum).ThenBy(g => g.Key.Coins).ThenBy(g => g.Key.Hand))
                order.AddRange(BreakTies(ctx, group.Select(k => k.Id).OrderBy(id => id).ToList()));
            return order;
        }

        /// <summary>R-050: somma dei valori per l'ordine turno delle carte sopra coperta (slot vuoto e Jolly = 0, R-014).</summary>
        public static int CrewSum(PlayerState player, GameContext ctx) =>
            player.CrewAbove.Sum(card => CrewEffects.TurnOrderValue(card, ctx.Config));

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
