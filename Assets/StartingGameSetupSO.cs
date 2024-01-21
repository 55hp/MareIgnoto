using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StartingGameSetupSO", menuName = "Custom/StartingGameSetupSO")]
public class StartingGameSetupSO : ScriptableObject
{
    [Range(2, 8)] public int player_amount = 2;
    public Vector2[] spawnPoints;
}
