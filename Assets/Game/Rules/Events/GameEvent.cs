using System.Collections.Generic;

namespace hp55games.MareIgnoto.Rules.Events
{
    public enum EventVisibility
    {
        /// <summary>Visibile a tutti.</summary>
        Public,
        /// <summary>I dettagli sono visibili solo al giocatore <see cref="GameEvent.VisibleTo"/>.</summary>
        Private,
    }

    /// <summary>
    /// Qualcosa che è successo, immutabile, nell'ordine in cui è successo (04_motore.md §3).
    /// Un evento con dettagli segreti ha visibilità <see cref="EventVisibility.Private"/>: la presentazione di un
    /// giocatore lo legge con <see cref="ViewFor"/>, che per gli altri restituisce la forma pubblica
    /// ("ha pescato 2 carte") senza i dettagli.
    /// </summary>
    public abstract class GameEvent
    {
        public EventVisibility Visibility { get; }

        /// <summary>Il giocatore che vede i dettagli; -1 per gli eventi pubblici.</summary>
        public int VisibleTo { get; }

        /// <summary>Vero nella forma pubblica di un evento che nasce privato: i dettagli sono stati tolti.</summary>
        public bool IsRedacted { get; }

        protected GameEvent()
        {
            Visibility = EventVisibility.Public;
            VisibleTo = -1;
        }

        /// <summary>Evento con dettagli privati per <paramref name="owner"/>, oppure (redacted) la sua forma pubblica.</summary>
        protected GameEvent(int owner, bool redacted) : this(owner, redacted, true)
        {
        }

        /// <summary>Come sopra; se <paramref name="secret"/> è falso l'evento è pubblico (i dettagli segreti dipendono dal caso).</summary>
        protected GameEvent(int owner, bool redacted, bool secret)
        {
            bool isPrivate = secret && !redacted;
            Visibility = isPrivate ? EventVisibility.Private : EventVisibility.Public;
            VisibleTo = isPrivate ? owner : -1;
            IsRedacted = secret && redacted;
        }

        public bool IsVisibleTo(int viewer) => Visibility == EventVisibility.Public || VisibleTo == viewer;

        /// <summary>L'evento come lo vede <paramref name="viewer"/>: se ne vede i dettagli lo stesso evento, altrimenti la forma pubblica.</summary>
        public GameEvent ViewFor(int viewer) => IsVisibleTo(viewer) ? this : Redact();

        /// <summary>La forma pubblica, senza dettagli segreti. Gli eventi pubblici restituiscono se stessi.</summary>
        protected virtual GameEvent Redact() => this;

        /// <summary>Descrizione testuale deterministica, per test e debug (non è testo per il giocatore).</summary>
        public abstract string Describe();

        public override string ToString() => Describe();

        protected static string Join<T>(IEnumerable<T> items) => string.Join(",", items);
    }
}
