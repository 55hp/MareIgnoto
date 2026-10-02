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
