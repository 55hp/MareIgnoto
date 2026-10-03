using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Map;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>
    /// Il layout v4 approvato (tech/05_mappa.md §3, §4, §6), unica copia nel codice: dati di test, scritti con i nomi di
    /// cella del documento. MapLayout.asset (spec 0005) si costruirà dagli stessi elenchi.
    /// </summary>
    internal static class LayoutV4
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

        /// <summary>Spicchi in ordine R1…R8, poi nuvole N1–N12, con il livello iniziale (05 §3).</summary>
        private static readonly ZoneData[] Zones =
        {
            new ZoneData("R1", ZoneKind.RingSlice, 5, "P12, P13, P14, Q13, Q14, R14, R15"),
            new ZoneData("R2", ZoneKind.RingSlice, 5, "N16, O15, O16, O17, P17"),
            new ZoneData("R3", ZoneKind.RingSlice, 5, "J17, K15, K16, K17, L15, L16, M15"),
            new ZoneData("R4", ZoneKind.RingSlice, 5, "H14, H15, I13, I14, J14"),
            new ZoneData("R5", ZoneKind.RingSlice, 5, "H10, H9, I10, I11, J10, J11, J12"),
            new ZoneData("R6", ZoneKind.RingSlice, 5, "J7, K7, K8, K9, L8"),
            new ZoneData("R7", ZoneKind.RingSlice, 5, "M9, N8, N9, O7, O8, O9, P7"),
            new ZoneData("R8", ZoneKind.RingSlice, 5, "P10, Q10, Q11, R10, R9"),
            new ZoneData("N1", ZoneKind.Cloud, 0, "U10, U11, U12, U13, U14, U15, U9, V10, V11, V12, V13, V14"),
            new ZoneData("N2", ZoneKind.Cloud, 0, "U17, U18, V17, V18, W16, W17, W18, X15, X16"),
            new ZoneData("N3", ZoneKind.Cloud, 0, "P23, Q22, Q23, R20, R21, R22, S20, S21, S22"),
            new ZoneData("N4", ZoneKind.Cloud, 0, "J20, K20, K21, L20, L21, M20, M21, N20, N21, O20, O21, P20"),
            new ZoneData("N5", ZoneKind.Cloud, 0, "G20, G21, G22, H20, H21, H22, I22, I23, J23"),
            new ZoneData("N6", ZoneKind.Cloud, 0, "B15, B16, C16, C17, C18, D17, D18, E17, E18"),
            new ZoneData("N7", ZoneKind.Cloud, 0, "D10, D11, D12, D13, D14, E10, E11, E12, E13, E14, E15, E9"),
            new ZoneData("N8", ZoneKind.Cloud, 0, "B8, B9, C6, C7, C8, D6, D7, E6, E7"),
            new ZoneData("N9", ZoneKind.Cloud, 0, "G2, G3, G4, H2, H3, H4, I1, I2, J1"),
            new ZoneData("N10", ZoneKind.Cloud, 0, "J4, K3, K4, L3, L4, M3, M4, N3, N4, O3, O4, P4"),
            new ZoneData("N11", ZoneKind.Cloud, 0, "P1, Q1, Q2, R2, R3, R4, S2, S3, S4"),
            new ZoneData("N12", ZoneKind.Cloud, 0, "U6, U7, V6, V7, W6, W7, W8, X8, X9"),
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

        /// <summary>Il layout v4.</summary>
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
