using System;
using System.Linq;
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
            // Con al massimo 4 giocatori i preset per 5–8 sono di troppo; con 9 manca quello per 9.
            MapValidationResult fewer = TestSupport.StandardMap().Validate(new RulesConfig { maxPlayers = 4 });
            Assert.IsTrue(fewer.Has(MapValidationCode.SpawnPresetOutOfRange));
            MapValidationResult more = TestSupport.StandardMap().Validate(new RulesConfig { maxPlayers = 9 });
            Assert.IsTrue(more.Has(MapValidationCode.MissingSpawnPreset));
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
        public void DefaultSpawnPresetsAreThoseOf05Section4()
        {
            var layout = new MapLayout();
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 6, 7, 8 }, layout.spawnPresets.Select(p => p.playerCount));
            CollectionAssert.AreEqual(new[] { new Coord(0, 0), new Coord(19, 19) }, layout.PresetFor(2).cells.Select(c => c.ToCoord()));
            CollectionAssert.AreEqual(new[] { new Coord(0, 0), new Coord(19, 0), new Coord(9, 19) }, layout.PresetFor(3).cells.Select(c => c.ToCoord()));
            CollectionAssert.AreEqual(new[] { new Coord(0, 0), new Coord(19, 0), new Coord(0, 19), new Coord(19, 19) },
                layout.PresetFor(4).cells.Select(c => c.ToCoord()));
            CollectionAssert.AreEqual(new[]
                {
                    new Coord(0, 0), new Coord(19, 0), new Coord(0, 19), new Coord(19, 19),
                    new Coord(9, 19), new Coord(9, 0), new Coord(0, 9), new Coord(19, 9),
                },
                layout.PresetFor(8).cells.Select(c => c.ToCoord()));
            for (int n = 2; n <= 8; n++) Assert.AreEqual(n, layout.PresetFor(n).cells.Count, "preset " + n);
        }

        [Test]
        public void MissingSpawnPresetIsRejected()
        {
            MapLayout map = TestSupport.StandardMap();
            map.spawnPresets.RemoveAll(p => p.playerCount == 5);
            Assert.IsTrue(map.Validate().Has(MapValidationCode.MissingSpawnPreset));
        }

        [Test]
        public void DuplicateSpawnPresetIsRejected()
        {
            MapLayout map = TestSupport.StandardMap();
            map.spawnPresets.Add(new SpawnPreset(2, new LayoutCell(0, 5), new LayoutCell(19, 5)));
            Assert.IsTrue(map.Validate().Has(MapValidationCode.DuplicateSpawnPreset));
        }

        [Test]
        public void WrongNumberOfSpawnPointsIsRejected()
        {
            MapLayout fewer = TestSupport.StandardMap();
            fewer.PresetFor(8).cells.RemoveAt(7);
            Assert.IsTrue(fewer.Validate().Has(MapValidationCode.WrongSpawnCount));

            MapLayout more = TestSupport.StandardMap();
            more.PresetFor(3).cells.Add(new LayoutCell(7, 0));
            Assert.IsTrue(more.Validate().Has(MapValidationCode.WrongSpawnCount));
        }

        [Test]
        public void SpawnOffBorderIsRejected()
        {
            MapLayout map = TestSupport.StandardMap();
            map.PresetFor(4).cells[2] = new LayoutCell(5, 5);
            Assert.IsTrue(map.Validate().Has(MapValidationCode.SpawnNotOnBorder));
        }

        [Test]
        public void DuplicateSpawnIsRejected()
        {
            MapLayout map = TestSupport.StandardMap();
            SpawnPreset preset = map.PresetFor(6);
            preset.cells[3] = preset.cells[0];
            Assert.IsTrue(map.Validate().Has(MapValidationCode.DuplicateSpawn));
        }

        [Test]
        public void SameCellInDifferentPresetsIsAllowed()
        {
            // I preset sono alternativi: (0,0) sta in tutti.
            Assert.IsTrue(TestSupport.StandardMap().Validate().IsValid);
            Assert.IsTrue(TestSupport.StandardMap().spawnPresets.All(p => p.cells.Contains(new LayoutCell(0, 0))));
        }

        [Test]
        public void SpawnWithoutAdjacentSeaIsRejected()
        {
            // L'angolo (0,0) ha mare solo in (1,1): si copre con un'isola.
            MapLayout map = TestSupport.StandardMap();
            map.islandCells.Add(new LayoutIslandCell(1, 1, 8));
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
        public void SpawnPointsDependOnThePlayerCountAndKeepTheLayoutOrder_R031()
        {
            CollectionAssert.AreEqual(new[] { new Coord(0, 0), new Coord(19, 19) }, map.SpawnPointsFor(2));
            Assert.AreEqual(new Coord(9, 19), map.SpawnPointsFor(3)[2]);
            Assert.AreEqual(new Coord(19, 9), map.SpawnPointsFor(8)[7]);
            for (int n = 2; n <= 8; n++) Assert.AreEqual(n, map.SpawnPointsFor(n).Count);
            Assert.IsEmpty(map.SpawnPointsFor(9));
        }

        [Test]
        public void OutOfBoundsLookupsThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => map.KindAt(new Coord(-1, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.ZoneOf(new Coord(20, 20)));
        }
    }
}
