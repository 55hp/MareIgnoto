using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Bots;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Engine;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.State;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>Struttura del round (02_regole.md §4), ordine di turno dal round 2 e simulazione (08 §3).</summary>
    public class RoundTests
    {
        private static readonly Heading[] East3 = { Heading.E, Heading.E, Heading.E };

        /// <summary>Tre navi lontane tra loro, in zone Normali: nessun meteo, nessun Abbordaggio.</summary>
        private static RoundScenario Apart(int players = 3)
        {
            RoundScenario s = RoundScenario.Create(players).At(0, 2, 2).At(1, 16, 16);
            if (players > 2) s.At(2, 2, 16);
            return s.Order(Enumerable.Range(0, players).ToArray());
        }

        [Test]
        public void Phase1RunsHeadingsThenWeatherThenMovementThenBoarding_R043()
        {
            RoundScenario s = RoundScenario.Create(2).At(0, 3, 7).At(1, 5, 7).Order(0, 1).ZoneAt(3, 7, WeatherState.RoughSea);
            s.Random.Enqueue(8, 1, 5);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.O });

            int revealed = events.FindIndex(e => e is HeadingsRevealedEvent);
            int weather = events.FindIndex(e => e is WeatherAppliedEvent);
            int moveStart = events.FindIndex(e => e is MovementStartedEvent);
            int moveEnd = events.FindIndex(e => e is MovementEndedEvent);
            int boarding = events.FindIndex(e => e is BoardingStartedEvent);
            int phase2 = events.FindIndex(e => e is PhaseStartedEvent p && p.Phase == GamePhase.Active);
            Assert.That(new[] { revealed, weather, moveStart, moveEnd, boarding, phase2 }, Is.Ordered);
            Assert.That(revealed, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void NoCardIsPlayedInPhase1_R044()
        {
            var phase1Kinds = new HashSet<DecisionKind>
            {
                DecisionKind.ChooseHeading, DecisionKind.WeatherRotation, DecisionKind.WeatherPirateLoss,
                DecisionKind.StormCrewLoss, DecisionKind.CrossingCell, DecisionKind.BoardingLoss,
                DecisionKind.UseMedico, DecisionKind.BoardingReposition,
            };

            GameSession session = TestSupport.Start(4, 31, new RulesConfig { maxRounds = 30 });
            var bot = new RandomBot(new SeededRandom(31));
            while (session.Pending != null)
            {
                if (session.State.Phase == GamePhase.Preparation)
                    Assert.IsTrue(phase1Kinds.Contains(session.Pending.Kind), session.Pending.ToString());
                session.Submit(bot.Choose(session.Pending));
            }
        }

        [Test]
        public void ALeisureShipChoosesNoHeadingSuffersNoWeatherAndDoesNotMove_R045()
        {
            RoundScenario s = RoundScenario.Create(2).Order(0, 1);
            int resting = 1 - s.Session.Pending.Player; // la decisione già in attesa è dell'altro giocatore
            s.At(resting, 14, 5).At(1 - resting, 2, 2).ZoneAt(14, 6, WeatherState.Storm);
            s.P(resting).LeisureRound = s.Session.State.Round;
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.E });

            Assert.IsFalse(s.Decisions.Any(d => d.Kind == DecisionKind.ChooseHeading && d.Player == resting));
            Assert.IsNull(events.OfType<HeadingsRevealedEvent>().Single().Headings[resting]);
            Assert.AreEqual(0, events.OfType<MovementStartedEvent>().Single().Speeds[resting]);
            Assert.IsFalse(events.OfType<ShipMovedEvent>().Any(e => e.Player == resting));
            Assert.IsFalse(events.OfType<WeatherAppliedEvent>().Any(e => e.Player == resting));
            Assert.AreEqual(new Coord(14, 5), s.P(resting).Position);
        }

        [Test]
        public void FromRound2TheOrderIsByAscendingCrewSumWithEmptySlotsAndJokerAtZero_R050_R014()
        {
            RoundScenario s = Apart();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Culverin);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Saker);
            s.Crew(1, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Crew(1, s.AboveSlot(1), CrewCardId.Jolly);
            List<GameEvent> events = s.PlayRound(East3);

            var round2 = events.OfType<TurnOrderSetEvent>().Single(e => e.Round == 2);
            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, round2.Order);
            Assert.AreEqual(2, round2.Order[0], "slot vuoti = 0");
        }

        [Test]
        public void Round1KeepsTheOfferOrderInPhase2_R037_R050()
        {
            RoundScenario s = Apart().Order(1, 2, 0);
            List<GameEvent> events = s.PlayRound(East3);
            Assert.IsFalse(events.OfType<TurnOrderSetEvent>().Any(e => e.Round == 1));
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, events.OfType<TurnStartedEvent>().Where(e => e.Round == 1).Select(e => e.Player));
        }

        [Test]
        public void TiesGoToFewerCoins_R051()
        {
            RoundScenario s = Apart(2);
            s.P(0).Coins = 5;
            s.P(1).Coins = 3;
            List<GameEvent> events = s.PlayRound(East3);
            CollectionAssert.AreEqual(new[] { 1, 0 }, events.OfType<TurnOrderSetEvent>().Single(e => e.Round == 2).Order);
        }

        [Test]
        public void ThenToFewerPirateCards_R051()
        {
            RoundScenario s = Apart(2);
            s.P(0).Coins = 4;
            s.P(1).Coins = 4;
            s.Hand(0, PirateCardId.Bordata);
            s.Hand(1, PirateCardId.Bordata, PirateCardId.Parle);
            List<GameEvent> events = s.PlayRound(East3);
            CollectionAssert.AreEqual(new[] { 0, 1 }, events.OfType<TurnOrderSetEvent>().Single(e => e.Round == 2).Order);
        }

        [Test]
        public void ThenToTheHigherDieRerolledAmongTheTied_R051()
        {
            RoundScenario s = Apart(2);
            s.P(0).Coins = 4;
            s.P(1).Coins = 4;
            s.Random.Enqueue(5, 5, 2, 7);
            List<GameEvent> events = s.PlayRound(East3);

            CollectionAssert.AreEqual(new[] { 1, 0 }, events.OfType<TurnOrderSetEvent>().Single(e => e.Round == 2).Order);
            var rolls = events.OfType<DieRolledEvent>().Where(e => e.Reason == DiceReason.TurnOrderTiebreak).ToList();
            CollectionAssert.AreEqual(new[] { 5, 5, 2, 7 }, rolls.Select(r => r.Value));
        }

        [Test]
        public void Phase2RecomputesTheOrderAfterBoardingChangedTheCrew_R050()
        {
            // Inizio round 2: p1 (Mozzo, 1) prima di p0 (Culverin, 13). Nell'Abbordaggio p0 perde il Culverin:
            // in Fase 2 p0 ha 0 e passa davanti.
            RoundScenario s = Apart(2);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Culverin);
            s.Crew(1, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Hand(1, PirateCardId.Bordata);
            List<GameEvent> round1 = s.PlayRound(East3);
            CollectionAssert.AreEqual(new[] { 1, 0 }, round1.OfType<TurnOrderSetEvent>().Single(e => e.Round == 2).Order);

            s.At(0, 3, 7).At(1, 5, 7);
            s.Random.Enqueue(1, 5);
            List<GameEvent> round2 = s.PlayRound(new[] { Heading.E, Heading.O },
                RoundScenario.Prefer((d, o) => o is BoardingLossOption l && l.PirateCards.Count > 0));

            Assert.IsNull(s.P(0).Crew[s.AboveSlot(0)]);
            CollectionAssert.AreEqual(new[] { 0, 1 }, round2.OfType<TurnOrderSetEvent>().Single(e => e.Round == 2).Order);
            CollectionAssert.AreEqual(new[] { 0, 1 }, round2.OfType<TurnStartedEvent>().Select(e => e.Player));
        }

        [Test]
        public void TheCabinBoyPaysAtTheStartOfTheTurnAndTheJokerDoublesIt_R052()
        {
            RoundScenario s = Apart(2);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Crew(1, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Crew(1, s.AboveSlot(1), CrewCardId.Jolly);
            int coins0 = s.P(0).Coins, coins1 = s.P(1).Coins;
            List<GameEvent> events = s.PlayRound(East3);

            var paid = events.OfType<CoinsChangedEvent>().Where(e => e.Reason == CoinReason.CabinBoy).ToList();
            Assert.AreEqual(s.Config.cabinBoyCoins, paid.Single(e => e.Player == 0).Delta);
            Assert.AreEqual(2 * s.Config.cabinBoyCoins, paid.Single(e => e.Player == 1).Delta);
            Assert.AreEqual(coins0 + s.Config.cabinBoyCoins, s.P(0).Coins);
            Assert.AreEqual(coins1 + 2 * s.Config.cabinBoyCoins, s.P(1).Coins);
            int turnStart = events.FindIndex(e => e is TurnStartedEvent t && t.Player == 0);
            Assert.AreEqual(turnStart + 1, events.FindIndex(e => e is CoinsChangedEvent c && c.Player == 0));
        }

        [Test]
        public void OnTheBorderTheTurnEndsAtOnceWithoutCabinBoyCoins_R053_R052()
        {
            RoundScenario s = Apart(2).At(0, 0, 7);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            List<GameEvent> events = s.PlayRound(new[] { Heading.O, Heading.E });

            Assert.AreEqual(0, events.OfType<TurnSkippedEvent>().Single().Player);
            Assert.IsFalse(events.OfType<CoinsChangedEvent>().Any(e => e.Player == 0));
            Assert.AreEqual(1, events.OfType<TurnEndedEvent>().Count(e => e.Player == 0));
        }

        [Test]
        public void Phase2GivesEveryPlayerExactlyOneTurnInTurnOrder()
        {
            GameSession session = TestSupport.Start(4, 41, new RulesConfig { maxRounds = 10 });
            List<GameEvent> events = TestSupport.PlayToEnd(session, 41);

            for (int round = 1; round <= 10; round++)
            {
                var turns = events.OfType<TurnStartedEvent>().Where(e => e.Round == round).Select(e => e.Player).ToList();
                CollectionAssert.AreEquivalent(Enumerable.Range(0, 4), turns, "round " + round);
                IReadOnlyList<int> order = round == 1
                    ? events.OfType<TurnOrderSetEvent>().Single(e => e.Round == 1).Order
                    : events.OfType<TurnOrderSetEvent>().Last(e => e.Round == round).Order;
                CollectionAssert.AreEqual(order, turns, "round " + round);
            }
        }

        [Test]
        public void MaxRoundsEndsTheGame_R150()
        {
            var config = new RulesConfig { maxRounds = 3 };
            GameSession session = TestSupport.Start(2, 51, config);
            List<GameEvent> events = TestSupport.PlayToEnd(session, 51);

            Assert.IsTrue(session.IsOver);
            Assert.IsNull(session.Pending);
            Assert.IsTrue(session.Result.EndedByRoundLimit);
            Assert.AreEqual(config.maxRounds, session.Result.RoundsPlayed);
            Assert.AreEqual(config.maxRounds, events.OfType<GameEndedEvent>().Single().RoundsPlayed);
            Assert.AreEqual(config.maxRounds, events.OfType<RoundStartedEvent>().Count());
        }

        /// <summary>
        /// Criterio della spec 0002: 200 round con bot casuali a 2, 4 e 8 giocatori, invarianti verificati dopo ogni
        /// risposta (TestSupport.Play), senza eccezioni.
        /// </summary>
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        public void Simulation200RoundsWithRandomBots(int players)
        {
            var config = new RulesConfig { maxRounds = 200 };
            for (int seed = 1; seed <= 3; seed++)
            {
                GameSession session = TestSupport.Start(players, seed, config);
                List<GameEvent> events = TestSupport.PlayToEnd(session, seed);

                Assert.IsTrue(session.IsOver, "seed " + seed);
                Assert.AreEqual(config.maxRounds, session.Result.RoundsPlayed, "seed " + seed);
                Assert.Greater(events.OfType<ShipMovedEvent>().Count(), 0);
            }
        }

        /// <summary>Gate di 08 §3 su seed 1–1000: lungo, si lancia a mano.</summary>
        [Test, Explicit("Lungo: simulazione 1000 seed × 2/4/8 giocatori × 200 round")]
        public void Simulation1000Seeds()
        {
            var config = new RulesConfig { maxRounds = 200 };
            foreach (int players in new[] { 2, 4, 8 })
                for (int seed = 1; seed <= 1000; seed++)
                    TestSupport.PlayToEnd(TestSupport.Start(players, seed, config), seed);
        }
    }
}
