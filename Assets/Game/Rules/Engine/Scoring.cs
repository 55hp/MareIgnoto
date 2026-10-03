using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>Punteggio finale (R-143–R-147) e combinazioni poker (03_contenuti.md §4).</summary>
    internal static class Scoring
    {
        /// <summary>La taglia di ogni giocatore, con classifica (R-147).</summary>
        public static List<PlayerScore> Rank(GameState state)
        {
            RulesConfig cfg = state.Config;
            var scores = state.Players.Select(p => new PlayerScore(
                p.Id,
                p.BountyFrom(BountyReason.Battle),
                p.BountyFrom(BountyReason.Mission),
                p.BountyFrom(BountyReason.Treasure),
                p.Coins,
                p.Coins / cfg.coinsPerToken,
                p.Missions.Count,
                p.Missions.Count * cfg.incompleteMissionPenalty,
                Poker(Enumerable.Range(0, p.Crew.Count).Select(s => p.Crew[s]).Where(c => c != null).ToList(), p.CrewAbove, cfg),
                state.TreasureTakers.Contains(p.Id))).ToList();

            // R-147: taglia più alta; a parità vince chi ha preso il Tesoro, altrimenti la parità resta.
            foreach (PlayerScore score in scores)
                score.Rank = 1 + scores.Count(other => Beats(other, score));
            return scores.OrderBy(s => s.Rank).ThenBy(s => s.Player).ToList();
        }

        private static bool Beats(PlayerScore a, PlayerScore b) =>
            a.Total > b.Total || (a.Total == b.Total && a.TookTreasure && !b.TookTreasure);

        /// <summary>
        /// R-144–R-146 per le carte della ciurma <paramref name="crew"/> (sopra e sotto coperta) e quelle sopra coperta
        /// <paramref name="above"/> (per il Nostromo).
        /// </summary>
        public static PokerScore Poker(IReadOnlyList<CrewCard> crew, IReadOnlyList<CrewCard> above, RulesConfig cfg)
        {
            int jokers = crew.Count(c => c.IsJoker);
            var cards = crew.Where(c => !c.IsJoker).Select(c => new PokerCard(c.Rank, c.Suit)).ToList();

            PokerHand hand;
            int afterJokers;
            bool pair = jokers >= 2, halved = false;
            if (jokers == 1)
            {
                // R-145: il Jolly diventa la carta che dà la combinazione migliore, e il punteggio si dimezza.
                PokerHand withJoker = PokerHand.HighCard;
                foreach (int rank in Enumerable.Range(CrewCatalog.MinRank, CrewCatalog.MaxRank - CrewCatalog.MinRank + 1))
                    foreach (CrewSuit suit in CrewCatalog.SuitsInDeck)
                    {
                        var withCard = new List<PokerCard>(cards) { new PokerCard(rank, suit) };
                        withJoker = Better(withJoker, BestHand(withCard, cfg), cfg);
                    }

                // R-146: se il Jolly non fa parte della combinazione (le altre carte valgono almeno quanto la combinazione
                // col Jolly dimezzata) il dimezzamento non si applica.
                PokerHand without = BestHand(cards, cfg);
                int halvedScore = cfg.PokerTokens(withJoker) / cfg.singleJokerPokerDivisor;
                halved = halvedScore > cfg.PokerTokens(without);
                hand = halved ? withJoker : without;
                afterJokers = halved ? halvedScore : cfg.PokerTokens(without);
            }
            else
            {
                hand = BestHand(cards, cfg);
                afterJokers = pair ? cfg.jokerPairPokerScore : cfg.PokerTokens(hand);
            }

            int multiplier = 1;
            for (int i = CrewEffects.EffectiveCount(above, CrewCardId.Nostromo); i > 0; i--) multiplier *= cfg.nostromoPokerMultiplier;

            return new PokerScore(crew.ToArray(), hand, cfg.PokerTokens(hand), jokers, pair, halved, afterJokers, multiplier);
        }

        /// <summary>
        /// La combinazione che vale più segnalini tra quelle che le carte formano (a parità la più alta). Scala e colore
        /// richiedono 5 carte; il Mozzo (1) è l'asso, basso (1-2-3-4-5) o alto (10-J-Q-K-1), senza scale che girano.
        /// </summary>
        public static PokerHand BestHand(IReadOnlyList<PokerCard> cards, RulesConfig cfg)
        {
            PokerHand best = PokerHand.HighCard;
            foreach (PokerHand hand in Formed(cards)) best = Better(best, hand, cfg);
            return best;
        }

        private static PokerHand Better(PokerHand a, PokerHand b, RulesConfig cfg)
        {
            int ta = cfg.PokerTokens(a), tb = cfg.PokerTokens(b);
            return tb > ta || (tb == ta && b > a) ? b : a;
        }

        /// <summary>Tutte le combinazioni formate dalle carte (una mano può formarne più di una: il full contiene un tris).</summary>
        private static IEnumerable<PokerHand> Formed(IReadOnlyList<PokerCard> cards)
        {
            yield return PokerHand.HighCard;
            List<int> counts = cards.GroupBy(c => c.Rank).Select(g => g.Count()).OrderByDescending(n => n).ToList();
            int first = counts.Count > 0 ? counts[0] : 0;
            int second = counts.Count > 1 ? counts[1] : 0;

            if (first >= 2) yield return PokerHand.Pair;
            if (first >= 2 && second >= 2) yield return PokerHand.TwoPair;
            if (first >= 3) yield return PokerHand.ThreeOfAKind;
            if (first >= 3 && second >= 2) yield return PokerHand.FullHouse;
            if (first >= 4) yield return PokerHand.FourOfAKind;

            if (cards.Count != 5) yield break;
            bool flush = cards.All(c => c.Suit == cards[0].Suit);
            var ranks = new HashSet<int>(cards.Select(c => c.Rank));
            bool highAce = ranks.SetEquals(HighAceStraight);
            bool straight = ranks.Count == 5 && (ranks.Max() - ranks.Min() == 4 || highAce);

            if (flush) yield return PokerHand.Flush;
            if (straight) yield return PokerHand.Straight;
            if (flush && straight) yield return PokerHand.StraightFlush;
            if (flush && highAce) yield return PokerHand.RoyalStraightFlush;
        }

        /// <summary>10-J-Q-K-1: la scala con l'asso alto.</summary>
        private static readonly int[] HighAceStraight =
            Enumerable.Range(CrewCatalog.MaxRank - 3, 4).Concat(new[] { CrewCatalog.MinRank }).ToArray();
    }

    /// <summary>Rango e seme di una carta per il poker (anche quella impersonata dal Jolly).</summary>
    internal struct PokerCard
    {
        public int Rank { get; }
        public CrewSuit Suit { get; }

        public PokerCard(int rank, CrewSuit suit)
        {
            Rank = rank;
            Suit = suit;
        }
    }
}
