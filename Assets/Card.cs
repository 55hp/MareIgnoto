using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card
{
    private bool isPlayable;

    public bool GetIsPlayable()
    {
        //verifica le condizioni e setta is playable prima di restituire


        return isPlayable;
    }

    internal void SetIsPlayable(bool value)
    {
        isPlayable = value;
    }
}
