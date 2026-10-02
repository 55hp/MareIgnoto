using System;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Map;

namespace hp55games.MareIgnoto.Rules.Decisions
{
    /// <summary>
    /// I tipi di decisione (04_motore.md §2). Crescono con le spec successive: ogni nuovo tipo aggiunge un valore
    /// qui e una classe di opzione, senza toccare il resto del contratto.
    /// </summary>
    public enum DecisionKind
    {
        /// <summary>Tenere missioni tra quelle pescate (R-034). Opzioni: <see cref="KeepMissionsOption"/>.</summary>
        KeepMissions,
        /// <summary>Posizionare le crew iniziali sotto coperta (R-032). Opzioni: <see cref="PlaceCrewOption"/>.</summary>
        PlaceStartingCrew,
        /// <summary>Offerta a Gartya (R-036). Opzioni: <see cref="OfferOption"/>.</summary>
        GartyaOffer,
        /// <summary>Scelta della rotta (R-040). Opzioni: <see cref="HeadingOption"/>.</summary>
        ChooseHeading,
        /// <summary>Timoniere: rotazione della rotta per il meteo invece del d8 (R-086). Opzioni: <see cref="DieValueOption"/>.</summary>
        WeatherRotation,
        /// <summary>Mare Mosso: carta Pirateria da perdere (R-083). Opzioni: <see cref="PirateCardOption"/>.</summary>
        WeatherPirateLoss,
        /// <summary>Tempesta: crew sotto coperta da perdere (R-084). Opzioni: <see cref="CrewSlotOption"/>.</summary>
        StormCrewLoss,
        /// <summary>Attraversamento: cella dove si fermano le due navi (R-067). Opzioni: <see cref="CellOption"/>.</summary>
        CrossingCell,
        /// <summary>Abbordaggio fortuito: cosa perdere (R-070, R-071, R-073c). Opzioni: <see cref="BoardingLossOption"/>.</summary>
        BoardingLoss,
        /// <summary>Usare il Medico al posto della carta minacciata? (R-023, R-073d). Opzioni: <see cref="MedicoOption"/>.</summary>
        UseMedico,
        /// <summary>Timoniere: risultato del riposizionamento invece del d8 (R-073b). Opzioni: <see cref="DieValueOption"/>.</summary>
        BoardingReposition,
    }

    /// <summary>Una scelta legale. Ogni tipo di decisione usa la sua sottoclasse.</summary>
    public abstract class DecisionOption
    {
        /// <summary>Descrizione testuale deterministica, per test e debug (non è testo per il giocatore).</summary>
        public abstract string Describe();

        public override string ToString() => Describe();
    }

    public sealed class HeadingOption : DecisionOption
    {
        public Heading Heading { get; }

        public HeadingOption(Heading heading)
        {
            Heading = heading;
        }

        public override string Describe() => "Heading " + Heading;
    }

    public sealed class OfferOption : DecisionOption
    {
        public int Amount { get; }

        public OfferOption(int amount)
        {
            Amount = amount;
        }

        public override string Describe() => "Offer " + Amount;
    }

    /// <summary>Le missioni da tenere; le altre pescate vanno negli scarti.</summary>
    public sealed class KeepMissionsOption : DecisionOption
    {
        public IReadOnlyList<MissionCard> Kept { get; }

        public KeepMissionsOption(IReadOnlyList<MissionCard> kept)
        {
            Kept = kept;
        }

        public override string Describe() => "Keep [" + string.Join(",", Kept) + "]";
    }

    /// <summary>Dove mettere una carta crew.</summary>
    public readonly struct CrewPlacement
    {
        public readonly CrewCard Card;
        public readonly int Slot;

        public CrewPlacement(CrewCard card, int slot)
        {
            Card = card;
            Slot = slot;
        }

        public override string ToString() => Card + "->" + Slot;
    }

    /// <summary>Un posizionamento completo di tutte le crew pescate, ognuna in uno slot diverso.</summary>
    public sealed class PlaceCrewOption : DecisionOption
    {
        public IReadOnlyList<CrewPlacement> Placements { get; }

        public PlaceCrewOption(IReadOnlyList<CrewPlacement> placements)
        {
            Placements = placements;
        }

        public override string Describe() => "Place [" + string.Join(",", Placements) + "]";
    }

    /// <summary>Un risultato del d8 scelto invece di tirare (Timoniere, R-086, R-073b): 1..8.</summary>
    public sealed class DieValueOption : DecisionOption
    {
        public int Value { get; }

        public DieValueOption(int value)
        {
            Value = value;
        }

        public override string Describe() => "Die " + Value;
    }

    /// <summary>Una carta Pirateria della mano (per le carte uguali il motore offre una sola opzione).</summary>
    public sealed class PirateCardOption : DecisionOption
    {
        public PirateCard Card { get; }

        public PirateCardOption(PirateCard card)
        {
            Card = card;
        }

        public override string Describe() => "Pirate " + Card;
    }

    /// <summary>Uno slot della propria ciurma e la carta che contiene.</summary>
    public sealed class CrewSlotOption : DecisionOption
    {
        public int Slot { get; }
        public CrewCard Card { get; }

