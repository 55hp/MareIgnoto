using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Events
{
    // ---- Tipo di turno ----

    public enum TurnKind
    {
        /// <summary>Nave in mare (R-056).</summary>
        Sea,
        /// <summary>Nave su un'isola, in Svago o con la Vedetta che sceglie il porto (R-054, R-058, R-091).</summary>
        Port,
    }

    /// <summary>Il turno in Fase 2 è in mare o in porto.</summary>
    public sealed class TurnKindEvent : GameEvent
    {
        public int Player { get; }
        public TurnKind Kind { get; }
        /// <summary>Vero se il porto viene dalla Vedetta (R-058).</summary>
        public bool ByLookout { get; }

        public TurnKindEvent(int player, TurnKind kind, bool byLookout)
        {
            Player = player;
            Kind = kind;
            ByLookout = byLookout;
        }

        public override string Describe() => "TurnKind p" + Player + " " + Kind + (ByLookout ? " lookout" : "");
    }

    // ---- Porti (R-090–R-096) ----

    public enum PortAction
    {
        /// <summary>Saccheggio (R-090).</summary>
        Plunder,
        /// <summary>Svago (R-091).</summary>
        Leisure,
        /// <summary>Reclutamento, tieni 1 (R-092).</summary>
        RecruitOne,
        /// <summary>Reclutamento, tieni 2 (R-092).</summary>
        RecruitTwo,
        /// <summary>Missione (R-093).</summary>
        Mission,
        /// <summary>Commercio (R-094).</summary>
        Commerce,
        /// <summary>Nessuna azione: il turno di porto finisce senza mettere il segnalino (R-097).</summary>
        None,
    }

    /// <summary>Il segnalino isola del giocatore passa sull'isola dove ha compiuto un'azione di porto (R-097). Pubblico.</summary>
    public sealed class IslandMarkerPlacedEvent : GameEvent
    {
        public int Player { get; }
        public int IslandId { get; }
        /// <summary>L'isola da cui è stato tolto; -1 se era il primo.</summary>
        public int PreviousIslandId { get; }

        public IslandMarkerPlacedEvent(int player, int islandId, int previousIslandId)
        {
            Player = player;
            IslandId = islandId;
            PreviousIslandId = previousIslandId;
        }

        public override string Describe() => "IslandMarker p" + Player + " " + PreviousIslandId + "->" + IslandId;
    }

    /// <summary>L'azione di porto scelta, con il costo effettivo pagato (R-095).</summary>
    public sealed class PortActionEvent : GameEvent
    {
        public int Player { get; }
        public PortAction Action { get; }
        public int Cost { get; }

        public PortActionEvent(int player, PortAction action, int cost)
        {
            Player = player;
            Action = action;
            Cost = cost;
        }

        public override string Describe() => "PortAction p" + Player + " " + Action + " cost=" + Cost;
    }

    /// <summary>Commercio (R-094): la carta va negli scarti Crew. Dettagli privati se era sotto coperta.</summary>
    public sealed class CrewSoldEvent : GameEvent
    {
        public int Player { get; }
        public int Slot { get; }
        public bool Above { get; }
        public int Coins { get; }
        /// <summary>Null nella forma pubblica di una vendita da sotto coperta.</summary>
        public CrewCard Card { get; }

        public CrewSoldEvent(int player, int slot, bool above, int coins, CrewCard card) : base(player, false, !above)
        {
            Player = player;
            Slot = slot;
            Above = above;
            Coins = coins;
            Card = card;
        }

        private CrewSoldEvent(CrewSoldEvent source) : base(source.Player, true, true)
        {
            Player = source.Player;
            Slot = source.Slot;
            Above = source.Above;
            Coins = source.Coins;
        }

        protected override GameEvent Redact() => new CrewSoldEvent(this);

        public override string Describe() => "CrewSold p" + Player + " slot" + Slot + " +" + Coins + (Card != null ? " " + Card : "");
    }

    public enum ReceiveOutcome
    {
        /// <summary>Messa in uno slot vuoto.</summary>
        Placed,
        /// <summary>Al posto di una carta presente, che va negli scarti (R-020).</summary>
        Replaced,
        /// <summary>Scartata direttamente (R-020).</summary>
        Discarded,
    }

    /// <summary>
    /// Un giocatore riceve una carta crew (R-020). Privato se la carta finisce sotto coperta o negli scarti;
    /// gli altri vedono slot ed esito.
    /// </summary>
    public sealed class CrewReceivedEvent : GameEvent
    {
        public int Player { get; }
        public ReceiveOutcome Outcome { get; }
        /// <summary>Lo slot, -1 se scartata.</summary>
        public int Slot { get; }
        /// <summary>Null nella forma pubblica.</summary>
        public CrewCard Card { get; }

        public CrewReceivedEvent(int player, ReceiveOutcome outcome, int slot, bool above, CrewCard card)
            : base(player, false, !above)
        {
            Player = player;
            Outcome = outcome;
            Slot = slot;
            Card = card;
        }

        private CrewReceivedEvent(CrewReceivedEvent source) : base(source.Player, true, true)
        {
            Player = source.Player;
            Outcome = source.Outcome;
            Slot = source.Slot;
        }

        protected override GameEvent Redact() => new CrewReceivedEvent(this);

        public override string Describe() => "CrewReceived p" + Player + " " + Outcome + " slot" + Slot + (Card != null ? " " + Card : "");
    }

    // ---- Turno in mare (R-056, R-057) ----

    /// <summary>Swap crew (R-057): pubblico, le identità delle carte seguono i CrewSlotChangedEvent.</summary>
    public sealed class CrewSwappedEvent : GameEvent
    {
        public int Player { get; }
        public int From { get; }
        public int To { get; }

        public CrewSwappedEvent(int player, int from, int to)
        {
            Player = player;
            From = from;
            To = to;
        }

        public override string Describe() => "CrewSwapped p" + Player + " " + From + "<->" + To;
    }

    /// <summary>Una carta Pirateria giocata nel turno (§7.3) o come apertura d'attacco: pubblica.</summary>
    public sealed class CardPlayedEvent : GameEvent
    {
        public int Player { get; }
        public PirateCard Card { get; }
        /// <summary>Vero se ha consumato una delle carte del turno (R-056, R-112).</summary>
        public bool CountsAsTurnCard { get; }

        public CardPlayedEvent(int player, PirateCard card, bool countsAsTurnCard)
        {
            Player = player;
            Card = card;
            CountsAsTurnCard = countsAsTurnCard;
        }

        public override string Describe() => "CardPlayed p" + Player + " " + Card + (CountsAsTurnCard ? "" : " free");
    }

    /// <summary>Spyglass! (03 §2.1): scambiati due slot della nave bersaglio.</summary>
    public sealed class SpyglassEvent : GameEvent
    {
        public int Player { get; }
        public int Target { get; }
        public int SlotA { get; }
        public int SlotB { get; }

        public SpyglassEvent(int player, int target, int slotA, int slotB)
        {
            Player = player;
            Target = target;
            SlotA = slotA;
            SlotB = slotB;
        }

        public override string Describe() => "Spyglass p" + Player + " -> p" + Target + " " + SlotA + "<->" + SlotB;
    }

    /// <summary>Quartiermastro (R-111): scambio di carte sopra coperta tra due navi.</summary>
    public sealed class QuartermasterSwapEvent : GameEvent
    {
        public int Player { get; }
        public int OwnSlot { get; }
        public int Target { get; }
        public int TargetSlot { get; }

        public QuartermasterSwapEvent(int player, int ownSlot, int target, int targetSlot)
        {
            Player = player;
            OwnSlot = ownSlot;
            Target = target;
            TargetSlot = targetSlot;
        }

        public override string Describe() => "Quartermaster p" + Player + " slot" + OwnSlot + " <-> p" + Target + " slot" + TargetSlot;
    }

    // ---- Combattimento (R-100–R-112) ----

    public enum AttackOpening
    {
        /// <summary>Bordata! dalla mano (conta come carta del turno, R-112).</summary>
        HandBroadside,
        /// <summary>Bordata! gratuita del Cannoniere (R-105).</summary>
        FreeBroadside,
        /// <summary>Duello obbligatorio del Falconet: primo colpo gratuito (R-106).</summary>
        FalconetDuel,
        /// <summary>Arrembaggio! dalla mano (R-110, conta come carta del turno).</summary>
        Arrembaggio,
    }

    public sealed class AttackStartedEvent : GameEvent
    {
        public int Attacker { get; }
        public int Defender { get; }
        public AttackOpening Opening { get; }
        public int Distance { get; }
        /// <summary>Culverin da 2+ celle: il difensore non può giocare Parlè! (R-108).</summary>
        public bool ParleForbidden { get; }

        public AttackStartedEvent(int attacker, int defender, AttackOpening opening, int distance, bool parleForbidden)
        {
            Attacker = attacker;
            Defender = defender;
            Opening = opening;
            Distance = distance;
            ParleForbidden = parleForbidden;
        }

        public override string Describe() =>
            "AttackStarted p" + Attacker + " -> p" + Defender + " " + Opening + " d=" + Distance + (ParleForbidden ? " noParle" : "");
    }

    public enum CombatPlay
    {
        Broadside,
        /// <summary>Bordata! gratuita (Cannoniere, primo colpo del Falconet): nessuna carta consumata.</summary>
        FreeBroadside,
        Parle,
    }

    /// <summary>Un colpo o una difesa del botta e risposta (R-103, R-106): pubblico, carta per carta.</summary>
    public sealed class CombatPlayEvent : GameEvent
    {
        public int Player { get; }
        public CombatPlay Play { get; }
        /// <summary>Null per le Bordate gratuite.</summary>
        public PirateCard Card { get; }

        public CombatPlayEvent(int player, CombatPlay play, PirateCard card)
        {
            Player = player;
            Play = play;
            Card = card;
        }

        public override string Describe() => "CombatPlay p" + Player + " " + Play;
    }

    /// <summary>Chi doveva rispondere non lo fa (o non può): perde la battaglia.</summary>
    public sealed class CombatYieldedEvent : GameEvent
    {
        public int Player { get; }

        public CombatYieldedEvent(int player)
        {
            Player = player;
        }

        public override string Describe() => "CombatYielded p" + Player;
    }

    /// <summary>Battaglia vinta (R-104), con Saker (R-107) e Nostromo (R-109).</summary>
    public sealed class BattleWonEvent : GameEvent
    {
        public int Winner { get; }
        public int Loser { get; }
        public int Attacker { get; }
        public int Tokens { get; }
        public int Coins { get; }
        /// <summary>Moltiplicatore del Saker applicato (1 se nessuno).</summary>
        public int SakerFactor { get; }
        public int CardsStolen { get; }

        public BattleWonEvent(int winner, int loser, int attacker, int tokens, int coins, int sakerFactor, int cardsStolen)
        {
            Winner = winner;
            Loser = loser;
            Attacker = attacker;
            Tokens = tokens;
            Coins = coins;
            SakerFactor = sakerFactor;
            CardsStolen = cardsStolen;
        }

        public override string Describe() =>
            "BattleWon p" + Winner + " over p" + Loser + " tokens=" + Tokens + " coins=" + Coins + " x" + SakerFactor + " stolen=" + CardsStolen;
    }

    public enum BountyReason
    {
        /// <summary>Battaglia vinta (R-104, R-107).</summary>
        Battle,
        /// <summary>Missione Corsaro completata (R-132).</summary>
        Mission,
        /// <summary>Tesoro dell'Isola Sacra (R-140, R-141).</summary>
        Treasure,
    }

    public sealed class BountyChangedEvent : GameEvent
    {
        public int Player { get; }
        public int Delta { get; }
        public int Total { get; }
        public BountyReason Reason { get; }

        public BountyChangedEvent(int player, int delta, int total, BountyReason reason)
        {
            Player = player;
            Delta = delta;
            Total = total;
            Reason = reason;
        }

        public override string Describe() => "BountyChanged p" + Player + " " + Delta + " -> " + Total + " " + Reason;
    }

    /// <summary>
    /// Nostromo (R-109): una carta Pirateria passa dalla mano del perdente a quella del vincitore. La carta la vede solo
    /// il vincitore; il perdente la ritrova mancante nella propria vista.
    /// </summary>
    public sealed class CardStolenEvent : GameEvent
    {
        public int Thief { get; }
        public int Victim { get; }
        /// <summary>Null nella forma pubblica.</summary>
        public PirateCard Card { get; }

        public CardStolenEvent(int thief, int victim, PirateCard card) : base(thief, false)
        {
            Thief = thief;
            Victim = victim;
            Card = card;
        }

        private CardStolenEvent(CardStolenEvent source) : base(source.Thief, true)
        {
            Thief = source.Thief;
            Victim = source.Victim;
        }

        protected override GameEvent Redact() => new CardStolenEvent(this);

        public override string Describe() => "CardStolen p" + Victim + " -> p" + Thief + (Card != null ? " " + Card : "");
    }

    public enum BoardingDefense
    {
        Parle,
        /// <summary>Il difensore paga le monete di <c>boardingParryCoins</c> all'attaccante.</summary>
        Paid,
        /// <summary>Nessuna difesa: l'attaccante prende una crew sopra coperta.</summary>
        None,
    }

    /// <summary>La risposta del difensore ad Arrembaggio! (R-110).</summary>
    public sealed class BoardingDefendedEvent : GameEvent
    {
        public int Defender { get; }
        public BoardingDefense Defense { get; }

        public BoardingDefendedEvent(int defender, BoardingDefense defense)
        {
            Defender = defender;
            Defense = defense;
        }

        public override string Describe() => "BoardingDefended p" + Defender + " " + Defense;
    }
}
