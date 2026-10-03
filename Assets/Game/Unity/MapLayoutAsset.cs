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

        /// <summary>Valida il layout con i numeri di <paramref name="config"/> (i default se null).</summary>
        public MapValidationResult Validate(RulesConfig config = null)
        {
            return layout.Validate(config);
        }
    }
}
