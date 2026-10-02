using System;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Config;

namespace hp55games.MareIgnoto.Rules.Map
{
    /// <summary>Cella di un layout (campi pubblici: serializzabile da Unity).</summary>
    [Serializable]
    public struct LayoutCell
    {
        public int x;
        public int y;

        public LayoutCell(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Coord ToCoord() => new Coord(x, y);
    }

    /// <summary>Cella isola con l'id dell'isola cui appartiene (le celle con lo stesso id sono la stessa isola).</summary>
    [Serializable]
    public struct LayoutIslandCell
    {
        public int x;
        public int y;
        public int islandId;

        public LayoutIslandCell(int x, int y, int islandId)
        {
            this.x = x;
            this.y = y;
            this.islandId = islandId;
        }

        public Coord ToCoord() => new Coord(x, y);
    }

    /// <summary>Punti di partenza per un numero di giocatori (05_mappa.md §4): il posto k parte dal k-esimo punto (R-031).</summary>
    [Serializable]
    public sealed class SpawnPreset
    {
        public int playerCount;
        public List<LayoutCell> cells = new List<LayoutCell>();

        public SpawnPreset()
        {
        }

        public SpawnPreset(int playerCount, params LayoutCell[] cells)
        {
            this.playerCount = playerCount;
            this.cells.AddRange(cells);
        }
    }

    /// <summary>
    /// La mappa come dato (05_mappa.md §5): dimensioni, isole, Isola Sacra, punti di partenza.
    /// È l'oggetto puro che MapLayoutAsset produce; il motore lo converte in <see cref="GameMap"/>.
    /// </summary>
    [Serializable]
    public sealed class MapLayout
    {
        public int width = 20;
        public int height = 20;
        /// <summary>Celle isola, escluse quelle dell'Isola Sacra.</summary>
        public List<LayoutIslandCell> islandCells = new List<LayoutIslandCell>();
        public List<LayoutCell> sacredIslandCells = new List<LayoutCell>();
        /// <summary>Un preset di punti di partenza per ogni numero di giocatori; default = valori di 05_mappa.md §4 (20×20).</summary>
        public List<SpawnPreset> spawnPresets = DefaultSpawnPresets();

        /// <summary>I preset di 05_mappa.md §4 per la mappa 20×20: angoli, poi mezzerie (riga/colonna 9).</summary>
        public static List<SpawnPreset> DefaultSpawnPresets()
        {
            LayoutCell sw = new LayoutCell(0, 0), se = new LayoutCell(19, 0), nw = new LayoutCell(0, 19), ne = new LayoutCell(19, 19);
            LayoutCell n = new LayoutCell(9, 19), s = new LayoutCell(9, 0), w = new LayoutCell(0, 9), e = new LayoutCell(19, 9);
            return new List<SpawnPreset>
            {
                new SpawnPreset(2, sw, ne),
                new SpawnPreset(3, sw, se, n),
                new SpawnPreset(4, sw, se, nw, ne),
                new SpawnPreset(5, sw, se, nw, ne, n),
                new SpawnPreset(6, sw, se, nw, ne, n, s),
                new SpawnPreset(7, sw, se, nw, ne, n, s, w),
                new SpawnPreset(8, sw, se, nw, ne, n, s, w, e),
            };
        }

        /// <summary>Il preset per <paramref name="playerCount"/> giocatori; null se manca.</summary>
        public SpawnPreset PresetFor(int playerCount)
        {
            foreach (SpawnPreset preset in spawnPresets)
                if (preset != null && preset.playerCount == playerCount) return preset;
            return null;
        }

        /// <summary>Valida con i numeri di una <see cref="RulesConfig"/> (default se null).</summary>
        public MapValidationResult Validate(RulesConfig config = null)
        {
            if (config == null) config = new RulesConfig();
            var issues = new List<MapValidationIssue>();

            if (width != height)
                issues.Add(new MapValidationIssue(MapValidationCode.NotSquare,
                    "La mappa deve essere quadrata (" + width + "×" + height + ")."));
            if (width < config.minMapSize || height < config.minMapSize)
                issues.Add(new MapValidationIssue(MapValidationCode.TooSmall,
                    "Il lato minimo è " + config.minMapSize + " (" + width + "×" + height + ")."));
            if (issues.Count > 0) return new MapValidationResult(issues);

            var occupied = new HashSet<Coord>();
            foreach (LayoutIslandCell cell in islandCells)
                CheckLandCell(cell.ToCoord(), "Isola", occupied, issues);
            foreach (LayoutCell cell in sacredIslandCells)
                CheckLandCell(cell.ToCoord(), "Isola Sacra", occupied, issues);

            if (sacredIslandCells.Count == 0)
                issues.Add(new MapValidationIssue(MapValidationCode.NoSacredIsland,
                    "Serve almeno una cella di Isola Sacra."));

            ValidateSpawnPresets(config, occupied, issues);

            return new MapValidationResult(issues);
        }

