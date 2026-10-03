using System.Collections;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity.Views
{
    /// <summary>
    /// Le navi (/BOARD/BOARD_Ships, 06_presentazione.md §2): una per giocatore dal prefab Ship, del colore del posto.
    /// Avanzano di una cella per evento di movimento (04_motore.md §3), poi riposizionamenti e attraversamenti. Più navi
    /// sulla stessa cella (isole, cornice, R-071) si spostano di poco per restare visibili.
    /// </summary>
    public sealed class ShipsView : GameView
    {
        [SerializeField] private BoardView board;
        [SerializeField] private GameObject shipPrefab;
        [SerializeField] private PlayerPaletteAsset palette;

        [Tooltip("Spostamento dal centro, in celle, quando più navi stanno sulla stessa cella.")]
        [SerializeField] private float sharedCellSpread = 0.22f;

        private readonly List<GameObject> ships = new List<GameObject>();
        private readonly List<Coord> cells = new List<Coord>();

        /// <summary>La cella in cui la nave del giocatore è mostrata ora (in pari con gli eventi già riprodotti).</summary>
        public Coord CellOf(int player) => cells[player];

        public int ShipCount => ships.Count;

        public override void Initialize(IReadOnlyGameState state)
        {
            if (!Require(board, nameof(board)) || !Require(shipPrefab, nameof(shipPrefab)) || !Require(palette, nameof(palette))) return;

            for (int player = 0; player < state.PlayerCount; player++)
            {
                GameObject ship = Instantiate(shipPrefab, transform);
                ship.name = "Ship_" + UiText.BotName(player);
                Tint(ship, palette.ColorOf(player));
                ships.Add(ship);
                cells.Add(state.Player(player).Position);
            }

            LayoutAll();
        }

        public override IEnumerator Play(GameEvent gameEvent, float duration)
        {
            switch (gameEvent)
            {
                case ShipPlacedEvent placed:
                    cells[placed.Player] = placed.Position;
                    LayoutAll();
                    break;
                case ShipMovedEvent moved:
                    yield return StartCoroutine(Move(moved.Player, moved.To, duration));
                    break;
                case ShipRepositionedEvent repositioned:
                    yield return StartCoroutine(Move(repositioned.Player, repositioned.To, duration));
                    break;
                case CrossingResolvedEvent crossing:
                    cells[crossing.PlayerA] = crossing.Cell;
                    cells[crossing.PlayerB] = crossing.Cell;
                    LayoutAll();
                    break;
                case MovementEndedEvent ended:
                    // Allineamento finale: le posizioni del motore a fine movimento.
                    for (int player = 0; player < ended.Positions.Count && player < cells.Count; player++) cells[player] = ended.Positions[player];
                    LayoutAll();
                    break;
            }
        }

        private IEnumerator Move(int player, Coord to, float duration)
        {
            GameObject ship = ships[player];
            Vector3 from = ship.transform.position;
            Vector3 target = board.CellToWorld(to);
            Vector3 direction = target - from;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f) ship.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                ship.transform.position = Vector3.Lerp(from, target, t / duration);
                yield return null;
            }

            cells[player] = to;
            LayoutAll();
        }

        /// <summary>Mette ogni nave sulla sua cella; le navi che la condividono si dispongono in cerchio attorno al centro.</summary>
        private void LayoutAll()
        {
            foreach (IGrouping<Coord, int> group in Enumerable.Range(0, ships.Count).GroupBy(player => cells[player]))
            {
                List<int> players = group.ToList();
                for (int i = 0; i < players.Count; i++)
                {
                    Vector3 offset = Vector3.zero;
                    if (players.Count > 1)
                    {
                        float angle = 2f * Mathf.PI * i / players.Count;
                        offset = board.transform.TransformVector(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (sharedCellSpread * board.CellSize));
                    }

                    ships[players[i]].transform.position = board.CellToWorld(group.Key) + offset;
                }
            }
        }
    }
}
