using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    [SerializeField] MapNode nodePrefab;
    [SerializeField] MapNode spawnerPrefab;

    public int numberOfNodes = 49;
    [SerializeField] Transform topLeftCorner;
    [SerializeField] Transform bottomRightCorner;
    [SerializeField] StartingGameSetupSO start;

    private Dictionary<Vector2, MapNode> nodes = new Dictionary<Vector2, MapNode>();


    // Start is called before the first frame update
    void Start()
    {
        if(start != null)
            InitGrid(start);
    }

    public void InitGrid(StartingGameSetupSO setup)
    {
        //Calcola la distanza sull'asse x tra topLeftCorner e bottomRightCorner essendo quadrati non serve calcolare anche la distanza in verticale
        float horizontaDistance = Mathf.Abs(bottomRightCorner.position.x) + Mathf.Abs(topLeftCorner.position.x);

        //Ora calcola la distanza che deve esserci tra ogni nodo
        float nodeDistance = horizontaDistance / (numberOfNodes - 1);

        for (int i = 0; i < numberOfNodes; i++)
        {
            for (int k = 0; k < numberOfNodes; k++)
            {
                MapNode prefabToUse = GetPrefabToUse(i, k , setup.spawnPoints);
                Vector3 positionToPlaceTheNode = new Vector3(topLeftCorner.position.x + (nodeDistance * k), 0, topLeftCorner.position.z - (nodeDistance * i));
                MapNode n = Instantiate(prefabToUse, positionToPlaceTheNode, Quaternion.identity);
                n.gameObject.name = n.name + "_" + i.ToString() + "_" + k.ToString();
                n.transform.parent = transform;
                nodes.Add(new Vector2(i, k), n);
            }
        }
        Debug.Log($"Istanziati ben {nodes.Count} nodi.");
    }

    private MapNode GetPrefabToUse(int i , int k , Vector2[] startingSetupSpawnPoints)
    {
        MapNode prefabToUse = nodePrefab;
        foreach (Vector2 sp in startingSetupSpawnPoints)
        {
            if (sp.x == i && sp.y == k)
            {
                prefabToUse = spawnerPrefab;
                break;
            }
        }
        return prefabToUse;
    }

}
