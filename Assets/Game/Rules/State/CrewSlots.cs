using System;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;

namespace hp55games.MareIgnoto.Rules.State
{
    /// <summary>
    /// Gli slot della ciurma (R-010): prima quelli sopra coperta (indici 0..above-1, scoperti),
    /// poi quelli sotto coperta. Uno slot può essere vuoto (null).
    /// </summary>
    internal sealed class CrewSlots
    {
        private readonly CrewCard[] cards;

        public int AboveCount { get; }
        public int Count => cards.Length;
        public int BelowCount => cards.Length - AboveCount;

        public CrewSlots(int above, int below)
        {
            AboveCount = above;
            cards = new CrewCard[above + below];
        }

        public bool IsAbove(int slot) => slot >= 0 && slot < AboveCount;

        public bool IsBelow(int slot) => slot >= AboveCount && slot < cards.Length;

        public CrewCard this[int slot]
        {
            get
            {
                CheckSlot(slot);
                return cards[slot];
            }
        }

        public void Set(int slot, CrewCard card)
        {
            CheckSlot(slot);
            cards[slot] = card;
        }

        public CrewCard Take(int slot)
        {
            CheckSlot(slot);
            CrewCard card = cards[slot];
            cards[slot] = null;
            return card;
        }

        public IEnumerable<CrewCard> All()
        {
            foreach (CrewCard card in cards)
                if (card != null) yield return card;
        }

        public IEnumerable<int> BelowSlots()
        {
            for (int i = AboveCount; i < cards.Length; i++) yield return i;
        }

        public int EmptyBelowCount()
        {
            int n = 0;
            for (int i = AboveCount; i < cards.Length; i++)
                if (cards[i] == null) n++;
            return n;
        }

        private void CheckSlot(int slot)
        {
            if (slot < 0 || slot >= cards.Length)
                throw new ArgumentOutOfRangeException(nameof(slot), "Slot ciurma inesistente: " + slot);
        }
    }
}
