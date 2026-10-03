using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Engine;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Setup;
using NUnit.Framework;
using static hp55games.MareIgnoto.Rules.Tests.RoundScenario;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>
    /// Segnalino isola (R-097, R-053, R-069) sul layout v4. Isola 1 in C5: p0 ci arriva da B5 verso E. Isola 5 in F2.
    /// p1 resta lontana in Q16. Vento verso N.
    /// </summary>
    public class IslandMarkerTests
    {
        private const int C5 = 1, F2 = 5;
        private static readonly Heading[] ToC5 = { Heading.E, Heading.S };

        private static Coord C(string name) => Coord.Parse(name);

        private static RoundScenario FromB5(int marker = -1)
        {
            RoundScenario s = Create(2).At(0, 1, 5).At(1, 16, 16).Order(0, 1);
            s.P(0).IslandMarker = marker;
            s.P(0).Coins = 10;
            return s;
        }

        private static Func<PendingDecision, DecisionOption, bool> Port(PortAction action) =>
            Opt<PortActionOption>(0, o => o.Action == action);

        [Test]
        public void APortActionPutsTheMarkerOnThatIsland_R097()
        {
            RoundScenario s = FromB5();
            List<GameEvent> events = s.PlayRound(ToC5, Pick(Port(PortAction.Plunder)));

            Assert.AreEqual(C5, s.P(0).IslandMarker);
            IslandMarkerPlacedEvent placed = events.OfType<IslandMarkerPlacedEvent>().Single();
            Assert.AreEqual(C5, placed.IslandId);
            Assert.AreEqual(-1, placed.PreviousIslandId);
            Assert.AreEqual(C5, s.Session.State.Player(0).IslandMarker, "pubblico");
        }

        [Test]
        public void TheMarkerMovesFromThePreviousIsland_R097()
        {
            RoundScenario s = FromB5(F2);
            List<GameEvent> events = s.PlayRound(ToC5, Pick(Port(PortAction.Plunder)));

            Assert.AreEqual(C5, s.P(0).IslandMarker);
            Assert.AreEqual(F2, events.OfType<IslandMarkerPlacedEvent>().Single().PreviousIslandId);
        }

        [Test]
        public void NoActionIsOfferedAndDoesNotPlaceTheMarker_R097()
        {
            RoundScenario s = FromB5(F2);
            List<GameEvent> events = s.PlayRound(ToC5, Pick(Port(PortAction.None)));

            Assert.IsTrue(s.DecisionsOf(DecisionKind.PortAction, 0).Single().Options.Cast<PortActionOption>().Any(o => o.Action == PortAction.None));
            Assert.AreEqual(F2, s.P(0).IslandMarker);
            Assert.IsFalse(events.OfType<IslandMarkerPlacedEvent>().Any());
            Assert.AreEqual(10, s.P(0).Coins);
        }

        [Test]
        public void ArrivingOnTheOwnMarkerIslandIsLikeTheBorder_R097_R053_R069()
        {
            RoundScenario s = FromB5(C5);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            List<GameEvent> events = s.PlayRound(ToC5);

            Assert.AreEqual(C("C5"), s.P(0).Position);
            Assert.AreEqual(TurnSkipReason.OwnIslandMarker, events.OfType<TurnSkippedEvent>().Single(e => e.Player == 0).Reason);
            Assert.AreEqual(C("C5"), events.OfType<ShipStrandedEvent>().Single(e => e.Player == 0).Cell, "arenata");
            Assert.IsFalse(s.DecisionsOf(DecisionKind.PortAction, 0).Any(), "niente azione, nemmeno lo Svago");
            Assert.AreEqual(10, s.P(0).Coins, "turno saltato: niente Mozzo (R-052)");
        }

        [Test]
        public void ArrivingOnAnotherIslandIsANormalPortTurn_R097()
        {
            RoundScenario s = FromB5(F2);
            List<GameEvent> events = s.PlayRound(ToC5);

            Assert.IsFalse(events.OfType<TurnSkippedEvent>().Any(e => e.Player == 0));
            Assert.IsFalse(events.OfType<ShipStrandedEvent>().Any(e => e.Player == 0));
            Assert.IsTrue(s.DecisionsOf(DecisionKind.PortAction, 0).Any());
        }

        [Test]
        public void TheMarkerDoesNotConcernOtherShips_R097()
        {
            RoundScenario s = FromB5();
            s.P(1).IslandMarker = C5;
            List<GameEvent> events = s.PlayRound(ToC5);
            Assert.IsTrue(s.DecisionsOf(DecisionKind.PortAction, 0).Any(), "il segnalino di p1 non ferma p0");
            Assert.IsFalse(events.OfType<ShipStrandedEvent>().Any());
        }

        [Test]
        public void StayingOnTheIslandIsNotArriving_Leisure_R097_R091()
        {
            RoundScenario s = Create(2).At(0, 2, 5).At(1, 16, 16).Order(0, 1);
            s.P(0).IslandMarker = C5;
            s.P(0).LeisureRound = s.Session.State.Round;
            s.P(0).Coins = 10;
            List<GameEvent> events = s.PlayRound(ToC5, Pick(Port(PortAction.Plunder)));

            Assert.IsFalse(events.OfType<TurnSkippedEvent>().Any(e => e.Player == 0));
            Assert.IsTrue(s.DecisionsOf(DecisionKind.PortAction, 0).Any(), "in Svago la nave resta e può agire");
        }

        [Test]
        public void StayingOnTheIslandIsNotArriving_SpeedZero_R097_R064()
        {
            RoundScenario s = Create(2, new RulesConfig { baseSpeed = 0 }).At(0, 2, 5).At(1, 16, 16).Order(0, 1);
            s.P(0).IslandMarker = C5;
            s.PlayRound(ToC5);
            Assert.IsTrue(s.DecisionsOf(DecisionKind.PortAction, 0).Any());
        }

        [Test]
        public void LeavingAndComingBackIsArrivingAgain_R097()
        {
            RoundScenario s = Create(2).At(0, 2, 5).At(1, 16, 16).Order(0, 1);
            s.P(0).IslandMarker = C5;
            s.PlayRound(new[] { Heading.E, Heading.S }); // C5 → D5, in mare
            Assert.AreEqual(C("D5"), s.P(0).Position);

            List<GameEvent> back = s.PlayRound(new[] { Heading.O, Heading.S }); // D5 → C5
            Assert.AreEqual(TurnSkipReason.OwnIslandMarker, back.OfType<TurnSkippedEvent>().Single(e => e.Player == 0).Reason);
        }

        [Test]
        public void ARepositioningOntoTheOwnMarkerIslandCountsAsArriving_R097_R073a()
        {
            // Abbordaggio in D4 (p0 da D5 verso S, p1 da E4 verso O): p0 tira 8 (NO) e finisce sull'isola C5.
            RoundScenario s = Create(2).At(0, 3, 5).At(1, 4, 4).Order(0, 1).Wind(Heading.NE);
            s.P(0).IslandMarker = C5;
            s.Random.Enqueue(8, 3);
            List<GameEvent> events = s.PlayRound(new[] { Heading.S, Heading.O });

            Assert.AreEqual(C("C5"), s.P(0).Position);
            Assert.AreEqual(TurnSkipReason.OwnIslandMarker, events.OfType<TurnSkippedEvent>().Single(e => e.Player == 0).Reason);
            Assert.AreEqual(C("C5"), events.OfType<ShipStrandedEvent>().Single(e => e.Player == 0).Cell);
        }

        [Test]
        public void TheLookoutPlacesTheMarkerOnTheIslandItDocksAt_R097_R058()
        {
            RoundScenario s = Create(2).At(0, 3, 5).At(1, 16, 16).Order(0, 1);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Vedetta);
            s.P(0).IslandMarker = C5;
            s.PlayTurns(Pick(Opt<TurnKindOption>(0, o => o.Kind == TurnKind.Port), Port(PortAction.Plunder)));

            Assert.IsTrue(s.DecisionsOf(DecisionKind.PortAction, 0).Any(), "accanto all'isola del segnalino non è un arrivo");
            Assert.AreEqual(C5, s.P(0).IslandMarker);
            Assert.AreEqual(C("D5"), s.P(0).Position);
        }

        [Test]
        public void WithOneIslandInReachTheLookoutDoesNotAsk_R058_R097()
        {
            RoundScenario s = Create(2).At(0, 3, 5).At(1, 16, 16).Order(0, 1); // D5: solo C5 a portata
            s.Crew(0, s.AboveSlot(0), CrewCardId.Vedetta);
            s.PlayTurns(Pick(Opt<TurnKindOption>(0, o => o.Kind == TurnKind.Port), Port(PortAction.Plunder)));

            Assert.IsFalse(s.DecisionsOf(DecisionKind.LookoutIsland, 0).Any());
            Assert.AreEqual(C5, s.P(0).IslandMarker);
        }

        [TestCase(1)]
        [TestCase(3)]
        public void WithSeveralIslandsInReachThePlayerChooses_R058_R097(int chosen)
        {
            // C6 con Vedetta + Jolly (portata 2): a portata C5 (isola 1, a 1 cella) e D8 (isola 3, a 2 celle).
            RoundScenario s = Create(2).At(0, 2, 6).At(1, 16, 16).Order(0, 1);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Vedetta);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.PlayTurns(Pick(Opt<TurnKindOption>(0, o => o.Kind == TurnKind.Port), Opt<IslandOption>(0, o => o.IslandId == chosen),
                Port(PortAction.Plunder)));

            PendingDecision which = s.DecisionsOf(DecisionKind.LookoutIsland, 0).Single();
            Assert.IsTrue(which.IsSecret);
            CollectionAssert.AreEqual(new[] { 1, 3 }, which.Options.Cast<IslandOption>().Select(o => o.IslandId));
            Assert.AreEqual(chosen, s.P(0).IslandMarker, "il segnalino va sull'isola scelta");
        }

        [Test]
        public void ChoosingTheSeaTurnAsksNoIsland_R058()
        {
            RoundScenario s = Create(2).At(0, 2, 6).At(1, 16, 16).Order(0, 1);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Vedetta);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.PlayTurns(Pick(Opt<TurnKindOption>(0, o => o.Kind == TurnKind.Sea)));
            Assert.IsFalse(s.DecisionsOf(DecisionKind.LookoutIsland, 0).Any());
        }

        [Test]
        public void GambaDiLegnoCountsTheOwnMarkerIslandAsAStranding_R069_R097()
        {
            RoundScenario s = FromB5(C5);
            MissionCard card = s.Mission(0, MissionId.GambaDiLegno);
            s.PlayRound(ToC5);                    // C5, isola col segnalino
            s.At(0, 1, 8).PlayRound(new[] { Heading.O, Heading.S });  // A8, cornice
            s.At(0, 1, 11).PlayRound(new[] { Heading.O, Heading.S }); // A11, cornice
            Assert.IsTrue(s.P(0).Completed.Contains(card));
        }
    }

    /// <summary>Punti di partenza del layout v4 (R-031): angoli in mare, lati sulla cornice.</summary>
    public class SpawnTests
    {
        [Test]
        public void CornersStartAtSeaWithWindAndSidesOnTheBorderWithout_R031_R062()
        {
            GameSession session = TestSupport.Start(8, 3, null, null, setup => setup.Tutorial = new TutorialOptions { InitialWind = Heading.N });
            TestSupport.PlayWithBot(session);
            IReadOnlyList<Coord> spawns = session.State.Map.SpawnPointsFor(8);
            for (int id = 0; id < 8; id++) Assert.AreEqual(spawns[id], session.State.Player(id).Position);

            // Tutti verso N, col vento: gli angoli (in mare) hanno il bonus, i lati (cornice) no.
            var events = TestSupport.Play(session, d => d.Kind == DecisionKind.ChooseHeading
                ? d.Choose(d.Options.Cast<HeadingOption>().ToList().FindIndex(o => o.Heading == Heading.N))
                : d.Choose(0), s => s.State.Phase == State.GamePhase.Active, includeInitial: false);
            int[] speeds = events.OfType<MovementStartedEvent>().Single().Speeds.ToArray();
            RulesConfig cfg = session.State.Config;
            for (int id = 0; id < 8; id++)
            {
                bool corner = session.State.Map.KindAt(spawns[id]) == CellKind.Sea;
                Assert.AreEqual(cfg.baseSpeed + (corner ? cfg.windSpeedBonus : 0), speeds[id], spawns[id].Name);
            }
        }
    }
}
