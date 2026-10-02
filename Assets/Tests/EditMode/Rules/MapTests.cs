using System;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Map;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    public class HeadingAndCoordTests
    {
        [TestCase(1, Heading.N)]
        [TestCase(2, Heading.NE)]
        [TestCase(3, Heading.E)]
        [TestCase(4, Heading.SE)]
        [TestCase(5, Heading.S)]
        [TestCase(6, Heading.SO)]
        [TestCase(7, Heading.O)]
        [TestCase(8, Heading.NO)]
        public void D8MapsToHeadings_R072(int roll, Heading expected)
        {
            Assert.AreEqual(expected, HeadingExtensions.FromD8(roll));
        }

        [Test]
        public void D8OutOfRangeIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HeadingExtensions.FromD8(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => HeadingExtensions.FromD8(9));
        }

        [Test]
        public void ClockwiseRotationWrapsAndEightStepsChangeNothing_R083()
        {
            Assert.AreEqual(Heading.E, Heading.N.Rotate(2));
            Assert.AreEqual(Heading.N, Heading.NO.Rotate(1));
            Assert.AreEqual(Heading.SO, Heading.E.Rotate(3));
            foreach (Heading h in Enum.GetValues(typeof(Heading)))
            {
                Assert.AreEqual(h, h.Rotate(8));
                Assert.AreEqual(h, h.Rotate(3).Rotate(-3));
            }
        }

        [Test]
        public void OppositeIsHalfTurn_R061()
        {
            Assert.AreEqual(Heading.S, Heading.N.Opposite());
            Assert.AreEqual(Heading.SO, Heading.NE.Opposite());
            Assert.AreEqual(Heading.E, Heading.O.Opposite());
        }

        [Test]
        public void StepFollowsAxisConvention()
        {
            var origin = new Coord(5, 5);
            Assert.AreEqual(new Coord(5, 6), origin.Step(Heading.N));
            Assert.AreEqual(new Coord(6, 6), origin.Step(Heading.NE));
            Assert.AreEqual(new Coord(6, 5), origin.Step(Heading.E));
            Assert.AreEqual(new Coord(6, 4), origin.Step(Heading.SE));
            Assert.AreEqual(new Coord(5, 4), origin.Step(Heading.S));
            Assert.AreEqual(new Coord(4, 4), origin.Step(Heading.SO));
            Assert.AreEqual(new Coord(4, 5), origin.Step(Heading.O));
            Assert.AreEqual(new Coord(4, 6), origin.Step(Heading.NO));
        }

        [Test]
        public void DistanceIsChebyshev()
        {
            Assert.AreEqual(0, Coord.Distance(new Coord(3, 3), new Coord(3, 3)));
            Assert.AreEqual(1, Coord.Distance(new Coord(3, 3), new Coord(4, 4)));
            Assert.AreEqual(5, Coord.Distance(new Coord(1, 1), new Coord(6, 3)));
        }
    }

    public class MapLayoutTests
    {
        [Test]
        public void StandardLayoutIsValid()
        {
            MapValidationResult result = TestSupport.StandardMap().Validate();
            Assert.IsTrue(result.IsValid, result.ToString());
        }

        [Test]
        public void ValidatesAgainstTheGivenConfig()
        {
            var config = new RulesConfig { maxPlayers = 4 };
            MapValidationResult result = TestSupport.StandardMap().Validate(config);
            Assert.IsTrue(result.Has(MapValidationCode.WrongSpawnCount));
        }

        [Test]
        public void NotSquareIsRejected()
        {
            MapLayout map = TestSupport.StandardMap();
            map.height = 19;
            Assert.IsTrue(map.Validate().Has(MapValidationCode.NotSquare));
        }

        [Test]
        public void TooSmallIsRejected()
        {
            var map = new MapLayout { width = 4, height = 4 };
            Assert.IsTrue(map.Validate().Has(MapValidationCode.TooSmall));
        }

        [Test]
        public void IslandOnBorderOrOutsideIsRejected()
        {
            MapLayout onBorder = TestSupport.StandardMap();
            onBorder.islandCells.Add(new LayoutIslandCell(0, 7, 9));
            Assert.IsTrue(onBorder.Validate().Has(MapValidationCode.LandOutsideNavigableArea));

            MapLayout outside = TestSupport.StandardMap();
            outside.sacredIslandCells.Add(new LayoutCell(25, 3));
            Assert.IsTrue(outside.Validate().Has(MapValidationCode.LandOutsideNavigableArea));
        }

        [Test]
        public void OverlappingLandIsRejected()
        {
            MapLayout twoIslands = TestSupport.StandardMap();
            twoIslands.islandCells.Add(new LayoutIslandCell(4, 4, 7));
            Assert.IsTrue(twoIslands.Validate().Has(MapValidationCode.LandOverlap));

            MapLayout islandOnSacred = TestSupport.StandardMap();
            islandOnSacred.islandCells.Add(new LayoutIslandCell(9, 9, 7));
            Assert.IsTrue(islandOnSacred.Validate().Has(MapValidationCode.LandOverlap));
        }

        [Test]
        public void MissingSacredIslandIsRejected()
        {
            MapLayout map = TestSupport.StandardMap();
            map.sacredIslandCells.Clear();
            Assert.IsTrue(map.Validate().Has(MapValidationCode.NoSacredIsland));
        }

        [Test]
        public void WrongNumberOfSpawnPointsIsRejected()
        {
            MapLayout fewer = TestSupport.StandardMap();
            fewer.spawnCells.RemoveAt(7);
            Assert.IsTrue(fewer.Validate().Has(MapValidationCode.WrongSpawnCount));

            MapLayout more = TestSupport.StandardMap();
            more.spawnCells.Add(new LayoutCell(7, 0));
            Assert.IsTrue(more.Validate().Has(MapValidationCode.WrongSpawnCount));
        }

        [Test]
        public void SpawnOffBorderIsRejected()
        {
            MapLayout map = TestSupport.StandardMap();
            map.spawnCells[2] = new LayoutCell(5, 5);
            Assert.IsTrue(map.Validate().Has(MapValidationCode.SpawnNotOnBorder));
        }

        [Test]
        public void DuplicateSpawnIsRejected()
        {
            MapLayout map = TestSupport.StandardMap();
            map.spawnCells[3] = map.spawnCells[0];
            Assert.IsTrue(map.Validate().Has(MapValidationCode.DuplicateSpawn));
        }

        [Test]
        public void SpawnWithoutAdjacentSeaIsRejected()
        {
            // Il punto (10,0) ha mare solo in (9,1), (10,1), (11,1): si coprono con isole.
            MapLayout map = TestSupport.StandardMap();
            map.islandCells.Add(new LayoutIslandCell(9, 1, 8));
            map.islandCells.Add(new LayoutIslandCell(10, 1, 8));
            map.islandCells.Add(new LayoutIslandCell(11, 1, 8));
            MapValidationResult result = map.Validate();
            Assert.IsTrue(result.Has(MapValidationCode.SpawnWithoutSea), result.ToString());
        }

        [Test]
        public void GameMapRefusesInvalidLayout()
        {
            MapLayout map = TestSupport.StandardMap();
            map.sacredIslandCells.Clear();
            Assert.Throws<ArgumentException>(() => GameMap.Create(map, new RulesConfig()));
        }
    }

    public class GameMapTests
    {
        private readonly GameMap map = GameMap.Create(TestSupport.StandardMap(), new RulesConfig());

        [Test]
        public void CellKindsFollowTheLayout()
        {
            Assert.AreEqual(CellKind.Border, map.KindAt(new Coord(0, 0)));
            Assert.AreEqual(CellKind.Border, map.KindAt(new Coord(19, 7)));
            Assert.AreEqual(CellKind.Border, map.KindAt(new Coord(7, 19)));
            Assert.AreEqual(CellKind.Sea, map.KindAt(new Coord(1, 1)));
            Assert.AreEqual(CellKind.Sea, map.KindAt(new Coord(18, 18)));
            Assert.AreEqual(CellKind.Island, map.KindAt(new Coord(4, 4)));
            Assert.AreEqual(CellKind.SacredIsland, map.KindAt(new Coord(10, 10)));
            Assert.IsTrue(map.IsLand(new Coord(4, 4)));
            Assert.IsFalse(map.IsLand(new Coord(8, 8)));
        }

        [Test]
        public void IslandIdsAreExposed()
        {
            Assert.AreEqual(0, map.IslandIdAt(new Coord(5, 4)));
            Assert.AreEqual(3, map.IslandIdAt(new Coord(15, 14)));
            Assert.AreEqual(-1, map.IslandIdAt(new Coord(8, 8)));
        }

        [Test]
        public void ZonesAre3x3BlocksOfTheNavigableArea_R080()
        {
            Assert.AreEqual(36, map.ZoneCount);
            Assert.AreEqual(0, map.ZoneOf(new Coord(1, 1)));
            Assert.AreEqual(0, map.ZoneOf(new Coord(3, 3)));
            Assert.AreEqual(1, map.ZoneOf(new Coord(4, 1)));
            Assert.AreEqual(6, map.ZoneOf(new Coord(1, 4)));
            Assert.AreEqual(35, map.ZoneOf(new Coord(18, 18)));
        }

        [Test]
        public void BorderAndIslandCellsBelongToNoZone_R080()
        {
            Assert.AreEqual(-1, map.ZoneOf(new Coord(0, 0)));
            Assert.AreEqual(-1, map.ZoneOf(new Coord(19, 5)));
            Assert.AreEqual(-1, map.ZoneOf(new Coord(4, 4)));
            Assert.AreEqual(-1, map.ZoneOf(new Coord(9, 9)));
        }

        [Test]
        public void ZoneSizeComesFromConfig()
        {
            GameMap bigZones = GameMap.Create(TestSupport.StandardMap(), new RulesConfig { zoneSize = 6 });
            Assert.AreEqual(9, bigZones.ZoneCount);
            Assert.AreEqual(0, bigZones.ZoneOf(new Coord(6, 6)));
            Assert.AreEqual(1, bigZones.ZoneOf(new Coord(7, 1)));
        }

        [Test]
        public void SeaCellsOfZoneExcludeIslands()
        {
            // (4,4) sta nel blocco x 4–6, y 4–6: la zona 7.
            Assert.AreEqual(9, map.SeaCellsOfZone(0).Count);
            Assert.AreEqual(9 - 3, map.SeaCellsOfZone(7).Count); // isola 0 occupa (4,4), (5,4), (4,5)
        }

        [Test]
        public void SpawnPointsKeepTheLayoutOrder_R031()
        {
            Assert.AreEqual(new Coord(10, 0), map.SpawnPoints[0]);
            Assert.AreEqual(new Coord(10, 19), map.SpawnPoints[1]);
            Assert.AreEqual(8, map.SpawnPoints.Count);
        }

        [Test]
        public void OutOfBoundsLookupsThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => map.KindAt(new Coord(-1, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.ZoneOf(new Coord(20, 20)));
        }
    }
}
