using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    [SerializeField] MapNode nodePrefab;
    [SerializeField] PlayerPawn playerPrefab;
    public int numberOfNodes = 49;
    [SerializeField] Transform topLeftCorner;
    [SerializeField] Transform bottomRightCorner;
    private Dictionary<Vector2, MapNode> nodes = new Dictionary<Vector2, MapNode>();

    private List<PlayerPawn> playerPawns;

    public void InitGrid()
    {
        //Calcola la distanza sull'asse x tra topLeftCorner e bottomRightCorner essendo quadrati non serve calcolare anche la distanza in verticale
        float horizontaDistance = Mathf.Abs(bottomRightCorner.position.x) + Mathf.Abs(topLeftCorner.position.x);

        //Ora calcola la distanza che deve esserci tra ogni nodo
        float nodeDistance = horizontaDistance / (numberOfNodes - 1);

        //Visto che è un quadrato faccio un ciclo per lato
        for (int i = 0; i < numberOfNodes; i++)
        {
            for (int k = 0; k < numberOfNodes; k++)
            {
                //Cerco la posizione del nodo
                Vector3 positionToPlaceTheNode = new Vector3(topLeftCorner.position.x + (nodeDistance * k), 0, topLeftCorner.position.z - (nodeDistance * i));

                //Istanzio il nodo
                MapNode instantiatedNode = Instantiate(nodePrefab, positionToPlaceTheNode, Quaternion.identity);

                //Gli assegno un nome
                instantiatedNode.gameObject.name = instantiatedNode.name + "_" + i.ToString() + "_" + k.ToString();
                instantiatedNode.transform.parent = transform;
                nodes.Add(new Vector2(i, k), instantiatedNode);
            }
        }
        
        //Una volta pronta la griglia passiamo ad instanziare i giocatori
        SpawnPlayers();

    }

    public MapNode PlaceBoat(Player boat , Vector2 position)
    {
        MapNode node = GetNodeBySpawnPointPosition(position);
        node.HoldIt(boat);
        return node;
    }

    #region Private Methods

    private void SpawnPlayers()
    {
        if (GameManager.Instance.players.Count != GameManager.Instance.start.spawnPoints.Count)
            return;

        playerPawns = new List<PlayerPawn>();


        for (int i = 0; i < GameManager.Instance.players.Count; i++)
        {
            MapNode node = PlaceBoat(GameManager.Instance.players[i] , GameManager.Instance.start.spawnPoints[i].position);   

            PlayerPawn pawn = Instantiate(playerPrefab, new Vector3(node.transform.position.x, node.transform.position.y + 1 , node.transform.position.z), Quaternion.identity);
            playerPawns.Add(pawn);
        }
    }

    private MapNode GetNodeBySpawnPointPosition(Vector2 position)
    {
        return nodes[position];
    }





    #endregion
}
