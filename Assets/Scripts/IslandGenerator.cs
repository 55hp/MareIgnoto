using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class IslandGenerator : MonoBehaviour
{
    public string islandName = "Island_01";
    public Material island_mat;
    Mesh island;

    public int size = 100; // Dimensione dell'isola
    public float heightScale = 10f; // Moltiplicatore per l'altezza
    public float islandRadius = 20f; // Raggio dell'isola
    public int octaves = 4; // Numero di ottave per il rumore di Perlin
    public float lacunarity = 2f; // Lacunarity per il rumore di Perlin
    public float persistence = 0.5f; // Persistence per il rumore di Perlin

    void OnGUI()
    {
        if (GUILayout.Button("Genera Isolette"))
        {
            island = GenerateIslandMesh();
        }

        if (GUILayout.Button("Salva Mesh come Asset"))
        {
            SaveMeshAsAsset(island, islandName);
        }
    }

    Mesh GenerateIslandMesh()
    {
        Mesh islandMesh = new Mesh();
        islandMesh.name = "IslandMesh";

        Vector3[] vertices = new Vector3[size * size];
        int[] triangles = new int[(size - 1) * (size - 1) * 6];
        int triangleIndex = 0;

        Vector2 islandCenter = new Vector2(size / 2, size / 2);

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                float xCoord = (float)x / size * islandRadius;
                float zCoord = (float)z / size * islandRadius;

                float y = CalculateHeight(xCoord, zCoord);

                vertices[x * size + z] = new Vector3(x, y, z);

                // Crea i triangoli
                if (x < size - 1 && z < size - 1)
                {
                    triangles[triangleIndex] = x * size + z;
                    triangles[triangleIndex + 1] = x * size + z + 1;
                    triangles[triangleIndex + 2] = (x + 1) * size + z;
                    triangles[triangleIndex + 3] = (x + 1) * size + z;
                    triangles[triangleIndex + 4] = x * size + z + 1;
                    triangles[triangleIndex + 5] = (x + 1) * size + z + 1;

                    triangleIndex += 6;
                }
            }
        }

        islandMesh.vertices = vertices;
        islandMesh.triangles = triangles;
        islandMesh.RecalculateNormals();

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = islandMesh;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = island_mat;

        return islandMesh;
    }

    float CalculateHeight(float x, float z)
    {
        float xCoord = x / size * islandRadius;
        float zCoord = z / size * islandRadius;

        float height = 0f;
        float amplitude = 1f;

        for (int i = 0; i < octaves; i++)
        {
            float perlinValue = Mathf.PerlinNoise(xCoord, zCoord);
            height += perlinValue * amplitude;

            xCoord *= lacunarity;
            zCoord *= lacunarity;

            amplitude *= persistence;
        }

        return height * heightScale;
    }

    void SaveMeshAsAsset(Mesh mesh, string assetName)
    {
#if UNITY_EDITOR
        string path = "Assets/" + assetName + ".asset";
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        Debug.Log("Mesh salvata come asset: " + path);
#else
        Debug.LogError("Il salvataggio degli asset è supportato solo nell'Editor di Unity.");
#endif
    }
}
