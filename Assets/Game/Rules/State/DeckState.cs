using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Random;

namespace hp55games.MareIgnoto.Rules.State
{
    /// <summary>Dimensioni di un mazzo e della sua pila degli scarti (informazione pubblica).</summary>
    public readonly struct DeckInfo
    {
        public readonly DeckKind Kind;
        public readonly int DrawCount;
        public readonly int DiscardCount;

        public DeckInfo(DeckKind kind, int drawCount, int discardCount)
        {
            Kind = kind;
            DrawCount = drawCount;
            DiscardCount = discardCount;
        }
    }

    /// <summary>Un mazzo con la sua pila degli scarti (R-009; per il Corsaro la pila è fuori dal gioco). La cima del mazzo è l'ultimo elemento.</summary>
    internal sealed class DeckState<T> where T : Card
    {
        private readonly List<T> pile = new List<T>();
        private readonly List<T> discard = new List<T>();

        public int DrawCount => pile.Count;
        public int DiscardCount => discard.Count;
        public IReadOnlyList<T> DrawPile => pile;
        public IReadOnlyList<T> DiscardPile => discard;

        /// <summary>Riempie il mazzo (usato una volta sola, alla creazione).</summary>
        public void Fill(IEnumerable<T> cards)
        {
            pile.AddRange(cards);
        }

        public void Shuffle(IRandomSource random)
        {
            random.Shuffle(pile);
        }

        /// <summary>Porta in cima le carte indicate, nell'ordine di pesca (la prima della lista è la prossima a uscire).</summary>
        public void MoveToTop(IReadOnlyList<T> inDrawOrder)
        {
            foreach (T card in inDrawOrder) pile.Remove(card);
            for (int i = inDrawOrder.Count - 1; i >= 0; i--) pile.Add(inDrawOrder[i]);
        }

        /// <summary>Toglie la carta in cima; null se il mazzo è vuoto (non rimescola: lo fa GameContext, R-009).</summary>
        public T TakeTop()
        {
            if (pile.Count == 0) return null;
            T card = pile[pile.Count - 1];
            pile.RemoveAt(pile.Count - 1);
            return card;
        }

        /// <summary>Toglie una carta precisa dal mazzo o dagli scarti (per gli scenari dei test); falso se non c'è.</summary>
        public bool Remove(T card) => pile.Remove(card) || discard.Remove(card);

        public void Discard(T card)
        {
            discard.Add(card);
        }

        /// <summary>Gli scarti rimescolati diventano il nuovo mazzo (R-009).</summary>
        public void RecycleDiscard(IRandomSource random)
        {
            pile.AddRange(discard);
            discard.Clear();
            random.Shuffle(pile);
        }

        public DeckInfo Info(DeckKind kind) => new DeckInfo(kind, pile.Count, discard.Count);
    }
}
