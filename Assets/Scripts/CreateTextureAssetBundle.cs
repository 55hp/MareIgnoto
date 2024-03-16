using UnityEngine;
using UnityEditor;

public class CreateTextureAssetBundle : EditorWindow
{
    [MenuItem("Assets/Build Texture Asset Bundle")]
    static void BuildTextureAssetBundle()
    {
        // Seleziona le texture da includere nell'asset bundle
        Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);

        // Imposta il percorso dell'asset bundle
        string assetBundlePath = EditorUtility.SaveFilePanel("Save Texture Asset Bundle", "", "textureBundle", "unity3d");

        // Crea l'asset bundle
        BuildPipeline.BuildAssetBundle(null, selectedTextures, assetBundlePath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        // Aggiorna il database delle risorse
        AssetDatabase.Refresh();

        Debug.Log("Texture Asset Bundle creato con successo: " + assetBundlePath);
    }
}

