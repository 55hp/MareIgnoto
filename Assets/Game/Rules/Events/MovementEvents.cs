using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Events
{
    /// <summary>Il meteo della zona è stato applicato alla nave (R-042, R-082–R-085).</summary>
    public sealed class WeatherAppliedEvent : GameEvent
    {
        public int Player { get; }
        public int Zone { get; }
        /// <summary>Lo stato della zona.</summary>
        public WeatherState ZoneState { get; }
        /// <summary>L'intensità percepita dopo il Navigatore (R-085).</summary>
        public WeatherState Perceived { get; }

        public WeatherAppliedEvent(int player, int zone, WeatherState zoneState, WeatherState perceived)
        {
            Player = player;
            Zone = zone;
            ZoneState = zoneState;
            Perceived = perceived;
        }

        public override string Describe() => "WeatherApplied p" + Player + " z" + Zone + " " + ZoneState + "->" + Perceived;
    }

    public enum WeatherSkipReason
    {
        /// <summary>Svago (R-045).</summary>
        Leisure,
        /// <summary>Vento in Poppa (R-087).</summary>
        Tailwind,
    }

    /// <summary>La nave è in una zona meteo ma non ne subisce gli effetti.</summary>
    public sealed class WeatherSkippedEvent : GameEvent
    {
        public int Player { get; }
        public WeatherSkipReason Reason { get; }

        public WeatherSkippedEvent(int player, WeatherSkipReason reason)
        {
            Player = player;
            Reason = reason;
        }

        public override string Describe() => "WeatherSkipped p" + Player + " " + Reason;
    }

    /// <summary>La rotta della nave è stata ruotata in senso orario (R-083, R-084).</summary>
    public sealed class HeadingRotatedEvent : GameEvent
    {
        public int Player { get; }
        public int Steps { get; }
        public Heading From { get; }
        public Heading To { get; }

        public HeadingRotatedEvent(int player, int steps, Heading from, Heading to)
        {
            Player = player;
            Steps = steps;
            From = from;
            To = to;
        }

        public override string Describe() => "HeadingRotated p" + Player + " +" + Steps + " " + From + "->" + To;
    }

    /// <summary>Un giocatore con il Timoniere ha scelto il risultato invece di tirare (R-086, R-073b). Pubblico, come un tiro.</summary>
    public sealed class DieChosenEvent : GameEvent
    {
        public int Player { get; }
        public int Value { get; }
        public DiceReason Reason { get; }

        public DieChosenEvent(int player, int value, DiceReason reason)
        {
            Player = player;
            Value = value;
            Reason = reason;
        }

        public override string Describe() => "DieChosen p" + Player + " " + Value + " " + Reason;
    }

    /// <summary>Inizio del movimento (R-060–R-065): la velocità di ogni nave, per id giocatore.</summary>
    public sealed class MovementStartedEvent : GameEvent
    {
        /// <summary>Velocità per giocatore; 0 per chi non si muove (Svago compreso).</summary>
        public IReadOnlyList<int> Speeds { get; }

        public MovementStartedEvent(IReadOnlyList<int> speeds)
        {
            Speeds = speeds;
        }

        public override string Describe() => "MovementStarted speeds=[" + Join(Speeds) + "]";
    }

    /// <summary>Una nave avanza di una cella nel passo <see cref="Step"/> (R-065): un evento per nave per passo.</summary>
    public sealed class ShipMovedEvent : GameEvent
    {
        public int Player { get; }
        public int Step { get; }
        public Coord From { get; }
        public Coord To { get; }

        public ShipMovedEvent(int player, int step, Coord from, Coord to)
        {
            Player = player;
            Step = step;
            From = from;
            To = to;
        }

        public override string Describe() => "ShipMoved p" + Player + " s" + Step + " " + From + "->" + To;
    }

    public enum StopReason
    {
        /// <summary>Terra: isola, Isola Sacra o cornice (R-066).</summary>
        Land,
        /// <summary>Cella occupata da un'altra nave (R-066).</summary>
        Collision,
        /// <summary>Attraversamento (R-067).</summary>
        Crossing,
        /// <summary>La rotta porta fuori dalla mappa: la nave resta sulla cornice.</summary>
        MapEdge,
    }

    /// <summary>La nave si ferma e perde il movimento residuo (R-066, R-067).</summary>
    public sealed class ShipStoppedEvent : GameEvent
    {
        public int Player { get; }
        public int Step { get; }
        public Coord Cell { get; }
        public StopReason Reason { get; }

        public ShipStoppedEvent(int player, int step, Coord cell, StopReason reason)
        {
            Player = player;
            Step = step;
            Cell = cell;
            Reason = reason;
        }

        public override string Describe() => "ShipStopped p" + Player + " s" + Step + " " + Cell + " " + Reason;
    }

    /// <summary>Attraversamento (R-067): le due navi si fermano sulla cella scelta da <see cref="Chooser"/>.</summary>
    public sealed class CrossingResolvedEvent : GameEvent
    {
        public int Step { get; }
        public int PlayerA { get; }
        public int PlayerB { get; }
        public int Chooser { get; }
        public Coord Cell { get; }

        public CrossingResolvedEvent(int step, int playerA, int playerB, int chooser, Coord cell)
        {
            Step = step;
            PlayerA = playerA;
            PlayerB = playerB;
            Chooser = chooser;
            Cell = cell;
        }

        public override string Describe() =>
            "CrossingResolved s" + Step + " p" + PlayerA + "+p" + PlayerB + " by p" + Chooser + " at " + Cell;
    }

    /// <summary>La nave è finita sulla cornice: è arenata (R-069). Vale anche dopo un riposizionamento (R-073a).</summary>
    public sealed class ShipStrandedEvent : GameEvent
    {
        public int Player { get; }
        public Coord Cell { get; }

        public ShipStrandedEvent(int player, Coord cell)
        {
            Player = player;
            Cell = cell;
        }

        public override string Describe() => "ShipStranded p" + Player + " " + Cell;
    }

    /// <summary>La nave è entrata nell'Isola Sacra al passo <see cref="Step"/> (R-140, R-141; la fine partita arriva con la spec 0004).</summary>
    public sealed class SacredIslandEnteredEvent : GameEvent
    {
        public int Player { get; }
        public int Step { get; }
        public Coord Cell { get; }

        public SacredIslandEnteredEvent(int player, int step, Coord cell)
        {
            Player = player;
            Step = step;
            Cell = cell;
        }

        public override string Describe() => "SacredIslandEntered p" + Player + " s" + Step + " " + Cell;
    }

    /// <summary>Fine del movimento: la posizione di ogni nave, per id giocatore.</summary>
    public sealed class MovementEndedEvent : GameEvent
    {
        public IReadOnlyList<Coord> Positions { get; }

        public MovementEndedEvent(IReadOnlyList<Coord> positions)
        {
            Positions = positions;
        }

        public override string Describe() => "MovementEnded [" + Join(Positions) + "]";
    }

    /// <summary>Un Abbordaggio fortuito comincia sulla cella (R-070, R-071, R-073a).</summary>
    public sealed class BoardingStartedEvent : GameEvent
    {
        public Coord Cell { get; }
        public IReadOnlyList<int> Players { get; }
        /// <summary>0 per l'Abbordaggio nato dal movimento, n per l'n-esimo anello della catena (R-073a).</summary>
        public int ChainDepth { get; }
        /// <summary>Ripetizione del ciclo per tiri uguali (R-070): 0 al primo ciclo.</summary>
        public int Repeat { get; }

        public BoardingStartedEvent(Coord cell, IReadOnlyList<int> players, int chainDepth, int repeat)
        {
            Cell = cell;
            Players = players;
            ChainDepth = chainDepth;
            Repeat = repeat;
        }

        public override string Describe() =>
            "BoardingStarted " + Cell + " [" + Join(Players) + "] chain=" + ChainDepth + " repeat=" + Repeat;
    }

    /// <summary>Riposizionamento dopo un Abbordaggio (R-072): la nave va nella cella adiacente indicata dal risultato.</summary>
    public sealed class ShipRepositionedEvent : GameEvent
    {
        public int Player { get; }
        public int Value { get; }
        public Coord From { get; }
        public Coord To { get; }

        public ShipRepositionedEvent(int player, int value, Coord from, Coord to)
        {
            Player = player;
            Value = value;
            From = from;
            To = to;
        }

        public override string Describe() => "ShipRepositioned p" + Player + " " + Value + " " + From + "->" + To;
    }

    /// <summary>
    /// Diagnostica (R-073a): raggiunto <c>maxAbbordaggioChain</c>, le navi coinvolte restano dove sono.
    /// In una partita normale non dovrebbe succedere.
    /// </summary>
    public sealed class BoardingChainLimitEvent : GameEvent
    {
        public Coord Cell { get; }
        public IReadOnlyList<int> Players { get; }
        public int Limit { get; }

        public BoardingChainLimitEvent(Coord cell, IReadOnlyList<int> players, int limit)
        {
            Cell = cell;
            Players = players;
            Limit = limit;
        }

        public override string Describe() => "BoardingChainLimit " + Cell + " [" + Join(Players) + "] limit=" + Limit;
    }

    public enum CrewLossCause
    {
        /// <summary>Tempesta (R-084).</summary>
        Storm,
        /// <summary>Abbordaggio fortuito (R-070, R-071).</summary>
        Boarding,
        /// <summary>Uomo in mare! (03 §2.1).</summary>
        ManOverboard,
        /// <summary>Arrembaggio! riuscito (R-110): la carta passa all'attaccante.</summary>
        Arrembaggio,
        /// <summary>Sostituita da una carta ricevuta (R-020).</summary>
        Replaced,
    }

    /// <summary>
    /// Perdita di una carta crew (R-021): la carta va negli scarti Crew. Sopra coperta è pubblica; sotto coperta
    /// gli altri sanno solo che lo slot si è svuotato. <see cref="AtSea"/> serve a "Parlare con i pesci" (R-022).
    /// </summary>
    public sealed class CrewLostEvent : GameEvent
    {
        public int Player { get; }
        public int Slot { get; }
        public bool Above { get; }
        public CrewLossCause Cause { get; }
        public bool AtSea { get; }
        /// <summary>Null nella forma pubblica di una perdita sotto coperta.</summary>
        public CrewCard Card { get; }

        public CrewLostEvent(int player, int slot, bool above, CrewLossCause cause, bool atSea, CrewCard card)
            : base(player, false, !above)
        {
            Player = player;
            Slot = slot;
            Above = above;
            Cause = cause;
            AtSea = atSea;
            Card = card;
        }

        private CrewLostEvent(CrewLostEvent source) : base(source.Player, true, true)
        {
            Player = source.Player;
            Slot = source.Slot;
            Above = source.Above;
            Cause = source.Cause;
            AtSea = source.AtSea;
            Card = null;
        }

        protected override GameEvent Redact() => new CrewLostEvent(this);

        public override string Describe() =>
            "CrewLost p" + Player + " slot" + Slot + " " + Cause + (AtSea ? " atSea" : "") + (Card != null ? " " + Card : "");
    }

    /// <summary>
    /// Il Medico di bordo (R-023) sale sopra coperta al posto della carta minacciata, che scende nel suo slot:
    /// la perdita è annullata e non conta come perdita (R-021). Pubblico: il Medico ora è scoperto.
    /// </summary>
    public sealed class MedicoUsedEvent : GameEvent
    {
        public int Player { get; }
        public int MedicoSlot { get; }
        public int ThreatenedSlot { get; }
        public CrewCard Medico { get; }

        public MedicoUsedEvent(int player, int medicoSlot, int threatenedSlot, CrewCard medico)
        {
            Player = player;
            MedicoSlot = medicoSlot;
            ThreatenedSlot = threatenedSlot;
            Medico = medico;
        }

        public override string Describe() => "MedicoUsed p" + Player + " " + MedicoSlot + "<->" + ThreatenedSlot;
    }

    /// <summary>Inizio del turno di un giocatore in Fase 2.</summary>
    public sealed class TurnStartedEvent : GameEvent
    {
        public int Round { get; }
        public int Player { get; }

        public TurnStartedEvent(int round, int player)
        {
            Round = round;
            Player = player;
        }

        public override string Describe() => "TurnStarted r" + Round + " p" + Player;
    }

    public enum TurnSkipReason
    {
        /// <summary>Nave sulla cornice (R-053).</summary>
        Border,
        /// <summary>Nave sull'Isola Sacra: nessuna azione di porto (R-055).</summary>
        SacredIsland,
    }

    /// <summary>Il turno termina subito (R-053, R-055).</summary>
    public sealed class TurnSkippedEvent : GameEvent
    {
        public int Player { get; }
        public TurnSkipReason Reason { get; }

        public TurnSkippedEvent(int player, TurnSkipReason reason = TurnSkipReason.Border)
        {
            Player = player;
            Reason = reason;
        }

        public override string Describe() => "TurnSkipped p" + Player + " " + Reason;
    }

    public sealed class TurnEndedEvent : GameEvent
    {
        public int Round { get; }
        public int Player { get; }

        public TurnEndedEvent(int round, int player)
        {
            Round = round;
            Player = player;
        }

        public override string Describe() => "TurnEnded r" + Round + " p" + Player;
    }

    public enum GameEndReason
    {
        /// <summary>Raggiunto <c>maxRounds</c> (R-150): solo nelle simulazioni.</summary>
        RoundLimit,
    }

    public sealed class GameEndedEvent : GameEvent
    {
        public GameEndReason Reason { get; }
        /// <summary>Round completati.</summary>
        public int RoundsPlayed { get; }

        public GameEndedEvent(GameEndReason reason, int roundsPlayed)
        {
            Reason = reason;
            RoundsPlayed = roundsPlayed;
        }

        public override string Describe() => "GameEnded " + Reason + " rounds=" + RoundsPlayed;
    }
}
