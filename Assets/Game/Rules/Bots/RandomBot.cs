using System;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Bots
{
    /// <summary>
    /// Il bot casuale (04_motore.md §2, 08_test-e-simulazione.md §3): sceglie un'opzione legale a caso.
    /// Usa una sorgente propria, distinta da quella della partita, così le sue scelte non consumano la casualità del gioco.
    /// Serve alla simulazione e a riempire i posti vuoti nei playtest solitari.
    /// </summary>
    public sealed class RandomBot : IBot
    {
        private readonly IRandomSource random;

        public RandomBot(IRandomSource random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public DecisionAnswer Choose(PendingDecision decision)
        {
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            return decision.Choose(random.Range(0, decision.Options.Count));
        }

        /// <summary><see cref="IBot"/>: il punto di vista non serve, la scelta è la stessa di <see cref="Choose(PendingDecision)"/>.</summary>
        public DecisionAnswer Choose(PendingDecision decision, PlayerView view) => Choose(decision);
    }
}
