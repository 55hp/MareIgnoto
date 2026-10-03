using System.Collections;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity.Views
{
    /// <summary>
    /// La griglia (/BOARD/BOARD_Tiles, 06_presentazione.md §2): istanzia una tile per cella dal MapLayout (unica eccezione
    /// "generato a runtime") e mostra i segnalini isola (R-097) come dischi del colore del giocatore. La cella (x, y) sta
    /// in (x · cellSize, 0, y · cellSize) nello spazio di questo oggetto (05_mappa.md §1): le altre viste passano da qui
    /// per convertire celle e spigoli in posizioni.
    /// </summary>
    public sealed class BoardView : GameView
    {
        [Header("Tile (Assets/Game/Content/Prefabs)")]
        [SerializeField] private GameObject seaTilePrefab;
        [SerializeField] private GameObject islandTilePrefab;
        [SerializeField] private GameObject sacredTilePrefab;
        [SerializeField] private GameObject borderTilePrefab;

        [Header("Segnalini isola (R-097)")]
        [SerializeField] private GameObject islandMarkerPrefab;
        [SerializeField] private PlayerPaletteAsset palette;
        [Tooltip("Altezza dei segnalini sopra la cella.")]
        [SerializeField] private float markerHeight = 0.1f;
        [Tooltip("Distanza dal centro della cella, in celle, per non sovrapporre i segnalini di più giocatori sulla stessa isola.")]
        [SerializeField] private float markerSpread = 0.25f;

        [Tooltip("Lato di una cella in unità di mondo.")]
        [SerializeField, Min(0.01f)] private float cellSize = 1f;

        private readonly Dictionary<int, Coord> islandCells = new Dictionary<int, Coord>();
        private readonly Dictionary<int, GameObject> markers = new Dictionary<int, GameObject>();

        public float CellSize => cellSize;

        /// <summary>Il centro della cella, in coordinate di mondo.</summary>
        public Vector3 CellToWorld(Coord cell) => transform.TransformPoint(new Vector3(cell.X * cellSize, 0f, cell.Y * cellSize));

        /// <summary>
        /// Uno spigolo della griglia (<see cref="ZoneGeometry"/>: lo spigolo (i, j) è l'angolo in basso a sinistra della
        /// cella (i, j)), in coordinate di mondo.
        /// </summary>
        public Vector3 CornerToWorld(Coord corner) =>
            transform.TransformPoint(new Vector3((corner.X - 0.5f) * cellSize, 0f, (corner.Y - 0.5f) * cellSize));

        public override void Initialize(IReadOnlyGameState state)
        {
            if (!Require(seaTilePrefab, nameof(seaTilePrefab)) || !Require(islandTilePrefab, nameof(islandTilePrefab)) ||
                !Require(sacredTilePrefab, nameof(sacredTilePrefab)) || !Require(borderTilePrefab, nameof(borderTilePrefab)) ||
                !Require(islandMarkerPrefab, nameof(islandMarkerPrefab)) || !Require(palette, nameof(palette))) return;

            GameMap map = state.Map;
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                {
                    var cell = new Coord(x, y);
                    GameObject tile = Instantiate(PrefabFor(map.KindAt(cell)), transform);
                    tile.name = "Tile_" + cell.Name;
                    tile.transform.position = CellToWorld(cell);

                    int island = map.IslandIdAt(cell);
                    if (island >= 0 && !islandCells.ContainsKey(island)) islandCells[island] = cell;
                }

            for (int player = 0; player < state.PlayerCount; player++)
                if (state.Player(player).IslandMarker >= 0) PlaceMarker(player, state.Player(player).IslandMarker);
        }

        public override IEnumerator Play(GameEvent gameEvent, float duration)
        {
            if (gameEvent is IslandMarkerPlacedEvent placed) PlaceMarker(placed.Player, placed.IslandId);
            yield break;
        }

        private GameObject PrefabFor(CellKind kind)
        {
            switch (kind)
            {
                case CellKind.Island: return islandTilePrefab;
                case CellKind.SacredIsland: return sacredTilePrefab;
                case CellKind.Border: return borderTilePrefab;
                default: return seaTilePrefab;
            }
        }

        /// <summary>Il disco del giocatore sulla prima cella dell'isola, spostato di poco a seconda del posto.</summary>
        private void PlaceMarker(int player, int islandId)
        {
            if (!islandCells.TryGetValue(islandId, out Coord cell)) return;
            if (!markers.TryGetValue(player, out GameObject marker))
            {
                marker = Instantiate(islandMarkerPrefab, transform);
                marker.name = "IslandMarker_" + UiText.BotName(player);
                Tint(marker, palette.ColorOf(player));
                markers[player] = marker;
            }

            float angle = player * Mathf.PI / 4f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (markerSpread * cellSize);
            marker.transform.position = CellToWorld(cell) + transform.TransformVector(offset) + transform.up * markerHeight;
        }
    }
}
