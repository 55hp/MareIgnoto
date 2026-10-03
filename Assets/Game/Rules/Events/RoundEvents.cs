using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Events
{
    public sealed class RoundStartedEvent : GameEvent
    {
        public int Round { get; }

        public RoundStartedEvent(int round)
        {
            Round = round;
        }

        public override string Describe() => "RoundStarted " + Round;
    }

    public sealed class PhaseStartedEvent : GameEvent
    {
        public int Round { get; }
        public GamePhase Phase { get; }

        public PhaseStartedEvent(int round, GamePhase phase)
        {
            Round = round;
            Phase = phase;
        }

        public override string Describe() => "PhaseStarted r" + Round + " " + Phase;
    }

    /// <summary>L'ordine di turno è stato stabilito (R-037 per il round 1, R-050/R-051 dal round 2).</summary>
    public sealed class TurnOrderSetEvent : GameEvent
    {
        public int Round { get; }
        /// <summary>Id dei giocatori, dal primo all'ultimo.</summary>
        public IReadOnlyList<int> Order { get; }

        public TurnOrderSetEvent(int round, IReadOnlyList<int> order)
        {
            Round = round;
            Order = order;
        }

        public override string Describe() => "TurnOrderSet r" + Round + " [" + Join(Order) + "]";
    }

    public enum DiceReason
    {
        /// <summary>Spareggio dell'ordine di turno (R-037, R-051).</summary>
        TurnOrderTiebreak,

        /// <summary>Direzione iniziale del vento (R-038).</summary>
        InitialWind,

        /// <summary>Rotazione della rotta per Mare Mosso o Tempesta (R-083, R-084).</summary>
        WeatherRotation,

        /// <summary>Riposizionamento dopo un Abbordaggio fortuito (R-070–R-072).</summary>
        BoardingReposition,

        /// <summary>Saccheggio (R-090).</summary>
        Plunder,

        /// <summary>Monete vinte in battaglia (R-104).</summary>
        BattleLoot,
    }

    /// <summary>Ogni tiro di dado produce un evento con il risultato (04_motore.md §4).</summary>
    public sealed class DieRolledEvent : GameEvent
    {
        /// <summary>Chi tira; -1 se il tiro non appartiene a un giocatore.</summary>
        public int Player { get; }
        public int Value { get; }
        public DiceReason Reason { get; }

        public DieRolledEvent(int player, int value, DiceReason reason)
        {
            Player = player;
            Value = value;
            Reason = reason;
        }

        public override string Describe() => "DieRolled p" + Player + " " + Value + " " + Reason;
    }

    /// <summary>La direzione del vento dominante, all'inizio o dopo un cambio.</summary>
    public sealed class WindChangedEvent : GameEvent
    {
        public Heading Wind { get; }

        public WindChangedEvent(Heading wind)
        {
            Wind = wind;
        }

        public override string Describe() => "WindChanged " + Wind;
    }

    /// <summary>Il livello di una zona cambia per una carta Meteo (R-088) o per il setup del tutorial.</summary>
    public sealed class ZoneChangedEvent : GameEvent
    {
        public int Zone { get; }
        /// <summary>L'id della zona (05_mappa.md §3), come "N4".</summary>
        public string ZoneId { get; }
        public int FromLevel { get; }
        public int ToLevel { get; }
        /// <summary>Falso per una carta Meteo sprecata (R-088): pagata, ma il livello non cambia.</summary>
        public bool Changed => FromLevel != ToLevel;

        public ZoneChangedEvent(int zone, string zoneId, int fromLevel, int toLevel)
        {
            Zone = zone;
            ZoneId = zoneId;
            FromLevel = fromLevel;
            ToLevel = toLevel;
        }

        public override string Describe() => "ZoneChanged " + ZoneId + " " + FromLevel + "->" + ToLevel;
    }

    /// <summary>Un giocatore ha scelto la rotta (R-040). Gli altri non vedono quale (R-041).</summary>
    public sealed class HeadingChosenEvent : GameEvent
    {
        public int Player { get; }
        /// <summary>Solo nella forma privata.</summary>
        public Heading? Heading { get; }

        public HeadingChosenEvent(int player, Heading heading) : base(player, false)
        {
            Player = player;
            Heading = heading;
        }

        private HeadingChosenEvent(HeadingChosenEvent source) : base(source.Player, true)
        {
            Player = source.Player;
            Heading = null;
        }

        protected override GameEvent Redact() => new HeadingChosenEvent(this);

        public override string Describe() => "HeadingChosen p" + Player + (Heading.HasValue ? " " + Heading.Value : "");
    }

    /// <summary>Le rotte rivelate tutte insieme (R-041), una per giocatore, per id; null per chi è in Svago (R-045).</summary>
    public sealed class HeadingsRevealedEvent : GameEvent
    {
        public IReadOnlyList<Heading?> Headings { get; }

        public HeadingsRevealedEvent(IReadOnlyList<Heading?> headings)
        {
            Headings = headings;
        }

        public override string Describe() => "HeadingsRevealed [" + Join(Headings.Select(h => h.HasValue ? h.Value.ToString() : "-")) + "]";
    }
}
