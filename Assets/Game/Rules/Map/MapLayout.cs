using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>I due tipi di zona meteo (05_mappa.md §3).</summary>
    public enum ZoneKind
    {
        /// <summary>Nuvola: forma irregolare, parte a livello 0.</summary>
        Cloud,
        /// <summary>Spicchio dell'anello di tempesta attorno all'Isola Sacra (R1…R8); il livello iniziale è nel layout (05 §3).</summary>
        RingSlice,
    }

    /// <summary>Una zona meteo del layout (05_mappa.md §3, §5): id ("R1", "N4"), tipo, livello iniziale, celle.</summary>
    [Serializable]
    public sealed class LayoutZone
    {
        public string id;
        public ZoneKind kind;
        public int initialLevel;
        public List<LayoutCell> cells = new List<LayoutCell>();

        public LayoutZone()
        {
        }

        public LayoutZone(string id, ZoneKind kind, int initialLevel, IEnumerable<LayoutCell> cells)
        {
            this.id = id;
            this.kind = kind;
            this.initialLevel = initialLevel;
            this.cells.AddRange(cells);
        }
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
    /// La mappa come dato (05_mappa.md §5): dimensioni, isole, Isola Sacra, zone meteo, punti di partenza.
    /// È l'oggetto puro che MapLayoutAsset produce; il motore lo converte in <see cref="GameMap"/>. Il layout approvato
    /// (v4, 05 §6) si inserisce come dato (MapLayout.asset, spec 0005): qui ci sono solo le dimensioni di default.
    /// </summary>
    [Serializable]
    public sealed class MapLayout
    {
        public int width = 25;
        public int height = 25;
        /// <summary>Celle isola, escluse quelle dell'Isola Sacra.</summary>
        public List<LayoutIslandCell> islandCells = new List<LayoutIslandCell>();
        public List<LayoutCell> sacredIslandCells = new List<LayoutCell>();
        /// <summary>Nuvole e spicchi (05 §3). Gli spicchi sono in ordine R1…R8 attorno all'Isola Sacra.</summary>
        public List<LayoutZone> zones = new List<LayoutZone>();
        /// <summary>Un preset di punti di partenza per ogni numero di giocatori (05 §4).</summary>
        public List<SpawnPreset> spawnPresets = new List<SpawnPreset>();

        /// <summary>Il preset per <paramref name="playerCount"/> giocatori; null se manca.</summary>
        public SpawnPreset PresetFor(int playerCount)
        {
            foreach (SpawnPreset preset in spawnPresets)
                if (preset != null && preset.playerCount == playerCount) return preset;
            return null;
        }

        /// <summary>Valida con i numeri di una <see cref="RulesConfig"/> (default se null): 05_mappa.md §3 e §5.</summary>
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

            var islands = new HashSet<Coord>();
            var sacred = new HashSet<Coord>();
            var land = new HashSet<Coord>();
            foreach (LayoutIslandCell cell in islandCells ?? new List<LayoutIslandCell>())
                if (CheckLandCell(cell.ToCoord(), "Isola", land, issues)) islands.Add(cell.ToCoord());
            foreach (LayoutCell cell in sacredIslandCells ?? new List<LayoutCell>())
                if (CheckLandCell(cell.ToCoord(), "Isola Sacra", land, issues)) sacred.Add(cell.ToCoord());

            if (sacred.Count == 0)
                issues.Add(new MapValidationIssue(MapValidationCode.NoSacredIsland,
                    "Serve almeno una cella di Isola Sacra."));

            List<Coord> spawns = ValidateSpawnPresets(config, land, islands, sacred, issues);
            ValidateZones(config, land, islands, sacred, spawns, issues);

            return new MapValidationResult(issues);
        }

        // ---- Zone (05 §3) ----

        private void ValidateZones(RulesConfig config, HashSet<Coord> land, HashSet<Coord> islands, HashSet<Coord> sacred,
            List<Coord> spawns, List<MapValidationIssue> issues)
        {
            var zoneList = (zones ?? new List<LayoutZone>()).Where(z => z != null).ToList();
            var cellsOf = new List<HashSet<Coord>>();
            var owner = new Dictionary<Coord, string>();
            var ids = new HashSet<string>();

            foreach (LayoutZone zone in zoneList)
            {
                string id = string.IsNullOrEmpty(zone.id) ? "?" : zone.id;
                if (string.IsNullOrEmpty(zone.id) || !ids.Add(zone.id))
                    issues.Add(new MapValidationIssue(MapValidationCode.ZoneIdInvalid,
                        "Zona '" + id + "': l'id manca o è ripetuto."));

                var cells = new HashSet<Coord>();
                foreach (LayoutCell layoutCell in zone.cells ?? new List<LayoutCell>())
                {
                    Coord cell = layoutCell.ToCoord();
                    if (!IsNavigableArea(cell) || land.Contains(cell))
                        issues.Add(new MapValidationIssue(MapValidationCode.ZoneCellNotSea,
                            "Zona " + id + ": la cella " + cell.Name + " non è di mare."));
                    else if (owner.TryGetValue(cell, out string other))
                        issues.Add(new MapValidationIssue(MapValidationCode.ZoneOverlap,
                            "La cella " + cell.Name + " è sia nella zona " + other + " sia nella " + id + "."));
                    else
                    {
                        owner[cell] = id;
                        cells.Add(cell);
                    }
                }

                if (cells.Count == 0)
                    issues.Add(new MapValidationIssue(MapValidationCode.ZoneNotConnected, "Zona " + id + ": non ha celle."));
                else if (!IsEdgeConnected(cells))
                    issues.Add(new MapValidationIssue(MapValidationCode.ZoneNotConnected,
                        "Zona " + id + ": le celle non sono connesse per lati."));

                // Il livello iniziale viene solo dal layout (05 §3); deve essere un livello (R-081: intero ≥ 0).
                if (zone.initialLevel < 0)
                    issues.Add(new MapValidationIssue(MapValidationCode.ZoneInitialLevel,
                        "Zona " + id + ": il livello iniziale è negativo (" + zone.initialLevel + ")."));

                cellsOf.Add(cells);
            }

            var clouds = Enumerable.Range(0, zoneList.Count).Where(i => zoneList[i].kind == ZoneKind.Cloud).ToList();
            var slices = Enumerable.Range(0, zoneList.Count).Where(i => zoneList[i].kind == ZoneKind.RingSlice).ToList();
            string Id(int i) => zoneList[i].id;

            // Nuvole: tra loro, dagli spicchi, dai punti di partenza.
            for (int a = 0; a < clouds.Count; a++)
            {
                for (int b = a + 1; b < clouds.Count; b++)
                    if (MinDistance(cellsOf[clouds[a]], cellsOf[clouds[b]]) < config.cloudMinSpacing)
                        issues.Add(new MapValidationIssue(MapValidationCode.ZoneSpacing,
                            "Le nuvole " + Id(clouds[a]) + " e " + Id(clouds[b]) + " sono a meno di " + config.cloudMinSpacing + " celle."));
                foreach (int s in slices)
                    if (MinDistance(cellsOf[clouds[a]], cellsOf[s]) < config.cloudRingMinDistance)
                        issues.Add(new MapValidationIssue(MapValidationCode.ZoneSpacing,
                            "La nuvola " + Id(clouds[a]) + " è a meno di " + config.cloudRingMinDistance + " celle dallo spicchio " + Id(s) + "."));
                if (spawns.Count > 0 && MinDistance(cellsOf[clouds[a]], spawns) < config.cloudSpawnMinDistance)
                    issues.Add(new MapValidationIssue(MapValidationCode.ZoneSpacing,
                        "La nuvola " + Id(clouds[a]) + " è a meno di " + config.cloudSpawnMinDistance + " celle da un punto di partenza."));
            }

            // Anello: numero di spicchi, contatti tra consecutivi (un solo spigolo = varco), distanze dalle isole.
            if (slices.Count != config.ringSliceCount)
            {
                issues.Add(new MapValidationIssue(MapValidationCode.RingSliceCount,
                    "Gli spicchi dell'anello devono essere " + config.ringSliceCount + " (trovati " + slices.Count + ")."));
                return;
            }

            for (int i = 0; i < slices.Count; i++)
            {
                HashSet<Coord> slice = cellsOf[slices[i]];
                if (sacred.Count > 0 && MinDistance(slice, sacred) < config.ringSacredMinDistance)
                    issues.Add(new MapValidationIssue(MapValidationCode.ZoneSpacing,
                        "Lo spicchio " + Id(slices[i]) + " è a meno di " + config.ringSacredMinDistance + " celle dall'Isola Sacra."));
                if (islands.Count > 0 && MinDistance(slice, islands) < config.ringIslandMinDistance)
                    issues.Add(new MapValidationIssue(MapValidationCode.ZoneSpacing,
                        "Lo spicchio " + Id(slices[i]) + " è a meno di " + config.ringIslandMinDistance + " celle da un'isola."));

                for (int j = i + 1; j < slices.Count; j++)
                {
                    HashSet<Coord> other = cellsOf[slices[j]];
                    bool consecutive = j == i + 1 || (i == 0 && j == slices.Count - 1);
                    if (!consecutive)
                    {
                        if (MinDistance(slice, other) < config.ringNonConsecutiveMinDistance)
                            issues.Add(new MapValidationIssue(MapValidationCode.RingContact,
                                "Gli spicchi non consecutivi " + Id(slices[i]) + " e " + Id(slices[j]) + " sono troppo vicini."));
                        continue;
                    }

                    CheckGap(Id(slices[i]), slice, Id(slices[j]), other, land, owner, issues);
                }
            }
        }

        /// <summary>
        /// Due spicchi consecutivi si toccano per uno spigolo solo e per nessun lato; il blocco 2×2 del contatto ha le
        /// altre due celle di mare libero (il varco).
        /// </summary>
        private void CheckGap(string idA, HashSet<Coord> a, string idB, HashSet<Coord> b, HashSet<Coord> land,
            Dictionary<Coord, string> owner, List<MapValidationIssue> issues)
        {
            var corners = new List<KeyValuePair<Coord, Coord>>();
            foreach (Coord p in a)
                foreach (Coord q in b)
                {
                    int dx = Math.Abs(p.X - q.X), dy = Math.Abs(p.Y - q.Y);
                    if (dx + dy == 1)
                    {
                        issues.Add(new MapValidationIssue(MapValidationCode.RingContact,
                            "Gli spicchi " + idA + " e " + idB + " si toccano per un lato (" + p.Name + ", " + q.Name + ")."));
                        return;
                    }

                    if (dx == 1 && dy == 1) corners.Add(new KeyValuePair<Coord, Coord>(p, q));
                }

            if (corners.Count != 1)
            {
                issues.Add(new MapValidationIssue(MapValidationCode.RingContact,
                    "Gli spicchi " + idA + " e " + idB + " devono toccarsi per uno spigolo solo (contatti: " + corners.Count + ")."));
                return;
            }

            Coord c1 = new Coord(corners[0].Key.X, corners[0].Value.Y), c2 = new Coord(corners[0].Value.X, corners[0].Key.Y);
            foreach (Coord free in new[] { c1, c2 })
                if (!IsNavigableArea(free) || land.Contains(free) || owner.ContainsKey(free))
                    issues.Add(new MapValidationIssue(MapValidationCode.RingGap,
                        "Il varco tra " + idA + " e " + idB + ": la cella " + free.Name + " deve essere di mare libero."));
        }

        private static bool IsEdgeConnected(HashSet<Coord> cells)
        {
            var seen = new HashSet<Coord>();
            var stack = new Stack<Coord>();
            Coord start = cells.First();
            stack.Push(start);
            seen.Add(start);
            while (stack.Count > 0)
            {
                Coord c = stack.Pop();
                foreach (Coord n in new[] { new Coord(c.X + 1, c.Y), new Coord(c.X - 1, c.Y), new Coord(c.X, c.Y + 1), new Coord(c.X, c.Y - 1) })
                    if (cells.Contains(n) && seen.Add(n)) stack.Push(n);
            }

            return seen.Count == cells.Count;
        }

        private static int MinDistance(IEnumerable<Coord> a, IEnumerable<Coord> b)
        {
            int best = int.MaxValue;
            foreach (Coord p in a)
                foreach (Coord q in b)
                    best = Math.Min(best, Coord.Distance(p, q));
            return best;
        }

        // ---- Punti di partenza (05 §4–5) ----

        /// <summary>
        /// Per ogni N da minPlayers a maxPlayers: un solo preset di esattamente N punti distinti, ciascuno sulla cornice
        /// (con mare adiacente) oppure su mare libero non adiacente a un'isola. Restituisce tutti i punti validi.
        /// </summary>
        private List<Coord> ValidateSpawnPresets(RulesConfig config, HashSet<Coord> land, HashSet<Coord> islands,
            HashSet<Coord> sacred, List<MapValidationIssue> issues)
        {
            var all = new List<Coord>();
            var zoneCells = new HashSet<Coord>((zones ?? new List<LayoutZone>()).Where(z => z?.cells != null)
                .SelectMany(z => z.cells).Select(c => c.ToCoord()));
            var seenCounts = new HashSet<int>();
            foreach (SpawnPreset preset in spawnPresets ?? new List<SpawnPreset>())
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
                    string label = "Preset " + n + ", punto " + (i + 1) + " " + spawn.Name;
                    if (IsBorder(spawn))
                    {
                        if (!HasSeaNeighbor(spawn, land))
                            issues.Add(new MapValidationIssue(MapValidationCode.SpawnWithoutSea, label + ": non ha celle di mare adiacenti."));
                    }
                    else if (!IsNavigableArea(spawn) || land.Contains(spawn) || zoneCells.Contains(spawn))
                    {
                        issues.Add(new MapValidationIssue(MapValidationCode.SpawnNotAllowed,
                            label + ": deve stare sulla cornice o su mare libero (fuori dalle zone)."));
                        continue;
                    }
                    else if (islands.Concat(sacred).Any(island => Coord.Distance(island, spawn) <= 1))
                    {
                        issues.Add(new MapValidationIssue(MapValidationCode.SpawnNotAllowed, label + ": è adiacente a un'isola."));
                        continue;
                    }

                    if (!spawnSeen.Add(spawn))
                        issues.Add(new MapValidationIssue(MapValidationCode.DuplicateSpawn, label + ": è già usato nel preset."));
                    all.Add(spawn);
                }
            }

            for (int n = config.minPlayers; n <= config.maxPlayers; n++)
                if (!seenCounts.Contains(n))
                    issues.Add(new MapValidationIssue(MapValidationCode.MissingSpawnPreset,
                        "Manca il preset dei punti di partenza per " + n + " giocatori."));
            return all;
        }

        private bool IsInBounds(Coord c) => c.X >= 0 && c.Y >= 0 && c.X < width && c.Y < height;

        private bool IsBorder(Coord c) =>
            IsInBounds(c) && (c.X == 0 || c.Y == 0 || c.X == width - 1 || c.Y == height - 1);

        private bool IsNavigableArea(Coord c) => IsInBounds(c) && !IsBorder(c);

        private bool CheckLandCell(Coord cell, string what, HashSet<Coord> occupied, List<MapValidationIssue> issues)
        {
            if (!IsNavigableArea(cell))
            {
                issues.Add(new MapValidationIssue(MapValidationCode.LandOutsideNavigableArea,
                    what + " " + cell.Name + " fuori dall'area navigabile."));
                return false;
            }

            if (!occupied.Add(cell))
            {
                issues.Add(new MapValidationIssue(MapValidationCode.LandOverlap,
                    what + " " + cell.Name + " sovrapposta a un'altra cella isola."));
                return false;
            }

            return true;
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