        public CrewSlotOption(int slot, CrewCard card)
        {
            Slot = slot;
            Card = card;
        }

        public override string Describe() => "Slot " + Slot + " " + Card;
    }

    public sealed class CellOption : DecisionOption
    {
        public Coord Cell { get; }

        public CellOption(Coord cell)
        {
            Cell = cell;
        }

        public override string Describe() => "Cell " + Cell;
    }

    /// <summary>
    /// Cosa perde un giocatore in un Abbordaggio fortuito: carte crew (per slot) e/o carte Pirateria. A due navi è
    /// l'una o le altre (R-070), a 3+ entrambe (R-071); con meno carte del dovuto si perde ciò che si ha (R-073c).
    /// </summary>
    public sealed class BoardingLossOption : DecisionOption
    {
        public IReadOnlyList<int> CrewSlots { get; }
        public IReadOnlyList<PirateCard> PirateCards { get; }

        public BoardingLossOption(IReadOnlyList<int> crewSlots, IReadOnlyList<PirateCard> pirateCards)
        {
            CrewSlots = crewSlots;
            PirateCards = pirateCards;
        }

        public override string Describe() =>
            "Lose crew=[" + string.Join(",", CrewSlots) + "] pirate=[" + string.Join(",", PirateCards) + "]";
    }

    /// <summary>Il Medico da usare (slot sotto coperta), oppure -1 per lasciar perdere la carta minacciata (R-023).</summary>
    public sealed class MedicoOption : DecisionOption
    {
        public int MedicoSlot { get; }

        public bool Use => MedicoSlot >= 0;

        public MedicoOption(int medicoSlot)
        {
            MedicoSlot = medicoSlot;
        }

        public override string Describe() => Use ? "Medico " + MedicoSlot : "Medico no";
    }

    /// <summary>
    /// La decisione in attesa (04_motore.md §2): chi deve decidere, di che tipo, con quali opzioni legali e se
    /// è segreta (hot-seat: gli altri giocatori non devono vedere lo schermo). La risposta si costruisce con
    /// <see cref="Choose(int)"/> o <see cref="Choose(DecisionOption)"/> e si passa a GameSession.Submit.
    /// Ogni decisione espone sempre tutte le sue opzioni legali: è ciò che permette al bot di giocarla.
    /// </summary>
    public sealed class PendingDecision
    {
        /// <summary>Identifica questa decisione: una risposta a una decisione già chiusa viene rifiutata.</summary>
        public int Id { get; }
        public DecisionKind Kind { get; }
        public int Player { get; }
        public bool IsSecret { get; }

        /// <summary>
        /// Le opzioni legali, sempre almeno una. Il tipo concreto dipende da <see cref="Kind"/>.
        /// </summary>
        public IReadOnlyList<DecisionOption> Options { get; }

        /// <summary>Le carte su cui si decide (pescate in attesa di scelta); vuota se non ce ne sono.</summary>
        public IReadOnlyList<Card> Cards { get; }

        /// <summary>
        /// Vero per le decisioni segrete che il giocatore deve confermare anche con una sola opzione legale
        /// (le scelte di Fase 1, 04_motore.md §2). Le altre con una sola opzione le applica il motore da solo.
        /// </summary>
        public bool RequiresConfirmation { get; }

        internal PendingDecision(int id, DecisionKind kind, int player, bool isSecret, bool requiresConfirmation,
            IReadOnlyList<DecisionOption> options, IReadOnlyList<Card> cards)
        {
            if (options == null || options.Count == 0)
                throw new ArgumentException("Una decisione ha almeno un'opzione legale.", nameof(options));

            Id = id;
            Kind = kind;
            Player = player;
            IsSecret = isSecret;
            RequiresConfirmation = requiresConfirmation;
            Options = options;
            Cards = cards ?? new Card[0];
        }

        public DecisionAnswer Choose(int optionIndex) => new DecisionAnswer(Id, optionIndex);

        /// <summary>Risposta per l'opzione data (la stessa istanza presente in <see cref="Options"/>).</summary>
        public DecisionAnswer Choose(DecisionOption option)
        {
            for (int i = 0; i < Options.Count; i++)
                if (ReferenceEquals(Options[i], option)) return Choose(i);
            throw new ArgumentException("L'opzione non appartiene a questa decisione.", nameof(option));
        }

        public override string ToString() => "Decision#" + Id + " " + Kind + " p" + Player + " options=" + Options.Count;
    }

    /// <summary>La risposta del giocatore: l'indice dell'opzione scelta, per la decisione indicata.</summary>
    public sealed class DecisionAnswer
    {
        public int DecisionId { get; }
        public int OptionIndex { get; }

        public DecisionAnswer(int decisionId, int optionIndex)
        {
            DecisionId = decisionId;
            OptionIndex = optionIndex;
        }
    }

    /// <summary>Risposta non valida per la decisione in attesa. Lo stato di gioco non è cambiato.</summary>
    public sealed class InvalidDecisionException : Exception
    {
        public InvalidDecisionException(string message) : base(message)
        {
        }
    }
}
