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
        /// <summary>Celle di cornice, in ordine di posto: il posto 1 parte dal punto 1 (R-031).</summary>
        public List<LayoutCell> spawnCells = new List<LayoutCell>();

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

            if (spawnCells.Count != config.maxPlayers)
                issues.Add(new MapValidationIssue(MapValidationCode.WrongSpawnCount,
                    "Servono esattamente " + config.maxPlayers + " punti di partenza (trovati " + spawnCells.Count + ")."));

            var spawnSeen = new HashSet<Coord>();
            for (int i = 0; i < spawnCells.Count; i++)
            {
                Coord spawn = spawnCells[i].ToCoord();
                if (!IsBorder(spawn))
                {
                    issues.Add(new MapValidationIssue(MapValidationCode.SpawnNotOnBorder,
                        "Il punto di partenza " + (i + 1) + " " + spawn + " non è sulla cornice."));
                    continue;
                }

                if (!spawnSeen.Add(spawn))
                    issues.Add(new MapValidationIssue(MapValidationCode.DuplicateSpawn,
                        "Il punto di partenza " + (i + 1) + " " + spawn + " è già usato."));

                if (!HasSeaNeighbor(spawn, occupied))
                    issues.Add(new MapValidationIssue(MapValidationCode.SpawnWithoutSea,
                        "Il punto di partenza " + (i + 1) + " " + spawn + " non ha celle di mare adiacenti."));
            }

            return new MapValidationResult(issues);
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
