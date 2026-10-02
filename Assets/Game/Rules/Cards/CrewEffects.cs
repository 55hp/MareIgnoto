using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Config;

namespace hp55games.MareIgnoto.Rules.Cards
{
    /// <summary>
    /// Regole trasversali sulle carte crew, usate da tutti gli effetti: quante volte vale un effetto (R-013, R-015)
    /// e il valore di una carta per ordine turno e Commercio (R-014).
    /// </summary>
    public static class CrewEffects
    {
        /// <summary>
        /// Quante volte vale l'effetto di <paramref name="kind"/> per chi ha queste carte sopra coperta (una voce per slot, null se vuoto).
        /// Gli effetti sono cumulabili anche tra copie dello stesso rango (R-013). Un Jolly sopra coperta
        /// duplica l'effetto dell'altra carta sopra coperta; da solo o accanto all'altro Jolly non fa nulla (R-015).
        /// Il Medico non ha effetto sopra coperta (R-011). Per <see cref="CrewCardId.Jolly"/> restituisce quanti Jolly ci sono.
        /// </summary>
        public static int EffectiveCount(IReadOnlyList<CrewCard> above, CrewCardId kind)
        {
            int jokers = 0;
            int copies = 0;
            foreach (CrewCard card in above)
            {
                if (card == null) continue;
                if (card.IsJoker) jokers++;
                else if (card.Kind == kind) copies++;
            }

            if (kind == CrewCardId.Jolly) return jokers;
            if (CrewCatalog.EffectIsBelowDeck(kind)) return 0;

            // Con 2 slot sopra coperta un Jolly ha un solo "altro slot" da duplicare.
            return copies + jokers * copies;
        }

        /// <summary>Valore per l'ordine turno (R-014); slot vuoto = 0.</summary>
        public static int TurnOrderValue(CrewCard card, RulesConfig config)
        {
            if (card == null) return 0;
            if (card.IsJoker) return config.jokerTurnOrderValue;
            if (CrewCatalog.IsCourt(card.Rank)) return config.courtTurnOrderValues[card.Rank - CrewCatalog.FirstCourtRank];
            return card.Rank;
        }

        /// <summary>Valore Commercio (R-014, R-094); slot vuoto = 0.</summary>
        public static int CommerceValue(CrewCard card, RulesConfig config)
        {
            if (card == null) return 0;
            if (card.IsJoker) return config.jokerCommerceValue;
            if (CrewCatalog.IsCourt(card.Rank)) return config.courtCommerceValue;
            return card.Rank;
        }
    }
}
