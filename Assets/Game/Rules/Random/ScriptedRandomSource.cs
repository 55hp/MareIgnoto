using System;
using System.Collections.Generic;

namespace hp55games.MareIgnoto.Rules.Random
{
    /// <summary>
    /// Per il tutorial: restituisce prima i tiri di d8 prestabiliti, poi prosegue con la sorgente di riserva.
    /// Range e Shuffle vanno sempre alla sorgente di riserva.
    /// </summary>
    public sealed class ScriptedRandomSource : IRandomSource
    {
        private readonly Queue<int> rolls;
        private readonly IRandomSource fallback;

        public ScriptedRandomSource(IEnumerable<int> d8Rolls, IRandomSource fallback)
        {
            if (d8Rolls == null) throw new ArgumentNullException(nameof(d8Rolls));
            this.fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
            rolls = new Queue<int>();
            foreach (int roll in d8Rolls)
            {
                if (roll < 1 || roll > 8)
                    throw new ArgumentOutOfRangeException(nameof(d8Rolls), "I tiri prestabiliti devono essere tra 1 e 8.");
                rolls.Enqueue(roll);
            }
        }

        /// <summary>Tiri prestabiliti non ancora consumati.</summary>
        public int RemainingScripted => rolls.Count;

        public int RollD8()
        {
            return rolls.Count > 0 ? rolls.Dequeue() : fallback.RollD8();
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return fallback.Range(minInclusive, maxExclusive);
        }

        public void Shuffle<T>(IList<T> list)
        {
            fallback.Shuffle(list);
        }
    }
}
