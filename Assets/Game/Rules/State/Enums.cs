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

    /// <summary>Effetto meteo di un livello di zona (R-081): 0 Normale, 1 Mare Mosso, da 2 in su Tempesta.</summary>
    public enum WeatherState
    {
        Normal,
        RoughSea,
        Storm,
    }
}
