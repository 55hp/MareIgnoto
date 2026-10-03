using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Events;

namespace hp55games.MareIgnoto.Rules.Decisions
{
    /// <summary>Vedetta (R-058): giocare il turno come in porto o in mare.</summary>
    public sealed class TurnKindOption : DecisionOption
    {
        public TurnKind Kind { get; }

        public TurnKindOption(TurnKind kind)
        {
            Kind = kind;
        }

        public override string Describe() => "Turn " + Kind;
    }

    /// <summary>Un'azione di porto con il costo effettivo (R-095); <see cref="Slot"/> solo per Commercio.</summary>
    public sealed class PortActionOption : DecisionOption
    {
        public PortAction Action { get; }
        public int Cost { get; }
        /// <summary>Lo slot da vendere con Commercio (R-094), -1 per le altre azioni.</summary>
        public int Slot { get; }
        /// <summary>Monete ricevute da Commercio; 0 per le altre azioni.</summary>
        public int Value { get; }

        public PortActionOption(PortAction action, int cost, int slot = -1, int value = 0)
        {
            Action = action;
            Cost = cost;
            Slot = slot;
            Value = value;
        }

        public override string Describe() =>
            "Port " + Action + (Cost > 0 ? " cost=" + Cost : "") + (Slot >= 0 ? " slot" + Slot + " +" + Value : "");
    }

    /// <summary>Le crew da tenere tra quelle pescate col Reclutamento (R-092).</summary>
    public sealed class KeepCrewOption : DecisionOption
    {
        public IReadOnlyList<CrewCard> Kept { get; }

        public KeepCrewOption(IReadOnlyList<CrewCard> kept)
        {
            Kept = kept;
        }

        public override string Describe() => "KeepCrew [" + string.Join(",", Kept) + "]";
    }

    /// <summary>Dove va una crew ricevuta (R-020).</summary>
    public sealed class ReceiveCrewOption : DecisionOption
    {
        public ReceiveOutcome Outcome { get; }
        /// <summary>Lo slot vuoto o da sostituire; -1 se la carta si scarta.</summary>
        public int Slot { get; }

        public ReceiveCrewOption(ReceiveOutcome outcome, int slot)
        {
            Outcome = outcome;
            Slot = slot;
        }

        public override string Describe() => "Receive " + Outcome + (Slot >= 0 ? " slot" + Slot : "");
    }

    /// <summary>R-056 (A): pesca carte Pirateria e il turno termina. Solo come prima azione.</summary>
    public sealed class DrawCardsOption : DecisionOption
    {
        public override string Describe() => "DrawCards";
    }

    /// <summary>Swap crew (R-057): sposta la carta di <see cref="From"/> in <see cref="To"/>, scambiandole se occupato.</summary>
    public sealed class SwapOption : DecisionOption
    {
        public int From { get; }
        public int To { get; }

        public SwapOption(int from, int to)
        {
            From = from;
            To = to;
        }

        public override string Describe() => "Swap " + From + "->" + To;
    }

    /// <summary>Attacco navale (R-100–R-102) contro <see cref="Target"/> con l'apertura indicata.</summary>
    public sealed class AttackOption : DecisionOption
    {
        public int Target { get; }
        public AttackOpening Opening { get; }

        public AttackOption(int target, AttackOpening opening)
        {
            Target = target;
            Opening = opening;
        }

        public override string Describe() => "Attack p" + Target + " " + Opening;
    }

    /// <summary>Quartiermastro (R-111) contro <see cref="Target"/>: le carte si scelgono dopo.</summary>
    public sealed class QuartermasterOption : DecisionOption
    {
        public int Target { get; }

        public QuartermasterOption(int target)
        {
            Target = target;
        }

        public override string Describe() => "Quartermaster p" + Target;
    }

    /// <summary>Gioca una carta Pirateria del turno (§7.3); i parametri si scelgono dopo, se servono.</summary>
    public sealed class PlayCardOption : DecisionOption
    {
        public PirateCard Card { get; }

