using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Map;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity
{
    /// <summary>
    /// La mappa come asset (05_mappa.md §5): dimensioni, isole, Isola Sacra, zone meteo, punti di partenza.
    /// L'Inspector (assembly Editor) mostra l'esito di <see cref="Validate"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "MareIgnoto/Map Layout", fileName = "MapLayout")]
    public sealed class MapLayoutAsset : ScriptableObject
    {
        [SerializeField] private MapLayout layout = new MapLayout();

        /// <summary>Una copia indipendente, da passare al motore.</summary>
        public MapLayout ToLayout()
        {
            return JsonUtility.FromJson<MapLayout>(JsonUtility.ToJson(layout));
        }

#if UNITY_EDITOR
        /// <summary>
        /// Solo Editor: sostituisce il contenuto dell'asset con una copia di <paramref name="source"/>. Lo usa il comando
        /// "MareIgnoto/Create MapLayout v4" (spec 0005, passo A); a runtime l'asset si legge e basta.
        /// </summary>
        public void EditorReplaceLayout(MapLayout source)
        {
            layout = JsonUtility.FromJson<MapLayout>(JsonUtility.ToJson(source));
        }
#endif

        /// <summary>Valida il layout con i numeri di <paramref name="config"/> (i default se null).</summary>
        public MapValidationResult Validate(RulesConfig config = null)
        {
            return layout.Validate(config);
        }
    }
}
