namespace hp55games.MareIgnoto.Rules.State
{
    /// <summary>Fasi della partita (02_regole.md §3, §4).</summary>
    public enum GamePhase
    {
        /// <summary>Preparazione della partita, prima del primo round (§3).</summary>
        Setup,
        /// <summary>Fase 1: rotte, meteo, movimento (simultanea, segreta).</summary>
        Preparation,
        /// <summary>Fase 2: turni attivi in ordine di turno.</summary>
        Active,
        Ended,
    }

    /// <summary>Stato meteo di una zona (R-081).</summary>
    public enum WeatherState
    {
        Normal,
        RoughSea,
        Storm,
    }
}
