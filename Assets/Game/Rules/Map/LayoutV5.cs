using System.Collections.Generic;
using System.Linq;
namespace hp55games.MareIgnoto.Rules.Map
{
    /// <summary>
    /// Il layout v5 approvato (tech/05_mappa.md §3, §4, §6), unica copia nel codice, scritto con i nomi di cella del
    /// documento. Serve al comando Editor "MareIgnoto/Create MapLayout v5", che ne genera MapLayout.asset, e ai test.
    /// La partita legge la mappa da MapLayout.asset, non da qui.
    /// </summary>
    public static class LayoutV5
    {
        private readonly struct ZoneData
        {
            public readonly string Id;
            public readonly ZoneKind Kind;
            public readonly int InitialLevel;
            public readonly string Cells;

            public ZoneData(string id, ZoneKind kind, int initialLevel, string cells)
            {
                Id = id;
                Kind = kind;
                InitialLevel = initialLevel;
                Cells = cells;
            }
        }

        public const int Size = 25;

        /// <summary>Isole, nell'ordine dell'id (1–16, 05 §6).</summary>
        public static readonly string[] Islands =
            { "C5", "C19", "D8", "D16", "F2", "F22", "I3", "I21", "Q3", "Q21", "T2", "T22", "V8", "V16", "W5", "W19" };

        public static readonly string[] SacredIsland = { "M12", "L12", "N12", "M11", "M13" };

        /// <summary>Spicchi in ordine R1…R8 (per angolo attorno all'Isola Sacra), poi nuvole N1–N8, con il livello iniziale (05 §3).</summary>
        private static readonly ZoneData[] Zones =
        {
            new ZoneData("R1", ZoneKind.RingSlice, 5, "Q13, Q14, Q15, R13, R14, R15"),
            new ZoneData("R2", ZoneKind.RingSlice, 5, "N16, N17, O16, O17, P16, P17"),
            new ZoneData("R3", ZoneKind.RingSlice, 5, "J16, J17, K16, K17, L16, L17"),
            new ZoneData("R4", ZoneKind.RingSlice, 5, "H13, H14, H15, I13, I14, I15"),
            new ZoneData("R5", ZoneKind.RingSlice, 5, "H10, H11, H9, I10, I11, I9"),
            new ZoneData("R6", ZoneKind.RingSlice, 5, "J7, J8, K7, K8, L7, L8"),
            new ZoneData("R7", ZoneKind.RingSlice, 5, "N7, N8, O7, O8, P7, P8"),
            new ZoneData("R8", ZoneKind.RingSlice, 5, "Q10, Q11, Q9, R10, R11, R9"),
            new ZoneData("N1", ZoneKind.Cloud, 0, "U17, U18, V17, V18, W16, W17, W18, X15, X16"),
            new ZoneData("N2", ZoneKind.Cloud, 0, "P23, Q22, Q23, R20, R21, R22, S20, S21, S22"),
            new ZoneData("N3", ZoneKind.Cloud, 0, "G20, G21, G22, H20, H21, H22, I22, I23, J23"),
            new ZoneData("N4", ZoneKind.Cloud, 0, "B15, B16, C16, C17, C18, D17, D18, E17, E18"),
            new ZoneData("N5", ZoneKind.Cloud, 0, "B8, B9, C6, C7, C8, D6, D7, E6, E7"),
            new ZoneData("N6", ZoneKind.Cloud, 0, "G2, G3, G4, H2, H3, H4, I1, I2, J1"),
            new ZoneData("N7", ZoneKind.Cloud, 0, "P1, Q1, Q2, R2, R3, R4, S2, S3, S4"),
            new ZoneData("N8", ZoneKind.Cloud, 0, "U6, U7, V6, V7, W6, W7, W8, X8, X9"),
        };

        /// <summary>I preset di 05 §4, per numero di giocatori.</summary>
        public static readonly Dictionary<int, string[]> SpawnPresets = new Dictionary<int, string[]>
        {
            [2] = new[] { "B1", "X23" },
            [3] = new[] { "B1", "X1", "M24" },
            [4] = new[] { "B1", "X1", "B23", "X23" },
            [5] = new[] { "B1", "X1", "B23", "X23", "M24" },
            [6] = new[] { "B1", "X1", "B23", "X23", "M24", "M0" },
            [7] = new[] { "B1", "X1", "B23", "X23", "M24", "M0", "A12" },
            [8] = new[] { "B1", "X1", "B23", "X23", "M24", "M0", "A12", "Y12" },
        };

        public static Coord C(string name) => Coord.Parse(name);

        private static LayoutCell L(string name)
        {
            Coord c = Coord.Parse(name);
            return new LayoutCell(c.X, c.Y);
        }

        private static IEnumerable<string> Split(string cells) => cells.Split(',').Select(c => c.Trim());

        /// <summary>Il layout v5.</summary>
        public static MapLayout Create()
        {
            var map = new MapLayout { width = Size, height = Size };
            for (int i = 0; i < Islands.Length; i++)
            {
                Coord c = C(Islands[i]);
                map.islandCells.Add(new LayoutIslandCell(c.X, c.Y, i + 1));
            }

            map.sacredIslandCells.AddRange(SacredIsland.Select(L));
            foreach (ZoneData zone in Zones)
                map.zones.Add(new LayoutZone(zone.Id, zone.Kind, zone.InitialLevel, Split(zone.Cells).Select(L)));
            foreach (KeyValuePair<int, string[]> preset in SpawnPresets)
                map.spawnPresets.Add(new SpawnPreset(preset.Key, preset.Value.Select(L).ToArray()));
            return map;
        }

        /// <summary>Le celle di una zona per id.</summary>
        public static IReadOnlyList<Coord> ZoneCells(string id) =>
            Split(Zones.Single(z => z.Id == id).Cells).Select(C).ToList();

        public static IEnumerable<string> ZoneIds(ZoneKind kind) => Zones.Where(z => z.Kind == kind).Select(z => z.Id);
    }
}