        /// <summary>Per ogni N da minPlayers a maxPlayers: un solo preset di esattamente N punti di cornice distinti con mare adiacente.</summary>
        private void ValidateSpawnPresets(RulesConfig config, HashSet<Coord> landCells, List<MapValidationIssue> issues)
        {
            var seenCounts = new HashSet<int>();
            foreach (SpawnPreset preset in spawnPresets)
            {
                if (preset == null) continue;
                int n = preset.playerCount;
                if (n < config.minPlayers || n > config.maxPlayers)
                {
                    issues.Add(new MapValidationIssue(MapValidationCode.SpawnPresetOutOfRange,
                        "Preset per " + n + " giocatori: i giocatori vanno da " + config.minPlayers + " a " + config.maxPlayers + "."));
                    continue;
                }

                if (!seenCounts.Add(n))
                {
                    issues.Add(new MapValidationIssue(MapValidationCode.DuplicateSpawnPreset,
                        "C'è più di un preset per " + n + " giocatori."));
                    continue;
                }

                List<LayoutCell> cells = preset.cells ?? new List<LayoutCell>();
                if (cells.Count != n)
                    issues.Add(new MapValidationIssue(MapValidationCode.WrongSpawnCount,
                        "Il preset per " + n + " giocatori deve avere esattamente " + n + " punti (trovati " + cells.Count + ")."));

                var spawnSeen = new HashSet<Coord>();
                for (int i = 0; i < cells.Count; i++)
                {
                    Coord spawn = cells[i].ToCoord();
                    string label = "Preset " + n + ", punto " + (i + 1) + " " + spawn;
                    if (!IsBorder(spawn))
                    {
                        issues.Add(new MapValidationIssue(MapValidationCode.SpawnNotOnBorder, label + ": non è sulla cornice."));
                        continue;
                    }

                    if (!spawnSeen.Add(spawn))
                        issues.Add(new MapValidationIssue(MapValidationCode.DuplicateSpawn, label + ": è già usato nel preset."));

                    if (!HasSeaNeighbor(spawn, landCells))
                        issues.Add(new MapValidationIssue(MapValidationCode.SpawnWithoutSea, label + ": non ha celle di mare adiacenti."));
                }
            }

            for (int n = config.minPlayers; n <= config.maxPlayers; n++)
                if (!seenCounts.Contains(n))
                    issues.Add(new MapValidationIssue(MapValidationCode.MissingSpawnPreset,
                        "Manca il preset dei punti di partenza per " + n + " giocatori."));
        }

        private bool IsInBounds(Coord c) => c.X >= 0 && c.Y >= 0 && c.X < width && c.Y < height;

        private bool IsBorder(Coord c) =>
            IsInBounds(c) && (c.X == 0 || c.Y == 0 || c.X == width - 1 || c.Y == height - 1);

        private bool IsNavigableArea(Coord c) => IsInBounds(c) && !IsBorder(c);

        private void CheckLandCell(Coord cell, string what, HashSet<Coord> occupied, List<MapValidationIssue> issues)
        {
            if (!IsNavigableArea(cell))
                issues.Add(new MapValidationIssue(MapValidationCode.LandOutsideNavigableArea,
                    what + " " + cell + " fuori dall'area navigabile."));
            else if (!occupied.Add(cell))
                issues.Add(new MapValidationIssue(MapValidationCode.LandOverlap,
                    what + " " + cell + " sovrapposta a un'altra cella isola."));
        }

        private bool HasSeaNeighbor(Coord spawn, HashSet<Coord> landCells)
        {
            for (int h = 0; h < HeadingExtensions.Count; h++)
            {
                Coord n = spawn.Step((Heading)h);
                if (IsNavigableArea(n) && !landCells.Contains(n)) return true;
            }

            return false;
        }
    }
}
