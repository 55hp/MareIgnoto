using System.Collections.Generic;

namespace hp55games.MareIgnoto.Rules.Random
{
    /// <summary>
    /// Unica sorgente di casualità del motore (R-008). Tutti i tiri e i mescolamenti passano da qui,
    /// così una partita è riproducibile da seed e risposte.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Tiro di d8: 1..8.</summary>
        int RollD8();

        /// <summary>Intero in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);

        void Shuffle<T>(IList<T> list);
    }
}
