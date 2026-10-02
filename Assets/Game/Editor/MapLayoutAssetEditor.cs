using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Unity;
using UnityEditor;
using UnityEngine;

namespace hp55games.MareIgnoto.Editor
{
    /// <summary>Inspector di <see cref="MapLayoutAsset"/>: sotto i campi mostra l'esito di Validate().</summary>
    [CustomEditor(typeof(MapLayoutAsset))]
    public sealed class MapLayoutAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var asset = (MapLayoutAsset)target;
            MapValidationResult result = asset.Validate();

            EditorGUILayout.Space();
            if (result.IsValid)
                EditorGUILayout.HelpBox("Layout valido.", MessageType.Info);
            else
                EditorGUILayout.HelpBox(result.ToString(), MessageType.Error);

            if (GUILayout.Button("Valida"))
            {
                if (result.IsValid)
                    Debug.Log("[MapLayout] " + asset.name + ": layout valido.", asset);
                else
                    Debug.LogError("[MapLayout] " + asset.name + ": layout non valido.\n" + result, asset);
            }
        }
    }
}
