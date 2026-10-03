using System;
using System.Collections.Generic;
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
        private static Coord C(string name) => Coord.Parse(name);

        private static LayoutCell L(string name) => new LayoutCell(C(name).X, C(name).Y);

        private static LayoutZone Zone(MapLayout map, string id) => map.zones.Single(z => z.id == id);

        [Test]
        public void CellNamesUseColumnLettersAndRowNumbers_R002()
        {
            Assert.AreEqual(new Coord(1, 1), C("B1"));
            Assert.AreEqual(new Coord(24, 12), C("Y12"));
            Assert.AreEqual(new Coord(12, 0), C("M0"));
            Assert.AreEqual("M24", new Coord(12, 24).Name);
            Assert.Throws<FormatException>(() => Coord.Parse("12"));
        }

        [Test]
        public void TheV5LayoutIsValid()
        {
            MapValidationResult result = TestSupport.StandardMap().Validate();
            Assert.IsTrue(result.IsValid, result.ToString());
        }

        [Test]
        public void TheV5LayoutIs25x25WithTheDocumentedCounts_05_6()
        {
            MapLayout map = TestSupport.StandardMap();
            Assert.AreEqual(25, map.width);
            Assert.AreEqual(25, map.height);
            Assert.AreEqual(16, map.islandCells.Select(c => c.islandId).Distinct().Count());
            Assert.AreEqual(5, map.sacredIslandCells.Count);
            Assert.AreEqual(8, map.zones.Count(z => z.kind == ZoneKind.RingSlice));
            Assert.AreEqual(8, map.zones.Count(z => z.kind == ZoneKind.Cloud));
            Assert.AreEqual(48, map.zones.Where(z => z.kind == ZoneKind.RingSlice).Sum(z => z.cells.Count));
            Assert.AreEqual(72, map.zones.Where(z => z.kind == ZoneKind.Cloud).Sum(z => z.cells.Count));
            Assert.AreEqual(120, map.zones.Sum(z => z.cells.Count));

            GameMap game = GameMap.Create(map, new RulesConfig());
            int sea = 0;
            for (int x = 0; x < map.width; x++)
                for (int y = 0; y < map.height; y++)
                    if (game.KindAt(new Coord(x, y)) == CellKind.Sea) sea++;
            Assert.AreEqual(508, sea);
        }

        [Test]
        public void EverySpawnIs11FromTheSacredIslandWithTheTwoNearestIslandsAt4_05_6()
        {
            MapLayout map = TestSupport.StandardMap();
            var sacred = map.sacredIslandCells.Select(c => c.ToCoord()).ToList();
            var islands = map.islandCells.Select(c => c.ToCoord()).ToList();
            foreach (Coord spawn in map.spawnPresets.SelectMany(p => p.cells).Select(c => c.ToCoord()).Distinct())
            {
                Assert.AreEqual(11, sacred.Min(s => Coord.Distance(s, spawn)), spawn.Name);
                CollectionAssert.AreEqual(new[] { 4, 4 }, islands.Select(i => Coord.Distance(i, spawn)).OrderBy(d => d).Take(2), spawn.Name);
            }
        }

        private static Coord Rotate(Coord c) => new Coord(c.Y, 24 - c.X);

        private static Coord Mirror(Coord c) => new Coord(24 - c.X, c.Y);

        private static void AssertInvariant(IEnumerable<Coord> cells, Func<Coord, Coord> transform, string what)
        {
            var set = new HashSet<Coord>(cells);
            Assert.IsTrue(set.SetEquals(set.Select(transform)), what);
        }

        [Test]
        public void TheV5LayoutIsSymmetric_05_6()
        {
            MapLayout map = TestSupport.StandardMap();
            var islands = map.islandCells.Select(c => c.ToCoord()).ToList();
            var sacred = map.sacredIslandCells.Select(c => c.ToCoord()).ToList();
            var clouds = map.zones.Where(z => z.kind == ZoneKind.Cloud).SelectMany(z => z.cells).Select(c => c.ToCoord()).ToList();
            var ring = map.zones.Where(z => z.kind == ZoneKind.RingSlice).SelectMany(z => z.cells).Select(c => c.ToCoord()).ToList();
            var spawns = map.spawnPresets.SelectMany(p => p.cells).Select(c => c.ToCoord()).ToList();

            foreach (Func<Coord, Coord> t in new Func<Coord, Coord>[] { Rotate, Mirror })
            {
                AssertInvariant(islands, t, "isole");
                AssertInvariant(sacred, t, "Isola Sacra");
                AssertInvariant(clouds, t, "nuvole");
                AssertInvariant(spawns, t, "punti di partenza");
                AssertInvariant(ring, t, "anello");
            }
        }

        [Test]
        public void TheSpawnPresetsAreThoseOf05Section4_R031()
        {
            MapLayout map = TestSupport.StandardMap();
            CollectionAssert.AreEqual(new[] { C("B1"), C("X23") }, map.PresetFor(2).cells.Select(c => c.ToCoord()));
            CollectionAssert.AreEqual(new[] { C("B1"), C("X1"), C("M24") }, map.PresetFor(3).cells.Select(c => c.ToCoord()));
            CollectionAssert.AreEqual(new[] { C("B1"), C("X1"), C("B23"), C("X23"), C("M24"), C("M0"), C("A12"), C("Y12") },
                map.PresetFor(8).cells.Select(c => c.ToCoord()));
        }

        [Test]
        public void ANewLayoutIs25x25AndEmpty()
        {
            var layout = new MapLayout();
            Assert.AreEqual(25, layout.width);
            Assert.AreEqual(25, layout.height);
            Assert.IsEmpty(layout.zones);
            Assert.IsEmpty(layout.spawnPresets, "il layout v5 si inserisce come dato (MapLayout.asset), non è nel codice");
        }

        [Test]
        public void ValidatesAgainstTheGivenConfig()
        {
            // Con al massimo 4 giocatori i preset per 5–8 sono di troppo; con 9 manca quello per 9.
            Assert.IsTrue(TestSupport.StandardMap().Validate(new RulesConfig { maxPlayers = 4 }).Has(MapValidationCode.SpawnPresetOutOfRange));
            Assert.IsTrue(TestSupport.StandardMap().Validate(new RulesConfig { maxPlayers = 9 }).Has(MapValidationCode.MissingSpawnPreset));
        }

        [Test]
        public void TheInitialLevelComesOnlyFromTheLayout_05_3()
        {
            // Qualsiasi livello ≥ 0 è accettato: lo decide il layout, non la config.
            MapLayout other = TestSupport.StandardMap();
            Zone(other, "R1").initialLevel = 4;
            Zone(other, "N3").initialLevel = 1;
            Assert.IsTrue(other.Validate().IsValid, other.Validate().ToString());
            GameMap map = GameMap.Create(other, new RulesConfig());
            Assert.AreEqual(4, map.ZoneInitialLevel(map.ZoneIndex("R1")));
            Assert.AreEqual(1, map.ZoneInitialLevel(map.ZoneIndex("N3")));

            MapLayout negative = TestSupport.StandardMap();
            Zone(negative, "N3").initialLevel = -1;
            Assert.IsTrue(negative.Validate().Has(MapValidationCode.ZoneInitialLevel));
        }

        [Test]
        public void NotSquareIsRejected()
        {
            MapLayout map = TestSupport.StandardMap();
            map.height = 24;
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
            onBorder.islandCells.Add(new LayoutIslandCell(0, 7, 99));
            Assert.IsTrue(onBorder.Validate().Has(MapValidationCode.LandOutsideNavigableArea));

            MapLayout outside = TestSupport.StandardMap();
            outside.sacredIslandCells.Add(new LayoutCell(25, 3));
            Assert.IsTrue(outside.Validate().Has(MapValidationCode.LandOutsideNavigableArea));
        }

        [Test]
        public void OverlappingLandIsRejected()
        {
            MapLayout twoIslands = TestSupport.StandardMap();
            twoIslands.islandCells.Add(new LayoutIslandCell(C("C5").X, C("C5").Y, 99));
            Assert.IsTrue(twoIslands.Validate().Has(MapValidationCode.LandOverlap));

            MapLayout islandOnSacred = TestSupport.StandardMap();
            islandOnSacred.islandCells.Add(new LayoutIslandCell(C("M12").X, C("M12").Y, 99));
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
        public void MissingDuplicateOrWrongSizedSpawnPresetsAreRejected()
        {
            MapLayout missing = TestSupport.StandardMap();
            missing.spawnPresets.RemoveAll(p => p.playerCount == 5);
            Assert.IsTrue(missing.Validate().Has(MapValidationCode.MissingSpawnPreset));

            MapLayout duplicate = TestSupport.StandardMap();
            duplicate.spawnPresets.Add(new SpawnPreset(2, L("B1"), L("X1")));
            Assert.IsTrue(duplicate.Validate().Has(MapValidationCode.DuplicateSpawnPreset));

            MapLayout fewer = TestSupport.StandardMap();
            fewer.PresetFor(8).cells.RemoveAt(7);
            Assert.IsTrue(fewer.Validate().Has(MapValidationCode.WrongSpawnCount));
        }

        [TestCase("Q13")] // dentro uno spicchio (R1)
        [TestCase("X15")] // dentro una nuvola (N1)
        [TestCase("B4")]  // adiacente all'isola C5
        [TestCase("C5")]  // su un'isola
        public void ASpawnMustBeBorderOrFreeSeaAwayFromIslands_05_5(string cell)
        {
            MapLayout map = TestSupport.StandardMap();
            map.PresetFor(4).cells[2] = L(cell);
            Assert.IsTrue(map.Validate().Has(MapValidationCode.SpawnNotAllowed), cell);
        }

        [Test]
        public void DuplicateSpawnIsRejectedButTheSameCellInDifferentPresetsIsAllowed()
        {
            MapLayout map = TestSupport.StandardMap();
            SpawnPreset preset = map.PresetFor(6);
            preset.cells[3] = preset.cells[0];
            Assert.IsTrue(map.Validate().Has(MapValidationCode.DuplicateSpawn));
            Assert.IsTrue(TestSupport.StandardMap().spawnPresets.All(p => p.cells.Contains(L("B1"))), "i preset sono alternativi");
        }

        [Test]
        public void ABorderSpawnNeedsAdjacentSea()
        {
            MapLayout map = TestSupport.StandardMap();
            foreach (string cell in new[] { "B11", "B12", "B13" }) map.islandCells.Add(new LayoutIslandCell(C(cell).X, C(cell).Y, 99));
            Assert.IsTrue(map.Validate().Has(MapValidationCode.SpawnWithoutSea));
        }

        [Test]
        public void ZoneCellsMustBeSeaOnceAndConnected_05_3()
        {
            MapLayout onBorder = TestSupport.StandardMap();
            Zone(onBorder, "N1").cells.Add(L("Y10"));
            Assert.IsTrue(onBorder.Validate().Has(MapValidationCode.ZoneCellNotSea));

            MapLayout onIsland = TestSupport.StandardMap();
            Zone(onIsland, "N8").cells.Add(L("V8"));
            Assert.IsTrue(onIsland.Validate().Has(MapValidationCode.ZoneCellNotSea));

            MapLayout overlap = TestSupport.StandardMap();
            Zone(overlap, "N1").cells.Add(L("Q13"));
            Assert.IsTrue(overlap.Validate().Has(MapValidationCode.ZoneOverlap));

            MapLayout split = TestSupport.StandardMap();
            Zone(split, "N1").cells.Remove(L("W16"));
            Zone(split, "N1").cells.Remove(L("W17")); // X15, X16 restano staccate
            Assert.IsTrue(split.Validate().Has(MapValidationCode.ZoneNotConnected));

            MapLayout sameId = TestSupport.StandardMap();
            Zone(sameId, "N2").id = "N1";
            Assert.IsTrue(sameId.Validate().Has(MapValidationCode.ZoneIdInvalid));
        }

        [Test]
        public void CloudsKeepTheirDistances_05_3()
        {
            MapLayout clouds = TestSupport.StandardMap();
            Zone(clouds, "N2").cells.Add(L("T20"));
            Zone(clouds, "N2").cells.Add(L("T19")); // a 1 cella da N1 (U18)
            Assert.IsTrue(clouds.Validate().Has(MapValidationCode.ZoneSpacing));

            MapLayout ring = TestSupport.StandardMap();
            Zone(ring, "N8").cells.Add(L("T7")); // a 2 celle dallo spicchio R8 (R9)
            Assert.IsTrue(ring.Validate().Has(MapValidationCode.ZoneSpacing));

            // N5 allungata lungo la colonna B fino a B2, adiacente al punto di partenza B1.
            MapLayout spawn = TestSupport.StandardMap();
            foreach (string cell in new[] { "B7", "B6", "B5", "B4", "B3", "B2" }) Zone(spawn, "N5").cells.Add(L(cell));
            MapValidationResult result = spawn.Validate();
            Assert.IsTrue(result.Has(MapValidationCode.ZoneSpacing), result.ToString());
        }

        [Test]
        public void TheRingNeedsEightSlicesTouchingOnlyAtTheGaps_05_3()
        {
            MapLayout seven = TestSupport.StandardMap();
            seven.zones.RemoveAll(z => z.id == "R8");
            Assert.IsTrue(seven.Validate().Has(MapValidationCode.RingSliceCount));

            MapLayout edge = TestSupport.StandardMap();
            Zone(edge, "R1").cells.Add(L("P15")); // tocca R2 per un lato
            Assert.IsTrue(edge.Validate().Has(MapValidationCode.RingContact));

            // R1, R3, R2…: l'ordine attorno all'isola non torna (R1 e R3 non si toccano, R3 e R2 non sono consecutivi nel layout).
            MapLayout order = TestSupport.StandardMap();
            LayoutZone r2 = Zone(order, "R2");
            order.zones.Remove(r2);
            order.zones.Insert(order.zones.IndexOf(Zone(order, "R3")) + 1, r2);
            Assert.IsTrue(order.Validate().Has(MapValidationCode.RingContact));

            MapLayout gap = TestSupport.StandardMap();
            gap.islandCells.Add(new LayoutIslandCell(C("P15").X, C("P15").Y, 99)); // il varco R1–R2 non è più libero
            Assert.IsTrue(gap.Validate().Has(MapValidationCode.RingGap));
        }

        [Test]
        public void ConsecutiveSlicesWithoutContactNeedAStraightFreeChannelOneCellWide_05_3()
        {
            // Canale R2–R3 (colonna M, righe 16–17): occupato da un'isola.
            MapLayout blocked = TestSupport.StandardMap();
            blocked.islandCells.Add(new LayoutIslandCell(C("M16").X, C("M16").Y, 99));
            Assert.IsTrue(blocked.Validate().Has(MapValidationCode.RingGap));

            // Canale R2–R3 dentro una zona: non è più mare libero.
            MapLayout zoned = TestSupport.StandardMap();
            zoned.zones.Add(new LayoutZone("X1", ZoneKind.Cloud, 0, new[] { L("M17") }));
            Assert.IsTrue(zoned.Validate().Has(MapValidationCode.RingGap));

            // R3 senza la colonna L: il canale è largo 2, né varco né canale.
            MapLayout wide = TestSupport.StandardMap();
            Zone(wide, "R3").cells.RemoveAll(c => c.ToCoord().X == C("L16").X);
            Assert.IsTrue(wide.Validate().Has(MapValidationCode.RingContact));
        }

        [Test]
        public void NonConsecutiveSlicesStayApart_05_3()
        {
            // R1 allungato verso R3 fino a M15: tocca il canale e arriva a 1 cella da R3 (L16).
            MapLayout near = TestSupport.StandardMap();
            foreach (string cell in new[] { "P14", "O14", "N14", "M14", "M15" }) Zone(near, "R1").cells.Add(L(cell));
            MapValidationResult result = near.Validate();
            Assert.IsTrue(result.Issues.Any(i => i.Code == MapValidationCode.RingContact && i.Message.Contains("R1 e R3")), result.ToString());
        }

        [Test]
        public void SlicesMustBeInAngleOrder_05_3()
        {
            MapLayout swapped = TestSupport.StandardMap();
            LayoutZone r5 = Zone(swapped, "R5"), r6 = Zone(swapped, "R6");
            int i5 = swapped.zones.IndexOf(r5), i6 = swapped.zones.IndexOf(r6);
            swapped.zones[i5] = r6;
            swapped.zones[i6] = r5;
            Assert.IsTrue(swapped.Validate().Has(MapValidationCode.RingOrder));
        }

        [Test]
        public void TheRingKeepsAwayFromTheIslands_05_3()
        {
            MapLayout sacred = TestSupport.StandardMap();
            sacred.sacredIslandCells.Add(L("P12")); // a 1 cella da R1 (Q13)
            Assert.IsTrue(sacred.Validate().Has(MapValidationCode.ZoneSpacing));

            MapLayout island = TestSupport.StandardMap();
            island.islandCells.Add(new LayoutIslandCell(C("T15").X, C("T15").Y, 99)); // a 2 celle da R1
            Assert.IsTrue(island.Validate().Has(MapValidationCode.ZoneSpacing));
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

        private static Coord C(string name) => Coord.Parse(name);

        [Test]
        public void CellKindsFollowTheLayout()
        {
            Assert.AreEqual(CellKind.Border, map.KindAt(C("A0")));
            Assert.AreEqual(CellKind.Border, map.KindAt(C("M0")));
            Assert.AreEqual(CellKind.Border, map.KindAt(C("Y12")));
            Assert.AreEqual(CellKind.Sea, map.KindAt(C("B1")));
            Assert.AreEqual(CellKind.Sea, map.KindAt(C("X23")));
            Assert.AreEqual(CellKind.Island, map.KindAt(C("C5")));
            Assert.AreEqual(CellKind.SacredIsland, map.KindAt(C("M12")));
            Assert.IsTrue(map.IsLand(C("C5")));
            Assert.IsFalse(map.IsLand(C("H8")));
        }

        [Test]
        public void IslandIdsAreExposed()
        {
            Assert.AreEqual(1, map.IslandIdAt(C("C5")));
            Assert.AreEqual(16, map.IslandIdAt(C("W19")));
            Assert.AreEqual(-1, map.IslandIdAt(C("H8")));
            CollectionAssert.AreEqual(Enumerable.Range(1, 16), map.IslandIds());
        }

        [Test]
        public void ZonesComeFromTheLayout_R080()
        {
            Assert.AreEqual(16, map.ZoneCount);
            int r1 = map.ZoneIndex("R1");
            Assert.AreEqual(r1, map.ZoneOf(C("Q13")));
            Assert.AreEqual(ZoneKind.RingSlice, map.ZoneKindOf(r1));
            Assert.AreEqual(5, map.ZoneInitialLevel(r1), "05 §3: gli spicchi partono a 5");
            int n2 = map.ZoneIndex("N2");
            Assert.AreEqual(n2, map.ZoneOf(C("R21")));
            Assert.AreEqual(ZoneKind.Cloud, map.ZoneKindOf(n2));
            Assert.AreEqual(0, map.ZoneInitialLevel(n2));
            Assert.AreEqual("N2", map.ZoneId(n2));
            CollectionAssert.AreEquivalent(LayoutV5.ZoneCells("N2"), map.SeaCellsOfZone(n2));
            Assert.AreEqual(-1, map.ZoneIndex("Z9"));
        }

        [Test]
        public void FreeSeaBorderAndIslandsBelongToNoZone_R080()
        {
            Assert.AreEqual(-1, map.ZoneOf(C("B1")), "mare libero");
            Assert.AreEqual(-1, map.ZoneOf(C("P15")), "varco");
            Assert.AreEqual(-1, map.ZoneOf(C("M16")), "canale");
            Assert.AreEqual(-1, map.ZoneOf(C("A0")));
            Assert.AreEqual(-1, map.ZoneOf(C("C5")));
            Assert.AreEqual(-1, map.ZoneOf(C("M12")));
        }

        [Test]
        public void SpawnPointsDependOnThePlayerCountAndKeepTheLayoutOrder_R031()
        {
            CollectionAssert.AreEqual(new[] { C("B1"), C("X23") }, map.SpawnPointsFor(2));
            Assert.AreEqual(C("M24"), map.SpawnPointsFor(3)[2]);
            Assert.AreEqual(C("Y12"), map.SpawnPointsFor(8)[7]);
            for (int n = 2; n <= 8; n++) Assert.AreEqual(n, map.SpawnPointsFor(n).Count);
            Assert.IsEmpty(map.SpawnPointsFor(9));
        }

        [Test]
        public void OutOfBoundsLookupsThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => map.KindAt(new Coord(-1, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.ZoneOf(new Coord(25, 25)));
        }
    }
}
