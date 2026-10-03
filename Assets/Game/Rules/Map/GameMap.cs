using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Config;

namespace hp55games.MareIgnoto.Rules.Map
{
    /// <summary>
    /// La mappa pronta per il gioco, ricavata da un <see cref="MapLayout"/> valido: tipo di cella, id isola,
    /// zona meteo e punti di partenza (05_mappa.md). Immutabile.
    /// </summary>
    public sealed class GameMap
    {
        private readonly CellKind[,] kinds;
        private readonly int[,] islandIds;
        private readonly int[,] zones;
        private readonly Dictionary<int, Coord[]> spawnPresets = new Dictionary<int, Coord[]>();
        private readonly string[] zoneIds;
        private readonly ZoneKind[] zoneKinds;
        private readonly int[] zoneInitialLevels;

        public int Width { get; }
        public int Height { get; }
        public int ZoneCount { get; }

        /// <summary>Punti di partenza per <paramref name="playerCount"/> giocatori, in ordine di posto (R-031); vuoto se il preset manca.</summary>
        public IReadOnlyList<Coord> SpawnPointsFor(int playerCount) =>
            spawnPresets.TryGetValue(playerCount, out Coord[] points) ? points : Array.Empty<Coord>();

        private GameMap(MapLayout layout, RulesConfig config)
        {
            Width = layout.width;
            Height = layout.height;
            kinds = new CellKind[Width, Height];
            islandIds = new int[Width, Height];
            zones = new int[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    bool border = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                    kinds[x, y] = border ? CellKind.Border : CellKind.Sea;
                    islandIds[x, y] = -1;
                    zones[x, y] = -1; // mare libero finché una zona non la reclama (R-080)
                }
            }

            foreach (LayoutIslandCell cell in layout.islandCells)
            {
                kinds[cell.x, cell.y] = CellKind.Island;
                islandIds[cell.x, cell.y] = cell.islandId;
            }

            foreach (LayoutCell cell in layout.sacredIslandCells)
                kinds[cell.x, cell.y] = CellKind.SacredIsland;

            // Zone nell'ordine del layout: l'indice è quello di stato, eventi e decisioni; l'id è quello di 05 §3.
            var layoutZones = layout.zones.Where(z => z != null).ToList();
            ZoneCount = layoutZones.Count;
            zoneIds = layoutZones.Select(z => z.id).ToArray();
            zoneKinds = layoutZones.Select(z => z.kind).ToArray();
            zoneInitialLevels = layoutZones.Select(z => z.initialLevel).ToArray();
            for (int i = 0; i < layoutZones.Count; i++)
                foreach (LayoutCell cell in layoutZones[i].cells)
                    zones[cell.x, cell.y] = i;

            foreach (SpawnPreset preset in layout.spawnPresets)
            {
                if (preset == null) continue;
                var points = new Coord[preset.cells.Count];
                for (int i = 0; i < points.Length; i++) points[i] = preset.cells[i].ToCoord();
                spawnPresets[preset.playerCount] = points;
            }
        }

        /// <summary>Costruisce la mappa; lancia ArgumentException se il layout non è valido.</summary>
        public static GameMap Create(MapLayout layout, RulesConfig config)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (config == null) throw new ArgumentNullException(nameof(config));

            MapValidationResult validation = layout.Validate(config);
            if (!validation.IsValid)
                throw new ArgumentException("MapLayout non valido:\n" + validation, nameof(layout));

            return new GameMap(layout, config);
        }

        public bool IsInBounds(Coord c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;

        public CellKind KindAt(Coord c)
        {
            if (!IsInBounds(c)) throw new ArgumentOutOfRangeException(nameof(c), c + " è fuori dalla mappa.");
            return kinds[c.X, c.Y];
        }

        /// <summary>Id dell'isola della cella, -1 se non è una cella isola (l'Isola Sacra non ha id).</summary>
        public int IslandIdAt(Coord c)
        {
            if (!IsInBounds(c)) throw new ArgumentOutOfRangeException(nameof(c), c + " è fuori dalla mappa.");
            return islandIds[c.X, c.Y];
        }

        /// <summary>Indice della zona meteo della cella; -1 per mare libero, cornice e isole (R-080).</summary>
        public int ZoneOf(Coord c)
        {
            if (!IsInBounds(c)) throw new ArgumentOutOfRangeException(nameof(c), c + " è fuori dalla mappa.");
            return zones[c.X, c.Y];
        }

        /// <summary>L'id della zona (05 §3), come "R1" o "N4".</summary>
        public string ZoneId(int zone) => zoneIds[zone];

        public ZoneKind ZoneKindOf(int zone) => zoneKinds[zone];

        /// <summary>Livello di partenza della zona (R-038, R-081).</summary>
        public int ZoneInitialLevel(int zone) => zoneInitialLevels[zone];

        /// <summary>L'indice della zona con questo id; -1 se non esiste.</summary>
        public int ZoneIndex(string id) => Array.IndexOf(zoneIds, id);

        /// <summary>Id delle isole (non l'Isola Sacra), una volta ciascuno, in ordine crescente.</summary>
        public IReadOnlyList<int> IslandIds()
        {
            var ids = new SortedSet<int>();
            foreach (int id in islandIds)
                if (id >= 0) ids.Add(id);
            return ids.ToArray();
        }

        /// <summary>Terra ferma: cornice, isola, Isola Sacra. Ferma il movimento (R-066).</summary>
        public bool IsLand(Coord c) => KindAt(c) != CellKind.Sea;

        /// <summary>Le celle di mare della zona, in ordine di riga e colonna.</summary>
        public IReadOnlyList<Coord> SeaCellsOfZone(int zone)
        {
            var cells = new List<Coord>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (zones[x, y] == zone) cells.Add(new Coord(x, y));
            return cells;
        }
    }
}
