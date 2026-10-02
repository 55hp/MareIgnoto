using System;

namespace hp55games.MareIgnoto.Rules.Map
{
    /// <summary>Le 8 direzioni, in ordine orario da Nord (05_mappa.md §1). L'indice segue il d8: risultato r → indice r-1 (R-072).</summary>
    public enum Heading
    {
        N = 0,
        NE = 1,
        E = 2,
        SE = 3,
        S = 4,
        SO = 5,
        O = 6,
        NO = 7,
    }

    public static class HeadingExtensions
    {
        public const int Count = 8;

        private static readonly int[] Dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly int[] Dy = { 1, 1, 0, -1, -1, -1, 0, 1 };

        public static int DeltaX(this Heading heading) => Dx[(int)heading];

        public static int DeltaY(this Heading heading) => Dy[(int)heading];

        /// <summary>Rotazione oraria di n scatti (n può essere negativo: antioraria).</summary>
        public static Heading Rotate(this Heading heading, int steps)
        {
            int index = ((int)heading + steps) % Count;
            if (index < 0) index += Count;
            return (Heading)index;
        }

        /// <summary>Direzione opposta (180°).</summary>
        public static Heading Opposite(this Heading heading) => heading.Rotate(Count / 2);

        /// <summary>Mappatura del d8 sulle direzioni: 1=N, 2=NE, ... 8=NO (R-072).</summary>
        public static Heading FromD8(int roll)
        {
            if (roll < 1 || roll > Count)
                throw new ArgumentOutOfRangeException(nameof(roll), "Il risultato del d8 è tra 1 e 8.");
            return (Heading)(roll - 1);
        }
    }
}
