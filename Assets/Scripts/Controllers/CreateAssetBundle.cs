using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateAssetBundle : MonoBehaviour
{
    [MenuItem("Assets/Build AssetBundle")]
    static void BuildAssetBundle()
    {
        // Crea un percorso per salvare l'asset bundle
        string assetBundleDirectory = "Assets/AssetBundles";
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        // Seleziona le texture da includere nell'asset bundle
        Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);

        // Imposta il percorso dell'asset bundle
        string assetBundlePath = assetBundleDirectory + "/texturebundle";

        // Crea l'asset bundle
        BuildPipeline.BuildAssetBundle(null, selectedTextures, assetBundlePath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        // Aggiorna il database delle risorse
        AssetDatabase.Refresh();

        Debug.Log("Asset Bundle creato con successo: " + assetBundlePath);
    }
}
