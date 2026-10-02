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
        WrongSpawnCount,
        SpawnNotOnBorder,
        DuplicateSpawn,
        SpawnWithoutSea,
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
