using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>Movimento simultaneo (02_regole.md §5.1–5.2). Zone tutte Normali, vento da Nord salvo dove indicato.</summary>
    public class MovementTests
    {
        private static Coord C(int x, int y) => new Coord(x, y);

        private static RoundScenario TwoShips(int x0, int y0, int x1, int y1)
        {
            return RoundScenario.Create(2).At(0, x0, y0).At(1, x1, y1).Order(0, 1);
        }

        [Test]
        public void BaseSpeedIsOneCell_R060()
        {
            RoundScenario s = TwoShips(2, 2, 16, 16);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.E });

            Assert.AreEqual(C(3, 2), s.P(0).Position);
            Assert.AreEqual(C(17, 16), s.P(1).Position);
            CollectionAssert.AreEqual(new[] { s.Config.baseSpeed, s.Config.baseSpeed }, events.OfType<MovementStartedEvent>().Single().Speeds);
        }

        [Test]
        public void WindAlongTheHeadingAddsAndAgainstItSubtracts_R061_R064()
        {
            RoundScenario s = TwoShips(2, 2, 16, 16).Wind(Heading.E);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.O });

            int[] speeds = events.OfType<MovementStartedEvent>().Single().Speeds.ToArray();
            Assert.AreEqual(s.Config.baseSpeed + s.Config.windSpeedBonus, speeds[0]);
            Assert.AreEqual(s.Config.baseSpeed - s.Config.windSpeedBonus, speeds[1]);
            Assert.AreEqual(C(2 + speeds[0], 2), s.P(0).Position);
            Assert.AreEqual(C(16, 16), s.P(1).Position, "velocità 0: la nave non si muove (R-064)");
        }

        [Test]
        public void DiagonalsToTheWindHaveNoEffect_R061()
        {
            RoundScenario s = TwoShips(2, 2, 16, 3).Wind(Heading.E);
            List<GameEvent> events = s.PlayRound(new[] { Heading.NE, Heading.NO });
            CollectionAssert.AreEqual(new[] { s.Config.baseSpeed, s.Config.baseSpeed }, events.OfType<MovementStartedEvent>().Single().Speeds);
        }

        [Test]
        public void WindIsNotAppliedLeavingAPortOrTheBorder_R062()
        {
            // p0 sull'isola 1 (14,5), p1 sulla cornice (0,7): entrambi col vento in poppa, ma velocità base.
            RoundScenario s = TwoShips(14, 5, 0, 7).Wind(Heading.E);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.E });

            CollectionAssert.AreEqual(new[] { s.Config.baseSpeed, s.Config.baseSpeed }, events.OfType<MovementStartedEvent>().Single().Speeds);
            Assert.AreEqual(C(15, 5), s.P(0).Position);
            Assert.AreEqual(C(1, 7), s.P(1).Position);
        }

        [Test]
        public void HelmsmanAddsSpeedAndTheJokerDuplicatesIt_R063_R015()
        {
            RoundScenario s = TwoShips(2, 2, 2, 16);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Timoniere);
            s.Crew(1, s.AboveSlot(0), CrewCardId.Timoniere);
            s.Crew(1, s.AboveSlot(1), CrewCardId.Jolly);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.E });

            int[] speeds = events.OfType<MovementStartedEvent>().Single().Speeds.ToArray();
            Assert.AreEqual(s.Config.baseSpeed + s.Config.helmsmanSpeedBonus, speeds[0]);
            Assert.AreEqual(s.Config.baseSpeed + 2 * s.Config.helmsmanSpeedBonus, speeds[1]);
            Assert.AreEqual(C(2 + speeds[1], 16), s.P(1).Position);
        }

        [Test]
        public void SpeedNeverGoesBelowZero_R064()
        {
            var config = new RulesConfig { baseSpeed = 0 };
            RoundScenario s = RoundScenario.Create(2, config).At(0, 5, 7).At(1, 12, 12).Order(0, 1).Wind(Heading.E);
            List<GameEvent> events = s.PlayRound(new[] { Heading.O, Heading.N });

            CollectionAssert.AreEqual(new[] { 0, 0 }, events.OfType<MovementStartedEvent>().Single().Speeds);
            Assert.IsFalse(events.OfType<ShipMovedEvent>().Any());
        }

        [Test]
        public void MovementEmitsOneEventPerShipPerStep_R065()
        {
            RoundScenario s = TwoShips(2, 2, 2, 16).Wind(Heading.E);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.N });

            var p0 = events.OfType<ShipMovedEvent>().Where(e => e.Player == 0).ToList();
            CollectionAssert.AreEqual(new[] { 1, 2 }, p0.Select(e => e.Step));
            Assert.AreEqual(C(3, 2), p0[0].To);
            Assert.AreEqual(C(4, 2), p0[1].To);
            var p1 = events.OfType<ShipMovedEvent>().Where(e => e.Player == 1).ToList();
            Assert.AreEqual(1, p1.Count, "speed 1: un solo passo, avanzato insieme al primo passo di p0");
            Assert.AreEqual(1, p1[0].Step);
        }

        [Test]
        public void ArrivingOnAnIslandStopsTheShip_R066()
        {
            // Velocità 3 (vento + Timoniere): (2,4) → (3,4) → (4,4) isola, il terzo passo si perde.
            RoundScenario s = TwoShips(2, 4, 16, 16).Wind(Heading.E);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Timoniere);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.N });

            Assert.AreEqual(C(4, 4), s.P(0).Position);
            var stop = events.OfType<ShipStoppedEvent>().Single(e => e.Player == 0);
            Assert.AreEqual(StopReason.Land, stop.Reason);
            Assert.AreEqual(2, stop.Step);
            Assert.AreEqual(2, events.OfType<ShipMovedEvent>().Count(e => e.Player == 0));
        }

        [Test]
        public void EnteringTheSacredIslandStopsTheShipAndRecordsTheStep_R066_R141()
        {
            RoundScenario s = TwoShips(7, 9, 16, 16).Wind(Heading.E);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Timoniere);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.N });

            Assert.AreEqual(C(9, 9), s.P(0).Position);
            var entered = events.OfType<SacredIslandEnteredEvent>().Single();
            Assert.AreEqual(0, entered.Player);
            Assert.AreEqual(2, entered.Step);
        }

        [Test]
        public void ReachingTheBorderStrandsTheShip_R066_R069()
        {
            RoundScenario s = TwoShips(2, 2, 16, 16).Wind(Heading.S);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Timoniere);
            List<GameEvent> events = s.PlayRound(new[] { Heading.S, Heading.E });

            Assert.AreEqual(C(2, 0), s.P(0).Position);
            Assert.AreEqual(StopReason.Land, events.OfType<ShipStoppedEvent>().Single(e => e.Player == 0).Reason);
            Assert.AreEqual(0, events.OfType<ShipStrandedEvent>().Single().Player);
        }

        [Test]
        public void HeadingOffTheMapLeavesTheShipOnTheBorder()
        {
            RoundScenario s = TwoShips(0, 7, 16, 16);
            List<GameEvent> events = s.PlayRound(new[] { Heading.O, Heading.E });

            Assert.AreEqual(C(0, 7), s.P(0).Position);
            Assert.AreEqual(StopReason.MapEdge, events.OfType<ShipStoppedEvent>().Single(e => e.Player == 0).Reason);
            Assert.IsFalse(events.OfType<ShipStrandedEvent>().Any(), "non si è mosso: non si arena di nuovo");
        }

        [Test]
        public void CollisionWithAStationaryShipStopsTheMover_R066()
        {
            // p1 contro vento resta ferma in (5,7); p0 a velocità 3 la raggiunge al secondo passo e si ferma.
            RoundScenario s = TwoShips(3, 7, 5, 7).Wind(Heading.E);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Timoniere);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.O });

            var stop = events.OfType<ShipStoppedEvent>().Single(e => e.Player == 0);
            Assert.AreEqual(StopReason.Collision, stop.Reason);
            Assert.AreEqual(C(5, 7), stop.Cell);
            Assert.AreEqual(2, stop.Step);
            Assert.AreEqual(C(5, 7), events.OfType<BoardingStartedEvent>().First().Cell, "la collisione porta all'Abbordaggio (R-070)");
        }

        [Test]
        public void TwoShipsArrivingOnTheSameCellBothStop_R066()
        {
            RoundScenario s = TwoShips(3, 7, 5, 7);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.O });

            var stops = events.OfType<ShipStoppedEvent>().Where(e => e.Reason == StopReason.Collision).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, stops.Select(e => e.Player));
            Assert.IsTrue(stops.All(e => e.Cell == C(4, 7)));
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, events.OfType<BoardingStartedEvent>().First().Players);
        }

        [Test]
        public void ShipsSwappingCellsCollideOnACellChosenByTheFirstInTurnOrder_R067()
        {
            // p0 (3,7)→E e p1 (4,7)→O si scambiano; p1 viene prima nell'ordine e sceglie (3,7).
            RoundScenario s = TwoShips(3, 7, 4, 7).Order(1, 0);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.O },
                RoundScenario.Prefer((d, o) => o is CellOption c && c.Cell == C(3, 7)));

            PendingDecision crossing = s.Decisions.Single(d => d.Kind == DecisionKind.CrossingCell);
            Assert.AreEqual(1, crossing.Player);
            Assert.IsFalse(crossing.IsSecret);
            CollectionAssert.AreEquivalent(new[] { C(3, 7), C(4, 7) }, crossing.Options.Cast<CellOption>().Select(o => o.Cell));

            var resolved = events.OfType<CrossingResolvedEvent>().Single();
            Assert.AreEqual(C(3, 7), resolved.Cell);
            Assert.AreEqual(C(3, 7), events.OfType<BoardingStartedEvent>().First().Cell, "dopo l'attraversamento le navi sono sulla stessa cella");
            Assert.AreEqual(2, events.OfType<ShipStoppedEvent>().Count(e => e.Reason == StopReason.Crossing));
        }

        [Test]
        public void LandHostsSeveralShipsWithoutCollision_R068()
        {
            RoundScenario s = TwoShips(3, 4, 4, 3);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.N });

            Assert.AreEqual(C(4, 4), s.P(0).Position);
            Assert.AreEqual(C(4, 4), s.P(1).Position);
            Assert.IsFalse(events.OfType<ShipStoppedEvent>().Any(e => e.Reason == StopReason.Collision));
            Assert.IsFalse(events.OfType<BoardingStartedEvent>().Any());
        }
    }
}
