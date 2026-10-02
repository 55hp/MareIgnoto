using System;
using System.Collections.Generic;
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
        private readonly Coord[] spawnPoints;
        private readonly int zonesPerSide;

        public int Width { get; }
        public int Height { get; }
        public int ZoneCount { get; }

        /// <summary>Punti di partenza in ordine di posto (R-031).</summary>
        public IReadOnlyList<Coord> SpawnPoints => spawnPoints;

        private GameMap(MapLayout layout, RulesConfig config)
        {
            Width = layout.width;
            Height = layout.height;
            kinds = new CellKind[Width, Height];
            islandIds = new int[Width, Height];
            zones = new int[Width, Height];

            int navigable = Width - 2;
            zonesPerSide = (navigable + config.zoneSize - 1) / config.zoneSize;
            ZoneCount = zonesPerSide * zonesPerSide;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    bool border = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                    kinds[x, y] = border ? CellKind.Border : CellKind.Sea;
                    islandIds[x, y] = -1;
                    zones[x, y] = border ? -1 : (y - 1) / config.zoneSize * zonesPerSide + (x - 1) / config.zoneSize;
                }
            }

            foreach (LayoutIslandCell cell in layout.islandCells)
            {
                kinds[cell.x, cell.y] = CellKind.Island;
                islandIds[cell.x, cell.y] = cell.islandId;
                zones[cell.x, cell.y] = -1;
            }

            foreach (LayoutCell cell in layout.sacredIslandCells)
            {
                kinds[cell.x, cell.y] = CellKind.SacredIsland;
                zones[cell.x, cell.y] = -1;
            }

            spawnPoints = new Coord[layout.spawnCells.Count];
            for (int i = 0; i < spawnPoints.Length; i++)
                spawnPoints[i] = layout.spawnCells[i].ToCoord();
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

        /// <summary>Indice della zona meteo (riga * zone per lato + colonna), -1 per cornice e isole (R-080).</summary>
        public int ZoneOf(Coord c)
        {
            if (!IsInBounds(c)) throw new ArgumentOutOfRangeException(nameof(c), c + " è fuori dalla mappa.");
            return zones[c.X, c.Y];
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
