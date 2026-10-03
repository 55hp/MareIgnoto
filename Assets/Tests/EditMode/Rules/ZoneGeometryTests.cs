using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Map;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>Contorno e cella dell'etichetta delle zone (ZoneOverlayView, spec 0005): geometria pura.</summary>
    public class ZoneGeometryTests
    {
        private static Coord P(int x, int y) => new Coord(x, y);

        private static IReadOnlyList<IReadOnlyList<Coord>> Outline(params Coord[] cells) => ZoneGeometry.Outline(cells);

        /// <summary>Area con segno (formula del laccio): positiva se antioraria.</summary>
        private static int TwiceArea(IReadOnlyList<Coord> loop)
        {
            int sum = 0;
            for (int i = 0; i < loop.Count; i++)
            {
                Coord a = loop[i], b = loop[(i + 1) % loop.Count];
                sum += a.X * b.Y - b.X * a.Y;
            }

            return sum;
        }

        [Test]
        public void OneCellIsASquareOfFourCorners()
        {
            var loops = Outline(P(3, 4));
            Assert.AreEqual(1, loops.Count);
            CollectionAssert.AreEquivalent(new[] { P(3, 4), P(4, 4), P(4, 5), P(3, 5) }, loops[0]);
            Assert.AreEqual(2, TwiceArea(loops[0]), "antiorario, area 1");
        }

        [Test]
        public void StraightSidesHaveNoMiddleVertices()
        {
            var loops = Outline(P(0, 0), P(1, 0), P(2, 0));
            Assert.AreEqual(1, loops.Count);
            Assert.AreEqual(4, loops[0].Count);
            Assert.AreEqual(2 * 3, TwiceArea(loops[0]));
        }

        [Test]
        public void AnLShapeHasSixCorners()
        {
            var loops = Outline(P(0, 0), P(1, 0), P(0, 1));
            Assert.AreEqual(1, loops.Count);
            Assert.AreEqual(6, loops[0].Count);
            Assert.AreEqual(2 * 3, TwiceArea(loops[0]));
        }

        [Test]
        public void AHoleGetsItsOwnClockwiseLoop()
        {
            var ring = new List<Coord>();
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    if (x != 1 || y != 1) ring.Add(P(x, y));
            var loops = ZoneGeometry.Outline(ring);

            Assert.AreEqual(2, loops.Count);
            Assert.AreEqual(1, loops.Count(l => TwiceArea(l) == 2 * 9), "bordo esterno, antiorario");
            Assert.AreEqual(1, loops.Count(l => TwiceArea(l) == -2 * 1), "buco, orario");
        }

        [Test]
        public void CellsTouchingAtACornerGiveTwoSeparateLoops()
        {
            var loops = Outline(P(0, 0), P(1, 1));
            Assert.AreEqual(2, loops.Count);
            Assert.IsTrue(loops.All(l => l.Count == 4));
        }

        [Test]
        public void EveryV5ZoneIsOneClosedOutlineAroundItsCells()
        {
            MapLayout layout = LayoutV5.Create();
            foreach (LayoutZone zone in layout.zones)
            {
                List<Coord> cells = zone.cells.Select(c => c.ToCoord()).ToList();
                var loops = ZoneGeometry.Outline(cells);
                Assert.AreEqual(1, loops.Count, zone.id + ": le zone v5 non hanno buchi");
                Assert.AreEqual(2 * cells.Count, TwiceArea(loops[0]), zone.id + ": l'area racchiusa è quella delle celle");
                for (int i = 0; i < loops[0].Count; i++)
                {
                    Coord a = loops[0][i], b = loops[0][(i + 1) % loops[0].Count];
                    Assert.IsTrue(a.X == b.X || a.Y == b.Y, zone.id + ": solo lati orizzontali e verticali");
                }

                CollectionAssert.Contains(cells, ZoneGeometry.LabelCell(cells), zone.id + ": l'etichetta sta in una cella della zona");
            }
        }

        [Test]
        public void TheLabelCellIsTheOneNearestToTheCentre()
        {
            Assert.AreEqual(P(1, 0), ZoneGeometry.LabelCell(new[] { P(0, 0), P(1, 0), P(2, 0) }));
            // Una C: il baricentro cade nel vuoto, l'etichetta resta su una cella della zona.
            var c = new[] { P(0, 0), P(1, 0), P(2, 0), P(0, 1), P(0, 2), P(1, 2), P(2, 2) };
            CollectionAssert.Contains(c, ZoneGeometry.LabelCell(c));
        }
    }
}
