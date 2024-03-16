using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeshGenerator : MonoBehaviour
{
    [Header("Generic")]
    public Material material_placeholder;
    public string meshName;

    [Header("Geometric")]
    public int verticiDellaBase = 4;
    public float altezza = 5f;

    [Header("---")]
    



    private Mesh g_mesh;
    private int counter = 0;
    private void OnGUI()
    {
        if (GUILayout.Button("Genera Piramide"))
        {
            g_mesh = GeneratePyramid(verticiDellaBase,altezza);
            GameObject obj = new GameObject("mesh_" + counter);
            counter++;

            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();

            obj.GetComponent<MeshFilter>().mesh = g_mesh;
            obj.GetComponent<MeshRenderer>().material = material_placeholder;
        }

        if (GUILayout.Button("Salva Mesh come Asset"))
        {
            SaveMeshAsAsset(g_mesh, meshName);
        }
    }

    Mesh GeneratePyramid(int baseVertices, float height)
    {
        Mesh pyramidMesh = new Mesh();
        pyramidMesh.name = "PyramidMesh";

        Vector3[] vertices = new Vector3[baseVertices + 1]; // +1 per la cima della piramide
        int[] triangles = new int[baseVertices * 3 * 2]; // Ogni triangolo ha 3 vertici, e ci sono 2 triangoli per ogni lato della piramide
        Vector3[] normals = new Vector3[baseVertices + 1]; // Normals per i vertici

        // Calcola i vertici della base
        for (int i = 0; i < baseVertices; i++)
        {
            float angle = (float)i / baseVertices * 2f * Mathf.PI;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            vertices[i] = new Vector3(x, 0f, z);
            normals[i] = Vector3.down; // Normale verso il basso per la base
        }

        // Aggiungi il vertice in cima alla piramide
        vertices[baseVertices] = new Vector3(0f, height, 0f);
        normals[baseVertices] = Vector3.up; // Normale verso l'alto per la cima

        // Calcola i triangoli della base
        for (int i = 0; i < baseVertices - 1; i++)
        {
            triangles[i * 6] = (i + 1) % baseVertices;
            triangles[i * 6 + 1] = i;
            triangles[i * 6 + 2] = baseVertices;

            triangles[i * 6 + 3] = (i + 2) % baseVertices;
            triangles[i * 6 + 4] = (i + 1) % baseVertices;
            triangles[i * 6 + 5] = baseVertices;
        }

        // Ultimo triangolo per chiudere la base
        triangles[(baseVertices - 1) * 6] = 0;
        triangles[(baseVertices - 1) * 6 + 1] = baseVertices - 1;
        triangles[(baseVertices - 1) * 6 + 2] = baseVertices;

        triangles[(baseVertices - 1) * 6 + 3] = 1;
        triangles[(baseVertices - 1) * 6 + 4] = 0;
        triangles[(baseVertices - 1) * 6 + 5] = baseVertices;

        pyramidMesh.vertices = vertices;
        pyramidMesh.triangles = triangles;
        pyramidMesh.normals = normals;

        return pyramidMesh;
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
