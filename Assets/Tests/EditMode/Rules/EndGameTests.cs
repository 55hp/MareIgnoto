using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Engine;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>
    /// Fine partita e punteggio (02_regole.md §9) sul layout v5. Isola Sacra: M12, L12, N12, M11, M13; vento verso N.
    /// p0 parte da K12 verso E ed entra in L12 al passo 1; le altre navi restano ferme (rotta S, contro vento) salvo dove
    /// indicato.
    /// </summary>
    public class EndGameTests
    {
        private static RoundScenario Arrival(int players = 3)
        {
            RoundScenario s = RoundScenario.Create(players).At(0, 10, 12).At(1, 2, 2);
            if (players > 2) s.At(2, 16, 16);
            s.Order(Enumerable.Range(0, players).ToArray());
            foreach (PlayerState p in s.State.Players) p.Coins = 0;
            s.State.Treasure = 0;
            return s;
        }

        private static Heading[] Headings(int players, params Heading[] first)
        {
            var headings = Enumerable.Repeat(Heading.S, players).ToArray();
            first.CopyTo(headings, 0);
            return headings;
        }

        [Test]
        public void EnteringTheSacredIslandEndsTheGameAfterTheCourtesyRound_R140_R142()
        {
            RoundScenario s = Arrival();
            s.Order(1, 0, 2);
            List<GameEvent> events = s.PlayRound(Headings(3, Heading.E));

            Assert.IsTrue(s.Session.IsOver);
            Assert.IsNull(s.Session.Pending);
            Assert.AreEqual(GameEndReason.SacredIsland, s.Session.Result.Reason);
            Assert.IsFalse(s.Session.Result.EndedByRoundLimit);
            Assert.AreEqual(1, s.Session.Result.RoundsPlayed);

            int entered = events.FindIndex(e => e is SacredIslandEnteredEvent);
            List<int> turns = events.Skip(entered).OfType<TurnStartedEvent>().Select(e => e.Player).ToList();
            CollectionAssert.AreEqual(new[] { 1, 0, 2 }, turns, "la Fase 2 del round si gioca per tutti");
            Assert.AreEqual(TurnSkipReason.SacredIsland, events.OfType<TurnSkippedEvent>().Single(e => e.Player == 0).Reason);

            Assert.IsInstanceOf<GameEndedEvent>(events.Last());
            Assert.AreEqual(3, events.OfType<FinalScoreEvent>().Count());
        }

        [Test]
        public void TheShipThatEntersTakesTheTreasure_R140()
        {
            RoundScenario s = Arrival();
            s.State.Treasure = 7;
            List<GameEvent> events = s.PlayRound(Headings(3, Heading.E));

            Assert.AreEqual(7, s.P(0).Coins);
            Assert.AreEqual(0, s.State.Treasure);
            Assert.AreEqual(s.Config.treasureTokens, s.P(0).BountyFrom(BountyReason.Treasure));
            CollectionAssert.AreEqual(new[] { 0 }, s.Session.Result.TreasureTakers);
            TreasureTakenEvent taken = events.OfType<TreasureTakenEvent>().Single();
            Assert.AreEqual(1, taken.Step);
            Assert.AreEqual(0, taken.CoinsLost);
            Assert.Less(events.IndexOf(taken), events.IndexOf(events.OfType<PhaseStartedEvent>().Last()), "subito dopo il movimento");
        }

        [Test]
        public void TheLowestStepTakesTheTreasure_R141()
        {
            // p1 da M9 verso N col vento a favore: velocità 2, entra in M11 al passo 2.
            RoundScenario s = Arrival();
            s.At(1, 12, 9);
            s.State.Treasure = 6;
            List<GameEvent> events = s.PlayRound(Headings(3, Heading.E, Heading.N));

            Assert.AreEqual(2, events.OfType<SacredIslandEnteredEvent>().Count());
            CollectionAssert.AreEqual(new[] { 0 }, s.Session.Result.TreasureTakers);
            Assert.AreEqual(6, s.P(0).Coins);
            Assert.AreEqual(0, s.P(1).BountyFrom(BountyReason.Treasure));
        }

        [Test]
        public void ATieOnTheStepSplitsTheCoinsAndGivesEachTheToken_R141()
        {
            // p1 da O12 verso O entra in N12 al passo 1, come p0.
            RoundScenario s = Arrival();
            s.At(1, 14, 12);
            s.State.Treasure = 7;
            List<GameEvent> events = s.PlayRound(Headings(3, Heading.E, Heading.O));

            CollectionAssert.AreEqual(new[] { 0, 1 }, s.Session.Result.TreasureTakers);
            Assert.AreEqual(3, s.P(0).Coins);
            Assert.AreEqual(3, s.P(1).Coins);
            Assert.AreEqual(1, events.OfType<TreasureTakenEvent>().Single().CoinsLost, "il resto va perso");
            Assert.AreEqual(s.Config.treasureTokens, s.P(0).BountyFrom(BountyReason.Treasure));
            Assert.AreEqual(s.Config.treasureTokens, s.P(1).BountyFrom(BountyReason.Treasure));
        }

        /// <summary>
        /// Abbordaggio in L11, che tocca tre celle dell'Isola Sacra (L12 a N, M12 a NE, M11 a E). Col vento verso NO:
        /// p0 da K11 verso E, p1 da L10 verso N, con 3 navi anche p2 da K10 verso NE. I tiri si accodano nell'ordine di turno.
        /// </summary>
        private static RoundScenario BoardingNextToTheIsland(int players)
        {
            RoundScenario s = Arrival(players).At(0, 10, 11).At(1, 11, 10).Wind(Heading.NO);
            if (players > 2) s.At(2, 10, 10);
            return s;
        }

        [Test]
        public void AShipRepositionedOntoTheSacredIslandTakesTheTreasureAndEndsTheGame_R073a_R140()
        {
            RoundScenario s = BoardingNextToTheIsland(2);
            s.State.Treasure = 5;
            s.Random.Enqueue(1, 5); // p0 verso N in L12 (Isola Sacra), p1 verso S in L10
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.N });

            Assert.IsTrue(events.OfType<SacredIslandEnteredEvent>().Single().ByBoarding);
            TreasureTakenEvent taken = events.OfType<TreasureTakenEvent>().Single();
            Assert.IsTrue(taken.ByBoarding);
            CollectionAssert.AreEqual(new[] { 0 }, s.Session.Result.TreasureTakers);
            Assert.AreEqual(5, s.P(0).Coins);
            Assert.AreEqual(GameEndReason.SacredIsland, s.Session.Result.Reason);
        }

        [Test]
        public void AnArrivalByMovementBeatsAnArrivalByBoarding_R141()
        {
            // p2 entra in movimento da O12 verso O, in N12 al passo 1; p0 arriva dopo, per Abbordaggio.
            RoundScenario s = BoardingNextToTheIsland(3).At(2, 14, 12);
            s.State.Treasure = 5;
            s.Random.Enqueue(1, 5);
            s.PlayRound(new[] { Heading.E, Heading.N, Heading.O });

            CollectionAssert.AreEqual(new[] { 2 }, s.Session.Result.TreasureTakers);
            Assert.AreEqual(0, s.P(0).BountyFrom(BountyReason.Treasure));
            Assert.AreEqual(Map.CellKind.SacredIsland, s.State.Map.KindAt(s.P(0).Position), "p0 è comunque sull'Isola Sacra");
        }

        [Test]
        public void BoardingArrivalsTieAndSplitTheTreasureAlsoWithThreeShips_R071_R141()
        {
            RoundScenario s = BoardingNextToTheIsland(3);
            s.State.Treasure = 7;
            s.Random.Enqueue(1, 2, 5); // p0 in L12, p1 in M12: Isola Sacra; p2 in L10
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.N, Heading.NE });

            Assert.AreEqual(3, events.OfType<BoardingStartedEvent>().Single().Players.Count);
            CollectionAssert.AreEqual(new[] { 0, 1 }, s.Session.Result.TreasureTakers);
            Assert.AreEqual(3, s.P(0).Coins);
            Assert.AreEqual(3, s.P(1).Coins);
            Assert.AreEqual(1, events.OfType<TreasureTakenEvent>().Single().CoinsLost);
        }

        [Test]
        public void MeteoCardsInTheCourtesyRoundArePaidButTheCoinsLeaveTheGame_R142()
        {
            RoundScenario s = Arrival(2);
            s.P(1).Coins = 10;
            s.State.Treasure = 4;
            s.Hand(1, PirateCardId.InvocazioneGartya);
            List<GameEvent> events = s.PlayRound(Headings(2, Heading.E),
                RoundScenario.Pick(RoundScenario.Opt<PlayCardOption>(1, o => o.Card.Id == PirateCardId.InvocazioneGartya)));

            Assert.AreEqual(10 - s.Config.PirateCost(PirateCardId.InvocazioneGartya), s.P(1).Coins, "pagata");
            Assert.AreEqual(0, s.State.Treasure, "il Tesoro era già stato preso");
            Assert.AreEqual(4, s.P(0).Coins);
            Assert.AreEqual(1, events.OfType<TreasureChangedEvent>().Count());
        }

        [Test]
        public void TheFinalBountyAddsEverySource_R143()
        {
            RoundScenario s = Arrival(2);
            RulesConfig cfg = s.Config;
            s.Bounty(0, BountyReason.Battle, 2);
            s.Bounty(0, BountyReason.Mission, 3);
            s.P(0).Coins = 2 * cfg.coinsPerToken - 1;
            s.State.Treasure = 1;
            s.Mission(0, MissionId.Barbarossa);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Mozzo);
            s.Crew(0, s.BelowSlot(1), CrewCardId.Mozzo);
            s.PlayRound(Headings(2, Heading.E));

            PlayerScore score = s.Session.Result.ScoreOf(0);
            Assert.AreEqual(2, score.BattleTokens);
            Assert.AreEqual(3, score.MissionTokens);
            Assert.AreEqual(cfg.treasureTokens, score.TreasureTokens);
            Assert.AreEqual(2 * cfg.coinsPerToken, score.Coins);
            Assert.AreEqual(2, score.CoinTokens);
            Assert.AreEqual(1, score.IncompleteMissions);
            Assert.AreEqual(cfg.incompleteMissionPenalty, score.MissionPenalty);
            Assert.AreEqual(PokerHand.Pair, score.Poker.Hand);
            Assert.AreEqual(2 + 3 + cfg.treasureTokens + 2 - cfg.incompleteMissionPenalty + cfg.PokerTokens(PokerHand.Pair), score.Total);
            Assert.IsTrue(score.TookTreasure);
            CollectionAssert.AreEqual(new[] { 0 }, s.Session.Result.Winners);
        }

        [Test]
        public void ATieWithTheTreasureTakerGoesToTheTreasureTaker_R147()
        {
            RoundScenario s = Arrival();
            s.Bounty(1, BountyReason.Battle, s.Config.treasureTokens);
            s.PlayRound(Headings(3, Heading.E));

            GameResult result = s.Session.Result;
            Assert.AreEqual(result.ScoreOf(0).Total, result.ScoreOf(1).Total);
            CollectionAssert.AreEqual(new[] { 0 }, result.Winners);
            Assert.AreEqual(2, result.ScoreOf(1).Rank);
            Assert.AreEqual(3, result.ScoreOf(2).Rank);
        }

        [Test]
        public void OtherTiesRemain_R147()
        {
            RoundScenario s = Arrival();
            s.Bounty(1, BountyReason.Battle, 5);
            s.Bounty(2, BountyReason.Battle, 5);
            s.PlayRound(Headings(3, Heading.E));

            GameResult result = s.Session.Result;
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, result.Winners);
            Assert.AreEqual(3, result.ScoreOf(0).Rank);
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, result.Ranking.Select(r => r.Player));
        }

        [Test]
        public void TheRoundLimitStillScoresTheGame_R150()
        {
            RoundScenario s = RoundScenario.Create(2, new RulesConfig { maxRounds = 1 }).At(0, 2, 2).At(1, 16, 16).Order(0, 1);
            s.PlayTurns();

            GameResult result = s.Session.Result;
            Assert.IsTrue(result.EndedByRoundLimit);
            Assert.IsEmpty(result.TreasureTakers);
            Assert.AreEqual(2, result.Ranking.Count);
        }
    }
}
