using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.Setup;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// Ciò che i flussi di gioco usano per agire: stato, configurazione, casualità, emissione di eventi e
    /// le operazioni comuni (pescare con riciclo degli scarti di Crew e Pirateria, tirare il dado, spostare monete, chiedere una decisione).
    /// Le regole nuove passano da qui, così eventi e casualità restano sempre coerenti.
    /// </summary>
    internal sealed class GameContext
    {
        private readonly List<GameEvent> events = new List<GameEvent>();
        private int nextDecisionId = 1;

        public GameState State { get; }
        public RulesConfig Config { get; }
        public IRandomSource Random { get; }
        public GameSetup Setup { get; }

        public GameContext(GameState state, RulesConfig config, IRandomSource random, GameSetup setup)
        {
            State = state;
            Config = config;
            Random = random;
            Setup = setup;
        }

        /// <summary>Registra l'evento; le missioni in mano ne aggiornano i contatori (R-131, <see cref="Missions.Observe"/>).</summary>
        public void Emit(GameEvent gameEvent)
        {
            events.Add(gameEvent);
            Missions.Observe(State, gameEvent);
        }

        /// <summary>Gli eventi emessi dall'ultima chiamata, nell'ordine in cui sono successi.</summary>
        public IReadOnlyList<GameEvent> TakeEvents()
        {
            var taken = events.ToArray();
            events.Clear();
            return taken;
        }

        /// <summary>Prepara la richiesta di una decisione; il flusso la rende con <c>yield return</c>.</summary>
        public AskStep Ask(DecisionKind kind, int player, bool isSecret, IReadOnlyList<DecisionOption> options,
            IReadOnlyList<Card> cards = null, bool requiresConfirmation = false)
        {
            return new AskStep(new PendingDecision(nextDecisionId++, kind, player, isSecret, requiresConfirmation, options, cards));
        }

        /// <summary>Tira un d8 dalla sorgente del motore e ne emette l'evento (04_motore.md §4).</summary>
        public int RollD8(int player, DiceReason reason)
        {
            int value = Random.RollD8();
            Emit(new DieRolledEvent(player, value, reason));
            return value;
        }

        /// <summary>Cambia le monete di un giocatore (mai sotto zero, R-006) ed emette l'evento.</summary>
        public void ChangeCoins(PlayerState player, int delta, CoinReason reason)
        {
            player.Coins += delta;
            Emit(new CoinsChangedEvent(player.Id, delta, player.Coins, reason));
        }

        /// <summary>Cambia i segnalini taglia di un giocatore ed emette l'evento.</summary>
        public void ChangeBounty(PlayerState player, int delta, BountyReason reason)
        {
            player.BountyTokens += delta;
            player.BountyBySource[reason] = player.BountyFrom(reason) + delta;
            Emit(new BountyChangedEvent(player.Id, delta, player.BountyTokens, reason));
        }

        /// <summary>
        /// Mette una carta (o null) in uno slot della ciurma ed emette l'evento. Un Nostromo che arriva sopra coperta
        /// resta bloccato sopra (R-016) finché il Medico non lo libera (<see cref="FreeNostromoByMedico"/>). Portare
        /// sotto un Nostromo bloccato è un errore del motore: nessun flusso deve chiederlo.
        /// </summary>
        public void PutCrew(PlayerState player, int slot, CrewCard card)
        {
            bool above = player.Crew.IsAbove(slot);
            if (card != null && !above && !CanGoBelow(card))
                throw new System.InvalidOperationException("R-016: il Nostromo " + card + " è bloccato sopra coperta (p" +
                                                           player.Id + ", slot " + slot + ").");
            player.Crew.Set(slot, card);
            if (card != null && above && card.Kind == CrewCardId.Nostromo)
            {
                State.LockedNostromi.Add(card.Uid);
                State.NostromiFreedByMedico.Remove(card.Uid);
            }

            Emit(new CrewSlotChangedEvent(player.Id, slot, above, card));
        }

        /// <summary>
        /// Toglie la carta dallo slot (perdita, Arrembaggio!, Commercio, sostituzione). Il blocco del Nostromo vale solo
        /// mentre è sopra coperta su una nave (R-016): uscito dalla nave non è più bloccato, e chi lo riceve può metterlo
        /// anche sotto coperta (R-020).
        /// </summary>
        public CrewCard TakeCrew(PlayerState player, int slot)
        {
            CrewCard card = player.Crew.Take(slot);
            if (card != null)
            {
                State.LockedNostromi.Remove(card.Uid);
                State.NostromiFreedByMedico.Remove(card.Uid);
            }

            return card;
        }

        /// <summary>
        /// R-023: il Medico è l'unico effetto che può riportare sotto coperta un Nostromo bloccato (eccezione a R-016).
        /// Chiamato solo da <see cref="Losses.LoseCrew"/>, prima di scambiare Medico e Nostromo.
        /// </summary>
        public void FreeNostromoByMedico(CrewCard card)
        {
            if (!State.LockedNostromi.Remove(card.Uid)) return;
            State.NostromiFreedByMedico.Add(card.Uid);
        }

        /// <summary>Falso per un Nostromo bloccato sopra coperta (R-016): non può finire sotto.</summary>
        public bool CanGoBelow(CrewCard card) => card == null || !State.LockedNostromi.Contains(card.Uid);

        /// <summary>
        /// Se si possono scambiare le carte di due slot dello stesso giocatore (o spostare in uno slot vuoto) senza
        /// portare sotto coperta un Nostromo bloccato (R-016).
        /// </summary>
        public bool CanExchange(PlayerState player, int a, int b)
        {
            if (player.Crew.IsBelow(b) && !CanGoBelow(player.Crew[a])) return false;
            if (player.Crew.IsBelow(a) && !CanGoBelow(player.Crew[b])) return false;
            return true;
        }

        /// <summary>Scambia il contenuto di due slot dello stesso giocatore (uno può essere vuoto).</summary>
        public void Exchange(PlayerState player, int a, int b)
        {
            CrewCard first = player.Crew[a];
            CrewCard second = player.Crew[b];
            PutCrew(player, a, second);
            PutCrew(player, b, first);
        }

        public void ChangeTreasure(int delta)
        {
            State.Treasure += delta;
            Emit(new TreasureChangedEvent(delta, State.Treasure));
        }

        /// <summary>
        /// Se dal mazzo si può pescare almeno una carta (R-009): Crew e Pirateria contano anche gli scarti da rimescolare,
        /// Corsaro solo il mazzo. Per il Corsaro vuoto l'azione di porto "Missione" non è selezionabile.
        /// </summary>
        public bool CanDraw(DeckKind deck)
        {
            switch (deck)
            {
                case DeckKind.Crew: return State.Crew.DrawCount + State.Crew.DiscardCount > 0;
                case DeckKind.Pirate: return State.Pirate.DrawCount + State.Pirate.DiscardCount > 0;
                default: return State.Corsair.DrawCount > 0;
            }
        }

        /// <summary>
        /// Pesca fino a <paramref name="count"/> carte dal mazzo (meno se non bastano, R-009: per il Corsaro quelle
        /// rimaste, per Crew e Pirateria anche gli scarti rimescolati) e le
        /// lascia "in transito" presso il giocatore, in attesa di una scelta. Emette l'evento di pesca.
        /// </summary>
        public IReadOnlyList<Card> DrawToTransit(PlayerState player, DeckKind deck, int count)
        {
            var drawn = DrawCards(deck, count);
            player.InTransit.AddRange(drawn);
            if (drawn.Count > 0) Emit(new CardsDrawnEvent(player.Id, deck, drawn));
            return drawn;
        }

        /// <summary>Pesca carte Pirateria direttamente nella mano del giocatore.</summary>
        public IReadOnlyList<PirateCard> DrawToHand(PlayerState player, int count)
        {
            var drawn = DrawCards(DeckKind.Pirate, count);
            var cards = new List<PirateCard>();
            foreach (Card card in drawn) cards.Add((PirateCard)card);
            player.Hand.AddRange(cards);
            if (drawn.Count > 0) Emit(new CardsDrawnEvent(player.Id, DeckKind.Pirate, drawn));
            return cards;
        }

        private List<Card> DrawCards(DeckKind deck, int count)
        {
            var drawn = new List<Card>();
            for (int i = 0; i < count; i++)
            {
                Card card = DrawOne(deck);
                if (card == null) break;
                drawn.Add(card);
            }

            return drawn;
        }

        private Card DrawOne(DeckKind deck)
        {
            switch (deck)
            {
                case DeckKind.Crew: return DrawOne(State.Crew, deck, true);
                case DeckKind.Pirate: return DrawOne(State.Pirate, deck, true);
                default: return DrawOne(State.Corsair, deck, false);
            }
        }

        /// <summary>R-009: il Corsaro non si rimescola, i suoi scarti sono fuori dal gioco.</summary>
        private Card DrawOne<T>(DeckState<T> deck, DeckKind kind, bool recyclesDiscard) where T : Card
        {
            T card = deck.TakeTop();
            if (card == null && recyclesDiscard && deck.DiscardCount > 0)
            {
                int recycled = deck.DiscardCount;
                deck.RecycleDiscard(Random);
                Emit(new DeckShuffledEvent(kind, recycled, true));
                card = deck.TakeTop();
            }

            return card;
        }
    }
}
