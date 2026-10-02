namespace hp55games.MareIgnoto.Rules.Cards
{
    /// <summary>
    /// Una carta fisica. L'Uid è unico in tutta la partita (tra tutti i mazzi) e identifica la carta
    /// negli eventi, nelle opzioni delle decisioni e nei test di conservazione.
    /// </summary>
    public abstract class Card
    {
        public int Uid { get; }
        public DeckKind Deck { get; }

        protected Card(int uid, DeckKind deck)
        {
            Uid = uid;
            Deck = deck;
        }
    }

    public sealed class CrewCard : Card
    {
        public CrewCardId Kind { get; }

        /// <summary>1–13 (J=11, Q=12, K=13); 0 per il Jolly. Il Mozzo (1) è anche l'asso del poker.</summary>
        public int Rank { get; }

        public CrewSuit Suit { get; }

        public bool IsJoker => Kind == CrewCardId.Jolly;

        public CrewCard(int uid, CrewCardId kind, int rank, CrewSuit suit) : base(uid, DeckKind.Crew)
        {
            Kind = kind;
            Rank = rank;
            Suit = suit;
        }

        public override string ToString() => "Crew#" + Uid + "(" + Kind + (IsJoker ? "" : "," + Suit) + ")";
    }

    public sealed class PirateCard : Card
    {
        public PirateCardId Id { get; }

        public bool IsWeather => PirateCatalog.IsWeather(Id);

        public PirateCard(int uid, PirateCardId id) : base(uid, DeckKind.Pirate)
        {
            Id = id;
        }

        public override string ToString() => "Pirate#" + Uid + "(" + Id + ")";
    }

    public sealed class MissionCard : Card
    {
        public MissionId Id { get; }

        public MissionCard(int uid, MissionId id) : base(uid, DeckKind.Corsair)
        {
            Id = id;
        }

        public override string ToString() => "Mission#" + Uid + "(" + Id + ")";
    }
}