        public PlayCardOption(PirateCard card)
        {
            Card = card;
        }

        public override string Describe() => "Play " + Card;
    }

    public sealed class EndTurnOption : DecisionOption
    {
        public override string Describe() => "EndTurn";
    }

    /// <summary>Una nave avversaria bersaglio (Uomo in mare!).</summary>
    public sealed class TargetPlayerOption : DecisionOption
    {
        public int Target { get; }

        public TargetPlayerOption(int target)
        {
            Target = target;
        }

        public override string Describe() => "Target p" + Target;
    }

    /// <summary>Spyglass!: i due slot della nave bersaglio da scambiare (sotto coperta: alla cieca, per slot).</summary>
    public sealed class SpyglassOption : DecisionOption
    {
        public int Target { get; }
        public int SlotA { get; }
        public int SlotB { get; }

        public SpyglassOption(int target, int slotA, int slotB)
        {
            Target = target;
            SlotA = slotA;
            SlotB = slotB;
        }

        public override string Describe() => "Spyglass p" + Target + " " + SlotA + "<->" + SlotB;
    }

    /// <summary>Una zona meteo bersaglio (R-121).</summary>
    public sealed class ZoneOption : DecisionOption
    {
        public int Zone { get; }
        /// <summary>L'id della zona (05_mappa.md §3): le carte Meteo la bersagliano per id.</summary>
        public string ZoneId { get; }

        public ZoneOption(int zone, string zoneId)
        {
            Zone = zone;
            ZoneId = zoneId;
        }

        public override string Describe() => "Zone " + ZoneId;
    }

    /// <summary>Supplica a Gartya: scatti della lancetta del vento (+1 orario, -1 antiorario).</summary>
    public sealed class WindStepOption : DecisionOption
    {
        public int Steps { get; }

        public WindStepOption(int steps)
        {
            Steps = steps;
        }

        public override string Describe() => "Wind " + (Steps > 0 ? "+" : "") + Steps;
    }

    /// <summary>
    /// Risposta nel combattimento: una carta della mano (<see cref="Card"/>), una Bordata! gratuita del Cannoniere,
    /// oppure nessuna risposta (<see cref="Yield"/>: si perde la battaglia).
    /// </summary>
    public sealed class CombatResponseOption : DecisionOption
    {
        public CombatPlay Play { get; }
        public PirateCard Card { get; }
        public bool Yield { get; }

        private CombatResponseOption(CombatPlay play, PirateCard card, bool yield)
        {
            Play = play;
            Card = card;
            Yield = yield;
        }

        public static CombatResponseOption FromHand(CombatPlay play, PirateCard card) => new CombatResponseOption(play, card, false);

        public static CombatResponseOption Free() => new CombatResponseOption(CombatPlay.FreeBroadside, null, false);

        public static CombatResponseOption GiveUp() => new CombatResponseOption(CombatPlay.Broadside, null, true);

        public override string Describe() => Yield ? "Yield" : "Respond " + Play + (Card != null ? " " + Card : "");
    }

    /// <summary>Difesa da Arrembaggio! (R-110).</summary>
    public sealed class BoardingDefenseOption : DecisionOption
    {
        public BoardingDefense Defense { get; }
        /// <summary>La Parlè! giocata, null altrimenti.</summary>
        public PirateCard Card { get; }

        public BoardingDefenseOption(BoardingDefense defense, PirateCard card = null)
        {
            Defense = defense;
            Card = card;
        }

        public override string Describe() => "Defend " + Defense;
    }

    /// <summary>Quartiermastro (R-111): la propria carta sopra coperta e quella della nave bersaglio.</summary>
    public sealed class QuartermasterSwapOption : DecisionOption
    {
        public int OwnSlot { get; }
        public int TargetSlot { get; }

        public QuartermasterSwapOption(int ownSlot, int targetSlot)
        {
            OwnSlot = ownSlot;
            TargetSlot = targetSlot;
        }

        public override string Describe() => "QM " + OwnSlot + "<->" + TargetSlot;
    }
}
