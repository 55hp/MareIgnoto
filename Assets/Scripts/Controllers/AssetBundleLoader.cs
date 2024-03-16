using System.Collections;
using UnityEngine;

public class AssetBundleLoader : MonoBehaviour
{
    // Percorso dell'asset bundle
    public string assetBundleURL;

    // Nome della texture da caricare dall'asset bundle
    public string textureName;

    IEnumerator Start()
    {
        // Carica l'asset bundle dall'URL specificato
        using (WWW www = new WWW(assetBundleURL))
        {
            yield return www;

            if (www.error != null)
            {
                Debug.Log("Error loading asset bundle: " + www.error);
                yield break;
            }

            // Estrai l'asset bundle
            AssetBundle bundle = www.assetBundle;

            // Carica la texture dall'asset bundle
            Texture2D texture = bundle.LoadAsset<Texture2D>(textureName);

            // Applica la texture a un oggetto della scena (per esempio un quad)
            GetComponent<Renderer>().material.mainTexture = texture;

            // Rilascia l'asset bundle
            bundle.Unload(false);
        }
    }
}


