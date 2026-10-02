using System;
using System.Collections.Generic;

namespace hp55games.MareIgnoto.Rules.Cards
{
    /// <summary>Struttura fissa del mazzo Crew: ranghi, semi, quale carta corrisponde a quale rango.</summary>
    public static class CrewCatalog
    {
        public const int MinRank = 1;
        public const int MaxRank = 13;
        /// <summary>Rango di J: da qui a MaxRank sono J, Q, K.</summary>
        public const int FirstCourtRank = 11;

        private static readonly CrewSuit[] Suits =
        {
            CrewSuit.Hearts, CrewSuit.Diamonds, CrewSuit.Clubs, CrewSuit.Spades,
        };

        /// <summary>I 4 semi dei ranghi 1–13 (il Jolly non ha seme).</summary>
        public static IReadOnlyList<CrewSuit> SuitsInDeck => Suits;

        public static CrewCardId KindForRank(int rank)
        {
            if (rank < MinRank || rank > MaxRank)
                throw new ArgumentOutOfRangeException(nameof(rank), "Il rango è tra 1 e 13.");
            return (CrewCardId)(rank - 1);
        }

        /// <summary>J, Q, K: le carte di gittata (R-100).</summary>
        public static bool IsCourt(int rank) => rank >= FirstCourtRank && rank <= MaxRank;

        /// <summary>
        /// Il Medico di bordo è l'unica carta il cui effetto vale sotto coperta (R-011);
        /// tutte le altre hanno effetto solo sopra coperta.
        /// </summary>
        public static bool EffectIsBelowDeck(CrewCardId kind) => kind == CrewCardId.Medico;
    }

    public static class PirateCatalog
    {
        public static bool IsWeather(PirateCardId id)
        {
            switch (id)
            {
                case PirateCardId.SupplicaGartya:
                case PirateCardId.InvocazioneGartya:
                case PirateCardId.IraGartya:
                case PirateCardId.FavoreGartya:
                case PirateCardId.VentoInPoppa:
                case PirateCardId.RafficaCanaglia:
                    return true;
                default:
                    return false;
            }
        }
    }
}
