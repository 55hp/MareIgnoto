namespace hp55games.MareIgnoto.Rules.Config
{
    /// <summary>Combinazioni del punteggio poker (03_contenuti.md §4, R-144).</summary>
    public enum PokerHand
    {
        HighCard,
        Pair,
        TwoPair,
        ThreeOfAKind,
        Straight,
        Flush,
        FullHouse,
        FourOfAKind,
        StraightFlush,
        /// <summary>Scala Reale Massima: 10-J-Q-K-1 dello stesso seme.</summary>
        RoyalStraightFlush,
    }
}
