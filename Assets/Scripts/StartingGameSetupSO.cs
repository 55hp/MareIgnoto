using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StartingGameSetupSO", menuName = "Custom/StartingGameSetupSO")]
public class StartingGameSetupSO : ScriptableObject
{
    [Range(2,8)]public int playerNumber;
    public List<SpawnPoint> spawnPoints;
}

[System.Serializable]
public class SpawnPoint
{
    public Vector2 position;
}
