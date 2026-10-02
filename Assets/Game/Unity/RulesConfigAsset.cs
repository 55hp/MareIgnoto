using hp55games.MareIgnoto.Rules.Config;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity
{
    /// <summary>
    /// I numeri del gioco, bilanciabili da Inspector (01_architettura.md §4). Un asset nuovo nasce con i default
    /// di <see cref="RulesConfig"/>, che coincidono con 02_regole.md e 03_contenuti.md: non c'è nulla da impostare.
    /// </summary>
    [CreateAssetMenu(menuName = "MareIgnoto/Rules Config", fileName = "RulesConfig")]
    public sealed class RulesConfigAsset : ScriptableObject
    {
        [SerializeField] private RulesConfig config = new RulesConfig();

        /// <summary>
        /// Una copia indipendente: chi la riceve (il motore) non può alterare l'asset, nemmeno in Editor
        /// dove le modifiche a un asset di Play Mode restano.
        /// </summary>
        public RulesConfig ToConfig()
        {
            return JsonUtility.FromJson<RulesConfig>(JsonUtility.ToJson(config));
        }
    }
}
