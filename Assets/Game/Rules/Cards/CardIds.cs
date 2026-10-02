namespace hp55games.MareIgnoto.Rules.Cards
{
    /// <summary>I tre mazzi (R-003, R-004, R-005).</summary>
    public enum DeckKind
    {
        Crew,
        /// <summary>Mazzo Pirateria (battaglia, economia, Meteo).</summary>
        Pirate,
        /// <summary>Mazzo Corsaro (missioni).</summary>
        Corsair,
    }

    /// <summary>Semi del mazzo Crew (03_contenuti.md §1). Servono solo al poker (R-144).</summary>
    public enum CrewSuit
    {
        /// <summary>Nessun seme: il Jolly.</summary>
        None,
        Hearts,
        Diamonds,
        Clubs,
        Spades,
    }

    /// <summary>Id delle carte crew (03_contenuti.md §1), uno per rango; J, Q, K sono Falconet, Saker, Culverin.</summary>
    public enum CrewCardId
    {
        Mozzo,
        Bucaniere,
        Vedetta,
        Cuoco,
        Medico,
        Navigatore,
        Cannoniere,
        Timoniere,
        Nostromo,
        Quartiermastro,
        Falconet,
        Saker,
        Culverin,
        Jolly,
    }

    /// <summary>Id delle carte Pirateria (03_contenuti.md §2).</summary>
    public enum PirateCardId
    {
        // Battaglia ed economia
        Bordata,
        Parle,
        PescaFortunata,
        ReteAStrascico,
        Arrembaggio,
        UomoInMare,
        Spyglass,
        // Meteo
        SupplicaGartya,
        InvocazioneGartya,
        IraGartya,
        FavoreGartya,
        VentoInPoppa,
        RafficaCanaglia,
    }

    /// <summary>Id delle missioni Corsaro (03_contenuti.md §3).</summary>
    public enum MissionId
    {
        Barbanera,
        Barbarossa,
        OlandeseVolante,
        MaledizionePirata,
        NaveCorsara,
        Avido,
        Avidissimo,
        SpugnaDiMare,
        CacciatoreDiTaglie,
        Attaccabrighe,
        Bancarotta,
        DispersiInMare,
        LupoDiMare,
        Gemelli,
        NaveDAssalto,
        ParlareConIPesci,
        GambaDiLegno,
    }
}
