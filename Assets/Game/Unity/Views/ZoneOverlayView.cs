using System.Collections;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;
using TMPro;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity.Views
{
    /// <summary>
    /// Le zone meteo (/BOARD/BOARD_Zones, 06_presentazione.md §2 e §6): ogni zona è una forma con il riempimento sulle sue
    /// celle e un contorno lungo il perimetro (<see cref="ZoneGeometry"/>). Il colore segue il livello (R-081): Normale
    /// invisibile, Mare Mosso giallo, Tempesta rosso; dal livello 1 in su la zona mostra il numero del livello.
    /// Si aggiorna con <see cref="ZoneChangedEvent"/>.
    /// </summary>
    public sealed class ZoneOverlayView : GameView
    {
        private sealed class ZoneShape
        {
            public GameObject Fill;
            public Renderer FillRenderer;
            public readonly List<LineRenderer> Outlines = new List<LineRenderer>();
            public TMP_Text Label;
        }

        [SerializeField] private BoardView board;

        [Tooltip("Prefab con MeshFilter + MeshRenderer e materiale trasparente: il codice ci mette la mesh delle celle.")]
        [SerializeField] private MeshFilter fillPrefab;

        [Tooltip("Prefab di una linea di contorno: il codice ne imposta i punti e il colore.")]
        [SerializeField] private LineRenderer outlinePrefab;

        [Tooltip("Prefab di testo 3D (TextMeshPro) per il livello della zona.")]
        [SerializeField] private TMP_Text labelPrefab;

        [SerializeField] private Color roughSeaColor = new Color(1f, 0.85f, 0.1f, 0.35f);
        [SerializeField] private Color stormColor = new Color(0.9f, 0.15f, 0.1f, 0.45f);

        [Tooltip("Altezze sopra le tile: riempimento, contorno, numero.")]
        [SerializeField] private float fillHeight = 0.04f;
        [SerializeField] private float outlineHeight = 0.05f;
        [SerializeField] private float labelHeight = 0.08f;

        private readonly List<ZoneShape> shapes = new List<ZoneShape>();
        private IReadOnlyGameState gameState;

        public override void Initialize(IReadOnlyGameState state)
        {
            if (!Require(board, nameof(board)) || !Require(fillPrefab, nameof(fillPrefab)) ||
                !Require(outlinePrefab, nameof(outlinePrefab)) || !Require(labelPrefab, nameof(labelPrefab))) return;

            gameState = state;
            GameMap map = state.Map;
            for (int zone = 0; zone < map.ZoneCount; zone++)
            {
                IReadOnlyList<Coord> cells = map.SeaCellsOfZone(zone);
                var shape = new ZoneShape();

                MeshFilter fill = Instantiate(fillPrefab, transform);
                fill.name = "Zone_" + map.ZoneId(zone);
                fill.sharedMesh = BuildMesh(cells, fill.transform);
                shape.Fill = fill.gameObject;
                shape.FillRenderer = fill.GetComponent<Renderer>();

                foreach (IReadOnlyList<Coord> loop in ZoneGeometry.Outline(cells))
                {
                    LineRenderer line = Instantiate(outlinePrefab, fill.transform);
                    line.name = "Outline";
                    // Il primo punto si ripete alla fine: il contorno è chiuso senza toccare le impostazioni del prefab.
                    Vector3[] points = loop.Concat(new[] { loop[0] })
                        .Select(corner => ToLineSpace(line, board.CornerToWorld(corner) + board.transform.up * outlineHeight))
                        .ToArray();
                    line.positionCount = points.Length;
                    line.SetPositions(points);
                    shape.Outlines.Add(line);
                }

                TMP_Text label = Instantiate(labelPrefab, fill.transform);
                label.name = "Level";
                label.transform.position = board.CellToWorld(ZoneGeometry.LabelCell(cells)) + board.transform.up * labelHeight;
                shape.Label = label;

                shapes.Add(shape);
                Apply(zone, state.ZoneLevels[zone]);
            }
        }

        public override IEnumerator Play(GameEvent gameEvent, float duration)
        {
            if (gameEvent is ZoneChangedEvent changed && changed.Zone >= 0 && changed.Zone < shapes.Count)
                Apply(changed.Zone, changed.ToLevel);
            yield break;
        }

        private void Apply(int zone, int level)
        {
            ZoneShape shape = shapes[zone];
            WeatherState effect = gameState.Config.WeatherAt(level);
            bool visible = effect != WeatherState.Normal;
            shape.Fill.SetActive(visible);
            if (!visible) return;

            Color color = effect == WeatherState.Storm ? stormColor : roughSeaColor;
            var block = new MaterialPropertyBlock();
            shape.FillRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            shape.FillRenderer.SetPropertyBlock(block);

            var opaque = new Color(color.r, color.g, color.b, 1f);
            foreach (LineRenderer line in shape.Outlines)
            {
                line.startColor = opaque;
                line.endColor = opaque;
            }

            shape.Label.text = UiText.ZoneLevel(level);
        }

        /// <summary>Un quad per cella (i suoi quattro spigoli di griglia), nello spazio dell'oggetto del riempimento.</summary>
        private Mesh BuildMesh(IReadOnlyList<Coord> cells, Transform space)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Vector3 lift = board.transform.up * fillHeight;
            foreach (Coord cell in cells)
            {
                int start = vertices.Count;
                // In basso a sinistra, in alto a sinistra, in alto a destra, in basso a destra: orario visto dall'alto.
                foreach (Coord corner in new[] { new Coord(cell.X, cell.Y), new Coord(cell.X, cell.Y + 1), new Coord(cell.X + 1, cell.Y + 1), new Coord(cell.X + 1, cell.Y) })
                    vertices.Add(space.InverseTransformPoint(board.CornerToWorld(corner) + lift));
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            var mesh = new Mesh { name = "ZoneFill" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 ToLineSpace(LineRenderer line, Vector3 world) =>
            line.useWorldSpace ? world : line.transform.InverseTransformPoint(world);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
    }
}
