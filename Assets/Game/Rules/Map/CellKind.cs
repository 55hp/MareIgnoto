namespace hp55games.MareIgnoto.Rules.Map
{
    /// <summary>Tipo di cella (05_mappa.md §2).</summary>
    public enum CellKind
    {
        /// <summary>Cornice: terra ferma e punti di partenza.</summary>
        Border,
        /// <summary>Mare navigabile, appartiene a una zona meteo.</summary>
        Sea,
        /// <summary>Isola: porto e zona franca.</summary>
        Island,
        /// <summary>Isola Sacra: entrarci fa scattare la fine partita (R-140).</summary>
        SacredIsland,
    }
}
