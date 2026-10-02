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
    }
}
