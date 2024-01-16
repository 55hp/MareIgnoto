using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerName;
    public bool canMove;
    public Card[] playerCards;

    public Player(string name)
    {
        playerName = name;
        canMove = true;
    }

    public bool HasActions()
    {
        foreach(var card in playerCards)
        {
            if(card.GetIsPlayable())
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
