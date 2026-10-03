using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Engine;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>Punteggio poker della ciurma (R-144–R-146, 03_contenuti.md §4).</summary>
    public class ScoringTests
    {
        private static readonly RulesConfig Config = new RulesConfig();
        private int nextUid = 1;

        private const CrewSuit H = CrewSuit.Hearts, D = CrewSuit.Diamonds, C = CrewSuit.Clubs, S = CrewSuit.Spades;

        private CrewCard Card(int rank, CrewSuit suit) => new CrewCard(nextUid++, CrewCatalog.KindForRank(rank), rank, suit);

        private CrewCard Joker() => new CrewCard(nextUid++, CrewCardId.Jolly, 0, CrewSuit.None);

        /// <summary>Le carte sono la ciurma; le prime due (se ci sono) stanno sopra coperta.</summary>
        private static PokerScore Poker(params CrewCard[] crew) =>
            Scoring.Poker(crew, crew.Take(Config.slotsAbove).ToArray(), Config);

        private static PokerScore PokerAllBelow(params CrewCard[] crew) => Scoring.Poker(crew, new CrewCard[0], Config);

        private static IEnumerable<TestCaseData> Hands()
        {
            yield return new TestCaseData(new[] { 1, 3, 5, 7, 9 }, new[] { H, D, C, S, H }, PokerHand.HighCard).SetName("CartaAlta");
            yield return new TestCaseData(new[] { 2, 2, 5, 7, 9 }, new[] { H, D, C, S, H }, PokerHand.Pair).SetName("Coppia");
            yield return new TestCaseData(new[] { 2, 2, 5, 5, 9 }, new[] { H, D, C, S, H }, PokerHand.TwoPair).SetName("DoppiaCoppia");
            yield return new TestCaseData(new[] { 2, 2, 2, 7, 9 }, new[] { H, D, C, S, H }, PokerHand.ThreeOfAKind).SetName("Tris");
            yield return new TestCaseData(new[] { 1, 2, 3, 4, 5 }, new[] { H, D, C, S, H }, PokerHand.Straight).SetName("ScalaAssoBasso");
            yield return new TestCaseData(new[] { 10, 11, 12, 13, 1 }, new[] { H, D, C, S, H }, PokerHand.Straight).SetName("ScalaAssoAlto");
            yield return new TestCaseData(new[] { 12, 13, 1, 2, 3 }, new[] { H, D, C, S, H }, PokerHand.HighCard).SetName("NienteScalaCheGira");
            yield return new TestCaseData(new[] { 1, 3, 5, 7, 9 }, new[] { H, H, H, H, H }, PokerHand.Flush).SetName("Colore");
            yield return new TestCaseData(new[] { 2, 2, 2, 5, 5 }, new[] { H, D, C, S, H }, PokerHand.FullHouse).SetName("Full");
            yield return new TestCaseData(new[] { 2, 2, 2, 2, 9 }, new[] { H, D, C, S, H }, PokerHand.FourOfAKind).SetName("Poker");
            yield return new TestCaseData(new[] { 3, 4, 5, 6, 7 }, new[] { S, S, S, S, S }, PokerHand.StraightFlush).SetName("ScalaReale");
            yield return new TestCaseData(new[] { 1, 2, 3, 4, 5 }, new[] { C, C, C, C, C }, PokerHand.StraightFlush).SetName("ScalaRealeAssoBasso");
            yield return new TestCaseData(new[] { 10, 11, 12, 13, 1 }, new[] { D, D, D, D, D }, PokerHand.RoyalStraightFlush).SetName("ScalaRealeMassima");
            yield return new TestCaseData(new[] { 2, 3, 4, 5 }, new[] { H, H, H, H }, PokerHand.HighCard).SetName("ScalaEColoreVoglionoCinqueCarte");
            yield return new TestCaseData(new[] { 8, 8, 8 }, new[] { H, D, C }, PokerHand.ThreeOfAKind).SetName("TrisConTreCarte");
            yield return new TestCaseData(new int[0], new CrewSuit[0], PokerHand.HighCard).SetName("CiurmaVuota");
        }

        [TestCaseSource(nameof(Hands))]
        public void EveryCombinationIsRecognised_R144(int[] ranks, CrewSuit[] suits, PokerHand expected)
        {
            PokerScore score = PokerAllBelow(ranks.Select((r, i) => Card(r, suits[i])).ToArray());
            Assert.AreEqual(expected, score.Hand);
            Assert.AreEqual(Config.PokerTokens(expected), score.Score);
        }

        [Test]
        public void OneJokerIsTheBestWildCardAndHalvesTheScore_R145()
        {
            PokerScore four = PokerAllBelow(Card(13, H), Card(13, D), Card(13, C), Card(2, S), Joker());
            Assert.AreEqual(PokerHand.FourOfAKind, four.Hand);
            Assert.IsTrue(four.HalvedByJoker);
            Assert.AreEqual(Config.PokerTokens(PokerHand.FourOfAKind) / Config.singleJokerPokerDivisor, four.Score);

            PokerScore royal = PokerAllBelow(Card(10, H), Card(11, H), Card(12, H), Card(13, H), Joker());
            Assert.AreEqual(PokerHand.RoyalStraightFlush, royal.Hand);
            Assert.AreEqual(Config.PokerTokens(PokerHand.RoyalStraightFlush) / Config.singleJokerPokerDivisor, royal.Score);

            PokerScore odd = PokerAllBelow(Card(5, H), Joker());
            Assert.AreEqual(PokerHand.Pair, odd.Hand, "il Jolly completa anche una ciurma corta");
            Assert.AreEqual(Config.PokerTokens(PokerHand.Pair) / Config.singleJokerPokerDivisor, odd.Score, "per difetto");
        }

        [Test]
        public void BothJokersGiveAFixedScore_R145()
        {
            PokerScore score = PokerAllBelow(Card(2, H), Card(2, D), Card(2, C), Joker(), Joker());
            Assert.IsTrue(score.JokerPair);
            Assert.AreEqual(Config.jokerPairPokerScore, score.Score, "la tabella non conta");
        }

        [Test]
        public void TheNostromoAboveDeckDoublesThePokerScore_R146()
        {
            CrewCard nostromo = Card(9, H);
            PokerScore above = Poker(nostromo, Card(2, D), Card(9, D), Card(9, C), Card(5, S));
            Assert.AreEqual(PokerHand.ThreeOfAKind, above.Hand);
            Assert.AreEqual(Config.nostromoPokerMultiplier, above.NostromoMultiplier);
            Assert.AreEqual(Config.PokerTokens(PokerHand.ThreeOfAKind) * Config.nostromoPokerMultiplier, above.Score);

            PokerScore below = PokerAllBelow(Card(9, S), Card(2, D), Card(9, D), Card(9, C), Card(5, S));
            Assert.AreEqual(1, below.NostromoMultiplier, "sotto coperta non vale");

            PokerScore two = Poker(Card(9, H), Card(9, S), Card(9, D), Card(9, C), Card(5, S));
            Assert.AreEqual(Config.nostromoPokerMultiplier * Config.nostromoPokerMultiplier, two.NostromoMultiplier, "per copia");
        }

        [Test]
        public void TheNostromoDoublesAfterTheJokerHalving_R145_R146()
        {
            // Nostromo e Jolly sopra coperta: il Jolly è la carta jolly del poker (R-145) e duplica il Nostromo (R-015).
            PokerScore score = Poker(Card(9, H), Joker(), Card(9, D), Card(9, C), Card(5, S));
            Assert.AreEqual(PokerHand.FourOfAKind, score.Hand);
            int halved = Config.PokerTokens(PokerHand.FourOfAKind) / Config.singleJokerPokerDivisor;
            Assert.AreEqual(halved, score.AfterJokers);
            Assert.AreEqual(halved * Config.nostromoPokerMultiplier * Config.nostromoPokerMultiplier, score.Score);
        }
    }
}
