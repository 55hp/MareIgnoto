using System.Collections.Generic;

namespace hp55games.MareIgnoto.Rules.Map
{
    /// <summary>Gli errori che <see cref="MapLayout.Validate"/> sa riconoscere (05_mappa.md §5).</summary>
    public enum MapValidationCode
    {
        NotSquare,
        TooSmall,
        LandOutsideNavigableArea,
        LandOverlap,
        NoSacredIsland,
        MissingSpawnPreset,
        DuplicateSpawnPreset,
        SpawnPresetOutOfRange,
        WrongSpawnCount,
        /// <summary>Punto di partenza né sulla cornice né su mare libero lontano dalle isole (05 §5).</summary>
        SpawnNotAllowed,
        DuplicateSpawn,
        SpawnWithoutSea,
        // ---- Zone (05 §3) ----
        ZoneIdInvalid,
        ZoneCellNotSea,
        ZoneOverlap,
        ZoneNotConnected,
        /// <summary>Livello iniziale diverso da quello della regola (nuvole 0, spicchi <c>ringInitialLevel</c>; R-038, R-081).</summary>
        ZoneInitialLevel,
        /// <summary>Distanze tra nuvole, spicchi, isole e punti di partenza.</summary>
        ZoneSpacing,
        RingSliceCount,
        /// <summary>Contatto tra spicchi: i consecutivi per uno spigolo solo, i non consecutivi lontani.</summary>
        RingContact,
        /// <summary>Le due celle del varco tra spicchi consecutivi non sono di mare libero.</summary>
        RingGap,
    }

    public sealed class MapValidationIssue
    {
        public MapValidationCode Code { get; }
        public string Message { get; }

        public MapValidationIssue(MapValidationCode code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString() => Message;
    }

    public sealed class MapValidationResult
    {
        public IReadOnlyList<MapValidationIssue> Issues { get; }
        public bool IsValid => Issues.Count == 0;

        public MapValidationResult(IReadOnlyList<MapValidationIssue> issues)
        {
            Issues = issues;
        }

        public bool Has(MapValidationCode code)
        {
            foreach (MapValidationIssue issue in Issues)
                if (issue.Code == code) return true;
            return false;
        }

        /// <summary>Un errore per riga; stringa vuota se valido.</summary>
        public override string ToString() => string.Join("\n", Issues);
    }
}
