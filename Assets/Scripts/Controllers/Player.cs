using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerName;
    public bool canMove;
    public UICard[] playerCards;

    public Player(string name)
    {
        playerName = name;
        canMove = true;
    }

    public bool HasActions()
    {
        foreach(var card in playerCards)
        {
            if(card.isPlayable)
                return true;
        }
        return false; // Modifica questo in base alla tua logica
    }

    internal void Move()
    {
        throw new NotImplementedException();
    }

    internal bool CanMove()
    {
        return canMove;
    }
}
