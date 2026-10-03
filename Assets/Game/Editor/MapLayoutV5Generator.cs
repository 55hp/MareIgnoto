using System.Linq;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Unity;
using UnityEditor;
using UnityEngine;

namespace hp55games.MareIgnoto.Editor
{
    /// <summary>
    /// Comando "MareIgnoto/Create MapLayout v5" (spec 0005, passo A): genera MapLayout.asset dal layout v5 approvato
    /// (<see cref="LayoutV5"/>, tech/05_mappa.md §3–§6), poi lo valida e scrive l'esito in Console.
    /// Se l'asset esiste già chiede conferma e lo sovrascrive sul posto, così il GUID (e i riferimenti) restano.
    /// </summary>
    public static class MapLayoutV5Generator
    {
        public const string AssetPath = "Assets/Game/Content/Config/MapLayout.asset";
        private const string RulesConfigPath = "Assets/Game/Content/Config/RulesConfig.asset";
        private const int MaxIssuesInLog = 10;

        [MenuItem("MareIgnoto/Create MapLayout v5")]
        public static void CreateMapLayoutV5()
        {
            MapLayout layout = LayoutV5.Create();

            MapLayoutAsset asset = AssetDatabase.LoadAssetAtPath<MapLayoutAsset>(AssetPath);
            if (asset == null && AssetDatabase.LoadMainAssetAtPath(AssetPath) != null)
            {
                Debug.LogError("[MapLayout] " + AssetPath + " esiste ma non è un MapLayoutAsset: non lo sovrascrivo.");
                return;
            }

            if (asset != null)
            {
                bool overwrite = EditorUtility.DisplayDialog("Create MapLayout v5",
                    AssetPath + " esiste già.\nSovrascriverlo con il layout v5 (05_mappa.md §6)?", "Sovrascrivi", "Annulla");
                if (!overwrite) return;

                Undo.RecordObject(asset, "Create MapLayout v5");
                asset.EditorReplaceLayout(layout);
                EditorUtility.SetDirty(asset);
            }
            else
            {
                asset = ScriptableObject.CreateInstance<MapLayoutAsset>();
                asset.EditorReplaceLayout(layout);
                AssetDatabase.CreateAsset(asset, AssetPath);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            RulesConfigAsset configAsset = AssetDatabase.LoadAssetAtPath<RulesConfigAsset>(RulesConfigPath);
            RulesConfig config = configAsset != null ? configAsset.ToConfig() : new RulesConfig();
            MapValidationResult result = asset.Validate(config);
            string against = configAsset != null ? RulesConfigPath : "RulesConfig di default";

            if (result.IsValid)
            {
                Debug.Log("[MapLayout] " + AssetPath + " creato dal layout v5: Validate OK (con " + against + ").", asset);
                return;
            }

            string first = string.Join("\n", result.Issues.Take(MaxIssuesInLog).Select(i => "- " + i.Message));
            string more = result.Issues.Count > MaxIssuesInLog ? "\n… e altri " + (result.Issues.Count - MaxIssuesInLog) + "." : "";
            Debug.LogError("[MapLayout] " + AssetPath + " creato, ma Validate() fallisce (" + result.Issues.Count +
                           " problemi, con " + against + "):\n" + first + more, asset);
        }
    }
}
