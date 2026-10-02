using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Events
{
    public sealed class GameStartedEvent : GameEvent
    {
        public IReadOnlyList<string> PlayerNames { get; }
        public int Seed { get; }

        public GameStartedEvent(IReadOnlyList<string> playerNames, int seed)
        {
            PlayerNames = playerNames;
            Seed = seed;
        }

        public override string Describe() => "GameStarted seed=" + Seed + " players=" + Join(PlayerNames);
    }

    /// <summary>Un mazzo è stato mescolato; <see cref="FromDiscard"/> se è il riciclo degli scarti (R-009).</summary>
    public sealed class DeckShuffledEvent : GameEvent
    {
        public DeckKind Deck { get; }
        public int CardCount { get; }
        public bool FromDiscard { get; }

        public DeckShuffledEvent(DeckKind deck, int cardCount, bool fromDiscard)
        {
            Deck = deck;
            CardCount = cardCount;
            FromDiscard = fromDiscard;
        }

        public override string Describe() =>
            "DeckShuffled " + Deck + " n=" + CardCount + (FromDiscard ? " fromDiscard" : "");
    }

    public sealed class ShipPlacedEvent : GameEvent
    {
        public int Player { get; }
        public Coord Position { get; }

        public ShipPlacedEvent(int player, Coord position)
        {
            Player = player;
            Position = position;
        }

        public override string Describe() => "ShipPlaced p" + Player + " " + Position;
    }

    /// <summary>
    /// Il giocatore ha pescato carte. Gli altri vedono solo quante (<see cref="Count"/>); le carte
    /// (<see cref="Cards"/>) le vede solo chi pesca.
    /// </summary>
    public sealed class CardsDrawnEvent : GameEvent
    {
        public int Player { get; }
        public DeckKind Deck { get; }
        public int Count { get; }
        /// <summary>Vuota nella forma pubblica.</summary>
        public IReadOnlyList<Card> Cards { get; }

        public CardsDrawnEvent(int player, DeckKind deck, IReadOnlyList<Card> cards) : base(player, false)
        {
            Player = player;
            Deck = deck;
            Count = cards.Count;
            Cards = cards;
        }

        private CardsDrawnEvent(CardsDrawnEvent source) : base(source.Player, true)
        {
            Player = source.Player;
            Deck = source.Deck;
            Count = source.Count;
            Cards = new Card[0];
        }

        protected override GameEvent Redact() => new CardsDrawnEvent(this);

        public override string Describe() =>
            "CardsDrawn p" + Player + " " + Deck + " n=" + Count + (IsRedacted ? "" : " [" + Join(Cards) + "]");
    }

    /// <summary>
    /// Carte finite negli scarti dalla mano o dalle carte in transito: missioni scartate (R-034, segrete per R-130),
    /// carte Pirateria perse per meteo o Abbordaggio (R-083, R-070, R-071). Il contenuto lo vede solo il proprietario;
    /// gli altri vedono quante.
    /// </summary>
    public sealed class CardsDiscardedEvent : GameEvent
    {
        public int Player { get; }
        public DeckKind Deck { get; }
        public int Count { get; }
        /// <summary>Vuota nella forma pubblica.</summary>
        public IReadOnlyList<Card> Cards { get; }

        public CardsDiscardedEvent(int player, DeckKind deck, IReadOnlyList<Card> cards) : base(player, false)
        {
            Player = player;
            Deck = deck;
            Count = cards.Count;
            Cards = cards;
        }

        private CardsDiscardedEvent(CardsDiscardedEvent source) : base(source.Player, true)
        {
            Player = source.Player;
            Deck = source.Deck;
            Count = source.Count;
            Cards = new Card[0];
        }

        protected override GameEvent Redact() => new CardsDiscardedEvent(this);

        public override string Describe() =>
            "CardsDiscarded p" + Player + " " + Deck + " n=" + Count + (IsRedacted ? "" : " [" + Join(Cards) + "]");
    }

    /// <summary>
    /// Una carta crew è entrata o uscita da uno slot della ciurma. Sopra coperta è pubblico; sotto coperta
    /// gli altri vedono che lo slot è occupato o vuoto, ma non quale carta c'è.
    /// </summary>
    public sealed class CrewSlotChangedEvent : GameEvent
    {
        public int Player { get; }
        public int Slot { get; }
        public bool Above { get; }
        public bool Occupied { get; }
        /// <summary>La carta ora nello slot, null se vuoto o nella forma pubblica di uno slot sotto coperta.</summary>
        public CrewCard Card { get; }

        public CrewSlotChangedEvent(int player, int slot, bool above, CrewCard card) : base(player, false, !above)
        {
            Player = player;
            Slot = slot;
            Above = above;
            Occupied = card != null;
            Card = card;
        }

        private CrewSlotChangedEvent(CrewSlotChangedEvent source) : base(source.Player, true, true)
        {
            Player = source.Player;
            Slot = source.Slot;
            Above = source.Above;
            Occupied = source.Occupied;
            Card = null;
        }

        protected override GameEvent Redact() => new CrewSlotChangedEvent(this);

        public override string Describe() =>
            "CrewSlot p" + Player + " slot=" + Slot + (Above ? " above " : " below ") +
            (IsRedacted ? (Occupied ? "occupied" : "empty") : (Card == null ? "empty" : Card.ToString()));
    }

    public enum CoinReason
    {
        /// <summary>Monete iniziali (R-035).</summary>
        Setup,
        /// <summary>Offerta a Gartya (R-036): le monete passano al Tesoro.</summary>
        GartyaOffer,
        /// <summary>Mozzo sopra coperta a inizio turno (R-052).</summary>
        CabinBoy,
        /// <summary>Saccheggio (R-090).</summary>
        Plunder,
        /// <summary>Costo di un'azione di porto (R-091, R-092): le monete escono dal gioco.</summary>
        PortCost,
        /// <summary>Commercio (R-094).</summary>
        Commerce,
        /// <summary>Pesca Fortunata! (R-122).</summary>
        FortunateCatch,
        /// <summary>Costo di una carta Meteo, pagato al Tesoro (R-121).</summary>
        WeatherCard,
        /// <summary>Monete passate dal perdente al vincitore di una battaglia (R-104).</summary>
        BattleLoot,
        /// <summary>Monete pagate per evitare Arrembaggio! (R-110).</summary>
        BoardingRansom,
    }

    public sealed class CoinsChangedEvent : GameEvent
    {
        public int Player { get; }
        public int Delta { get; }
        public int Total { get; }
        public CoinReason Reason { get; }

        public CoinsChangedEvent(int player, int delta, int total, CoinReason reason)
        {
            Player = player;
            Delta = delta;
            Total = total;
            Reason = reason;
        }

        public override string Describe() => "Coins p" + Player + " " + Delta.ToString("+0;-0;0") + " =" + Total + " " + Reason;
    }

    public sealed class TreasureChangedEvent : GameEvent
    {
        public int Delta { get; }
        public int Total { get; }

        public TreasureChangedEvent(int delta, int total)
        {
            Delta = delta;
            Total = total;
        }

        public override string Describe() => "Treasure " + Delta.ToString("+0;-0;0") + " =" + Total;
    }

    /// <summary>Un giocatore ha deciso la sua offerta a Gartya. Nella forma pubblica non si vede l'importo.</summary>
    public sealed class OfferMadeEvent : GameEvent
    {
        public int Player { get; }
        /// <summary>0 nella forma pubblica: non è un'offerta di zero, è nascosta (usare <see cref="GameEvent.IsRedacted"/>).</summary>
        public int Amount { get; }

        public OfferMadeEvent(int player, int amount) : base(player, false)
        {
            Player = player;
            Amount = amount;
        }

        private OfferMadeEvent(OfferMadeEvent source) : base(source.Player, true)
        {
            Player = source.Player;
            Amount = 0;
        }

        protected override GameEvent Redact() => new OfferMadeEvent(this);

        public override string Describe() => "OfferMade p" + Player + (IsRedacted ? "" : " amount=" + Amount);
    }

    /// <summary>Le offerte rivelate insieme (R-036), una per giocatore, per id.</summary>
    public sealed class OffersRevealedEvent : GameEvent
    {
        public IReadOnlyList<int> Amounts { get; }

        public OffersRevealedEvent(IReadOnlyList<int> amounts)
        {
            Amounts = amounts;
        }

        public override string Describe() => "OffersRevealed [" + Join(Amounts) + "]";
    }
}
