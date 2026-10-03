using System;

namespace hp55games.MareIgnoto.Rules.Map
{
    /// <summary>Cella della griglia: x da 0 (ovest) verso est, y da 0 (sud) verso nord (05_mappa.md §1).</summary>
    public readonly struct Coord : IEquatable<Coord>
    {
        public readonly int X;
        public readonly int Y;

        public Coord(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>La cella adiacente nella direzione data.</summary>
        public Coord Step(Heading heading) => new Coord(X + heading.DeltaX(), Y + heading.DeltaY());

        /// <summary>Distanza di Chebyshev; "adiacente" = distanza 1.</summary>
        public static int Distance(Coord a, Coord b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        public bool Equals(Coord other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is Coord other && Equals(other);

        public override int GetHashCode() => unchecked(X * 397) ^ Y;

        public static bool operator ==(Coord a, Coord b) => a.Equals(b);

        public static bool operator !=(Coord a, Coord b) => !a.Equals(b);

        public override string ToString() => "(" + X + "," + Y + ")";

        /// <summary>
        /// Il nome della cella per documentazione, UI e log (05_mappa.md §1): lettera della colonna (A = 0) + riga, come
        /// "B1" o "Y12". Il motore usa gli indici; oltre la Z ripiega su "(x,y)".
        /// </summary>
        public string Name => X >= 0 && X < 26 && Y >= 0 ? (char)('A' + X) + Y.ToString() : ToString();

        /// <summary>La cella da un nome come "B1" o "M24" (05_mappa.md §1); lancia FormatException se non è valido.</summary>
        public static Coord Parse(string name)
        {
            string text = name?.Trim() ?? "";
            if (text.Length < 2 || char.ToUpperInvariant(text[0]) < 'A' || char.ToUpperInvariant(text[0]) > 'Z' ||
                !int.TryParse(text.Substring(1), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out int row))
                throw new FormatException("Nome di cella non valido: '" + name + "' (atteso lettera + riga, come B1).");
            return new Coord(char.ToUpperInvariant(text[0]) - 'A', row);
        }
    }
}
