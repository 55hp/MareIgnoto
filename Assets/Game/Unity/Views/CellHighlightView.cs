using System.Collections;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity.Views
{
    /// <summary>
    /// Evidenziazioni sulle celle (/BOARD/BOARD_Highlights). Prima versione (spec 0005): le rotte rivelate (R-041), un
    /// segno del colore del giocatore sulla cella verso cui punta la sua nave, girato nella direzione della rotta.
    /// Segue le rotazioni del meteo (R-083) e sparisce quando parte il movimento.
    /// </summary>
    public sealed class CellHighlightView : GameView
    {
        [SerializeField] private BoardView board;
        [SerializeField] private ShipsView ships;
        [SerializeField] private PlayerPaletteAsset palette;

        [Tooltip("Prefab del segno di rotta (per esempio una freccia piatta che punta lungo +Z).")]
        [SerializeField] private GameObject routeMarkerPrefab;

        [SerializeField] private float height = 0.07f;

        private readonly List<GameObject> markers = new List<GameObject>();
        private GameMap map;

        public override void Initialize(IReadOnlyGameState state)
        {
            if (!Require(board, nameof(board)) || !Require(ships, nameof(ships)) || !Require(palette, nameof(palette)) ||
                !Require(routeMarkerPrefab, nameof(routeMarkerPrefab))) return;

            map = state.Map;
            for (int player = 0; player < state.PlayerCount; player++)
            {
                GameObject marker = Instantiate(routeMarkerPrefab, transform);
                marker.name = "Route_" + UiText.BotName(player);
                Tint(marker, palette.ColorOf(player));
                marker.SetActive(false);
                markers.Add(marker);
            }
        }

        public override IEnumerator Play(GameEvent gameEvent, float duration)
        {
            switch (gameEvent)
            {
                case HeadingsRevealedEvent revealed:
                    for (int player = 0; player < revealed.Headings.Count && player < markers.Count; player++)
                    {
                        if (revealed.Headings[player].HasValue) Show(player, revealed.Headings[player].Value);
                        else markers[player].SetActive(false); // in Svago: nessuna rotta (R-045)
                    }

                    break;
                case HeadingRotatedEvent rotated when rotated.Player < markers.Count:
                    Show(rotated.Player, rotated.To);
                    break;
                case MovementStartedEvent _:
                    foreach (GameObject marker in markers) marker.SetActive(false);
                    break;
            }

            yield break;
        }

        private void Show(int player, Heading heading)
        {
            Coord from = ships.CellOf(player);
            Coord target = from.Step(heading);
            GameObject marker = markers[player];
            if (!map.IsInBounds(target))
            {
                marker.SetActive(false);
                return;
            }

            Vector3 direction = board.CellToWorld(target) - board.CellToWorld(from);
            marker.transform.position = board.CellToWorld(target) + board.transform.up * height;
            marker.transform.rotation = Quaternion.LookRotation(direction, board.transform.up);
            marker.SetActive(true);
        }
    }
}
