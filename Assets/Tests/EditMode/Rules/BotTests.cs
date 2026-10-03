using System;
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
using static hp55games.MareIgnoto.Rules.Tests.RoundScenario;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>Bot strategici (09_bot.md): strumenti comuni, profili Rush e Cacciatore, simulazione con posti misti.</summary>
    public class BotTests
    {
        private static Coord C(string name) => Coord.Parse(name);

        private static PlayerView View(RoundScenario s, int player) => s.Session.State.ViewFor(player);

        private static StrategicBot Bot(BotProfile profile, int seed = 1) => new StrategicBot(profile, new SeededRandom(seed));

        private static PendingDecision HeadingDecision(int player) =>
            new PendingDecision(1, DecisionKind.ChooseHeading, player, true, true,
                Enumerable.Range(0, HeadingExtensions.Count).Select(h => (DecisionOption)new HeadingOption((Heading)h)).ToList(), null);

        // ---- Velocità stimata (09 §3, R-060–R-064) ----

        [Test]
        public void EstimatedSpeedFollowsWindAndHelmsmen()
        {
            RoundScenario s = Create(2).At(0, 12, 5).Wind(Heading.N);
            RulesConfig cfg = s.Config;
            Assert.AreEqual(cfg.baseSpeed + cfg.windSpeedBonus, BotTools.EstimatedSpeed(View(s, 0), Heading.N));
            Assert.AreEqual(Math.Max(0, cfg.baseSpeed - cfg.windSpeedBonus), BotTools.EstimatedSpeed(View(s, 0), Heading.S));
            Assert.AreEqual(cfg.baseSpeed, BotTools.EstimatedSpeed(View(s, 0), Heading.E));

            s.Crew(0, s.AboveSlot(0), CrewCardId.Timoniere);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            Assert.AreEqual(cfg.baseSpeed + cfg.windSpeedBonus + 2 * cfg.helmsmanSpeedBonus, BotTools.EstimatedSpeed(View(s, 0), Heading.N),
                "Timoniere duplicato dal Jolly");

            s.At(0, 2, 5); // isola C5: niente vento in partenza
            Assert.AreEqual(cfg.baseSpeed + 2 * cfg.helmsmanSpeedBonus, BotTools.EstimatedSpeed(View(s, 0), Heading.N));
        }

        [Test]
        public void SimulatedMovementStopsOnLand()
        {
            GameMap map = GameMap.Create(TestSupport.StandardMap(), new RulesConfig());
            Assert.AreEqual(C("M7"), BotTools.SimulateMove(map, C("M5"), Heading.N, 2));
            Assert.AreEqual(C("L12"), BotTools.SimulateMove(map, C("J12"), Heading.E, 5), "si ferma sull'Isola Sacra");
            Assert.AreEqual(C("A12"), BotTools.SimulateMove(map, C("C12"), Heading.O, 5), "si ferma sulla cornice");
        }

        // ---- Rotta (09 §3) ----

        [Test]
        public void OnFreeSeaTheRouteHeadsForTheGoal()
        {
            RoundScenario s = Create(2).At(0, 12, 5).Wind(Heading.N); // M5, sotto l'Isola Sacra, vento a favore verso N
            var goals = BotTools.SacredIslandCells(s.State.Map);
            Assert.AreEqual(Heading.N, BotTools.ChooseHeading(View(s, 0), goals, BotProfile.Rush, new SeededRandom(1)));
        }

        [Test]
        public void AStormOnTheArrivalCellCostsThePenalty()
        {
            // K5 verso N (velocità 2) arriva in K7, nello spicchio R6.
            RoundScenario s = Create(2).At(0, 10, 5).Wind(Heading.N);
            var goals = BotTools.SacredIslandCells(s.State.Map);
            int[] calm = BotTools.HeadingCosts(View(s, 0), goals, BotProfile.Rush);
            s.Zone("R6", 5);
            int[] storm = BotTools.HeadingCosts(View(s, 0), goals, BotProfile.Rush);
            Assert.AreEqual(calm[(int)Heading.N] + BotProfile.Rush.StormPenalty, storm[(int)Heading.N]);
            s.Zone("R6", 1);
            int[] rough = BotTools.HeadingCosts(View(s, 0), goals, BotProfile.Rush);
            Assert.AreEqual(calm[(int)Heading.N] + BotProfile.Rush.RoughSeaPenalty, rough[(int)Heading.N]);
            Assert.AreEqual(calm[(int)Heading.O], storm[(int)Heading.O], "le altre rotte non cambiano");
        }

        // ---- Stima di punteggio (09 §3, R-143) ----

        [Test]
        public void ScoreEstimatesUseOnlyWhatIsVisible()
        {
            RoundScenario s = Create(2);
            RulesConfig cfg = s.Config;
            s.P(0).Coins = 15;
            s.P(1).Coins = 23;
            s.State.Treasure = 7;
            s.Bounty(1, BountyReason.Battle, 2);
            s.Mission(1, MissionId.Barbarossa);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Mozzo);
            s.Crew(0, s.BelowSlot(1), CrewCardId.Mozzo);   // la propria coppia sotto coperta conta
            s.Crew(1, s.BelowSlot(0), CrewCardId.Cuoco);
            s.Crew(1, s.BelowSlot(1), CrewCardId.Cuoco);   // quella degli altri no

            Assert.AreEqual(cfg.treasureTokens + (15 + 7) / cfg.coinsPerToken + cfg.PokerTokens(PokerHand.Pair),
                BotTools.OwnScoreIfTakingTreasure(View(s, 0)));
            Assert.AreEqual(2 + 23 / cfg.coinsPerToken - cfg.incompleteMissionPenalty, BotTools.MinKnownScore(s.Session.State, 1));
        }

        // ---- Rush ----

        private static RoundScenario RushNextToTheSacredIsland(int opponentTokens)
        {
            // p0 in M9, a due celle dall'Isola Sacra (M11), vento a favore: velocità 2 verso N. Monete e Tesoro a 0, nessuna
            // carta: "proprio" vale solo il segnalino del Tesoro, "altrui" i segnalini di p1.
            RoundScenario s = Create(2).At(0, 12, 9).At(1, 2, 2).Wind(Heading.N);
            s.P(0).Coins = 0;
            s.P(1).Coins = 0;
            s.State.Treasure = 0;
            s.Bounty(1, BountyReason.Battle, opponentTokens);
            return s;
        }

        private static Coord RushArrival(RoundScenario s)
        {
            PendingDecision decision = HeadingDecision(0);
            DecisionAnswer answer = Bot(BotProfile.Rush).Choose(decision, View(s, 0));
            Heading chosen = ((HeadingOption)decision.Options[answer.OptionIndex]).Heading;
            return BotTools.SimulateMove(s.State.Map, s.P(0).Position, chosen, BotTools.EstimatedSpeed(View(s, 0), chosen));
        }

        [Test]
        public void RushEntersTheSacredIslandOnATie_R147()
        {
            RoundScenario s = RushNextToTheSacredIsland(1);
            Assert.AreEqual(BotTools.OwnScoreIfTakingTreasure(View(s, 0)), BotTools.MinKnownScore(s.Session.State, 1), "pari");
            Assert.IsFalse(BotTools.WouldLoseTakingTreasure(View(s, 0)));
            Assert.AreEqual(CellKind.SacredIsland, s.State.Map.KindAt(RushArrival(s)));
        }

        [Test]
        public void RushStaysOutOfTheSacredIslandWhenStrictlyBehind()
        {
            RoundScenario s = RushNextToTheSacredIsland(2);
            Assert.IsTrue(BotTools.WouldLoseTakingTreasure(View(s, 0)));
            Assert.AreNotEqual(CellKind.SacredIsland, s.State.Map.KindAt(RushArrival(s)), "modalità cauta");
        }

        [Test]
        public void RushOffersEverythingAndTheHunterNothing()
        {
            GameSession session = TestSupport.Start(2, 4);
            var rush = Bot(BotProfile.Rush);
            var hunter = Bot(BotProfile.Hunter);
            List<GameEvent> events = TestSupport.Play(session,
                d => (d.Player == 0 ? rush : hunter).Choose(d, session.State.ViewFor(d.Player)));

            var offers = events.OfType<OfferMadeEvent>().ToDictionary(e => e.Player, e => e.Amount);
            Assert.AreEqual(new RulesConfig().startingCoins, offers[0]);
            Assert.AreEqual(0, offers[1]);
            Assert.AreEqual(1, session.State.Player(0).MissionsInHandCount, "ne tiene una sola");
        }

        [Test]
        public void RushNeitherAttacksNorPlaysCards()
        {
            RoundScenario s = Create(2).At(0, 5, 7).At(1, 6, 7).Order(0, 1);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.PescaFortunata);
            StrategicBot rush = Bot(BotProfile.Rush);
            List<GameEvent> events = s.PlayTurns(d => d.Player == 0 ? rush.Choose(d, View(s, 0)) : Default(d));
            Assert.IsFalse(events.OfType<AttackStartedEvent>().Any());
            Assert.IsFalse(events.OfType<CardPlayedEvent>().Any());
            Assert.AreEqual(2 + s.Config.seaDrawCount, s.P(0).Hand.Count, "pesca");
        }

        // ---- Cacciatore ----

        [Test]
        public void TheHunterAttacksATargetInRange()
        {
            RoundScenario s = Create(2).At(0, 5, 7).At(1, 6, 7).Order(0, 1);
            s.Hand(0, PirateCardId.Bordata);
            StrategicBot hunter = Bot(BotProfile.Hunter);
            List<GameEvent> events = s.PlayTurns(d => d.Player == 0 ? hunter.Choose(d, View(s, 0)) : Default(d));
            AttackStartedEvent attack = events.OfType<AttackStartedEvent>().Single();
            Assert.AreEqual(0, attack.Attacker);
            Assert.AreEqual(1, attack.Defender);
        }

        [Test]
        public void WithoutATargetTheHunterDraws()
        {
            RoundScenario s = Create(2).At(0, 5, 7).At(1, 16, 16).Order(0, 1);
            s.Hand(0, PirateCardId.Bordata);
            StrategicBot hunter = Bot(BotProfile.Hunter);
            List<GameEvent> events = s.PlayTurns(d => d.Player == 0 ? hunter.Choose(d, View(s, 0)) : Default(d));
            Assert.IsFalse(events.OfType<AttackStartedEvent>().Any());
            Assert.AreEqual(1 + s.Config.seaDrawCount, s.P(0).Hand.Count);
        }

        // ---- Scambio crew (09 §3) ----

        [Test]
        public void AUsefulSwapBringsUpAHigherPriorityCard()
        {
            RoundScenario s = Create(2).At(0, 5, 7).At(1, 16, 16).Order(0, 1);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Cuoco);       // non nella priorità del Rush
            s.Crew(0, s.BelowSlot(0), CrewCardId.Timoniere);   // la prima della priorità
            StrategicBot rush = Bot(BotProfile.Rush);
            s.PlayTurns(d => d.Player == 0 ? rush.Choose(d, View(s, 0)) : Default(d));
            Assert.IsTrue(s.P(0).CrewAbove.Any(c => c != null && c.Kind == CrewCardId.Timoniere));
        }

        // ---- Partite intere ----

        private static List<string> PlayMixed(int seed)
        {
            var config = new RulesConfig { maxRounds = 40 };
            GameSession session = TestSupport.Start(4, seed, config);
            var bots = new IBot[]
            {
                new StrategicBot(BotProfile.Rush, new SeededRandom(StrategicBot.SeedFor(seed, 0))),
                new RandomBot(new SeededRandom(seed)),
                new StrategicBot(BotProfile.Hunter, new SeededRandom(StrategicBot.SeedFor(seed, 2))),
                new RandomBot(new SeededRandom(seed + 1)),
            };
            return TestSupport.Play(session, d => bots[d.Player].Choose(d, session.State.ViewFor(d.Player)), s => false)
                .Select(e => e.Describe()).ToList();
        }

        [Test]
        public void StrategicBotsAreDeterministic()
        {
            CollectionAssert.AreEqual(PlayMixed(9), PlayMixed(9));
        }

        [Test]
        public void MixedSeatsPlayCompleteGamesCleanly()
        {
            SimulationReport report = SimulationRunner.Run(new SimulationOptions
            {
                FirstSeed = 1, LastSeed = 15, Config = new RulesConfig { maxRounds = 500 }, Map = TestSupport.StandardMap(),
                SeatProfiles = new[] { BotProfile.Rush, BotProfile.Hunter, null, null, BotProfile.Rush, BotProfile.Hunter, null, null },
            });
            Assert.IsTrue(report.Clean, report.ToText());
            StringAssert.Contains("Rush:", report.ToText());
        }

        private static void RunScenario(string name, params BotProfile[] seats)
        {
            SimulationReport report = SimulationRunner.Run(new SimulationOptions
            {
                Name = name, PlayerCounts = new[] { 4 }, FirstSeed = 1, LastSeed = 500,
                Config = new RulesConfig { maxRounds = 500 }, Map = TestSupport.StandardMap(), SeatProfiles = seats,
            });
            TestContext.Out.WriteLine(report.ToText());
            Assert.IsTrue(report.Clean, report.ToText());
        }

        /// <summary>Gli scenari di 09 §5 (4 giocatori, 500 seed ciascuno): lunghi, si lanciano a mano.</summary>
        [Test, Explicit("Lungo: scenari A–D, 4 giocatori × 500 seed")]
        public void BotScenarios()
        {
            RunScenario("A — Rush + 3 casuali", BotProfile.Rush, null, null, null);
            RunScenario("B — Cacciatore + 3 casuali", BotProfile.Hunter, null, null, null);
            RunScenario("C — Rush + Cacciatore + 2 casuali", BotProfile.Rush, BotProfile.Hunter, null, null);
            RunScenario("D — 2 Rush + 2 casuali", BotProfile.Rush, BotProfile.Rush, null, null);
        }

        /// <summary>Gate con posti misti (spec 0011): seed 1–1000 × 2/4/8.</summary>
        [Test, Explicit("Lungo: 1000 seed × 2/4/8 con posti misti")]
        public void MixedSeats1000Seeds()
        {
            SimulationReport report = SimulationRunner.Run(new SimulationOptions
            {
                Name = "posti misti", FirstSeed = 1, LastSeed = 1000, Config = new RulesConfig { maxRounds = 500 },
                Map = TestSupport.StandardMap(),
                SeatProfiles = new[] { BotProfile.Rush, BotProfile.Hunter, null, null, BotProfile.Rush, BotProfile.Hunter, null, null },
            });
            TestContext.Out.WriteLine(report.ToText());
            Assert.IsTrue(report.Clean, report.ToText());
        }
    }
}
