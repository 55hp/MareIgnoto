using System;
using System.Collections.Generic;

namespace hp55games.MareIgnoto.Rules.Random
{
    /// <summary>
    /// Generatore seedato (PCG32) scritto nel motore: stessa sequenza in Unity e nell'harness,
    /// a differenza di System.Random / UnityEngine.Random.
    /// </summary>
    public sealed class SeededRandom : IRandomSource
    {
        private const ulong Multiplier = 6364136223846793005UL;

        private ulong state;
        private readonly ulong increment;

        public SeededRandom(int seed)
        {
            // splitmix64 sul seed: seed vicini (1, 2, 3...) producono flussi scorrelati.
            ulong z = unchecked((ulong)(uint)seed + 0x9E3779B97F4A7C15UL);
            z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
            z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
            z ^= z >> 31;

            state = 0UL;
            increment = (z << 1) | 1UL;
            NextUInt();
            unchecked { state += z; }
            NextUInt();
        }

        private uint NextUInt()
        {
            unchecked
            {
                ulong old = state;
                state = old * Multiplier + increment;
                uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
                int rot = (int)(old >> 59);
                return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
            }
        }

        public int RollD8()
        {
            return Range(1, 9);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "L'intervallo è vuoto.");

            uint span = unchecked((uint)(maxExclusive - minInclusive));
            // Scarta i valori che introdurrebbero bias nel modulo.
            uint threshold = unchecked(0u - span) % span;
            uint r;
            do
            {
                r = NextUInt();
            } while (r < threshold);

            return unchecked(minInclusive + (int)(r % span));
        }

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
