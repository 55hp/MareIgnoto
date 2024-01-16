using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Range(2,8)] public int numberOfPlayers = 2;
    public float turnDuration = 30f;
    public bool autoPlayEnabled = true;

    private Player[] players;
    private int currentPlayerIndex = 0;
    private bool isTakingTurn = false;

    void Start()
    {
        InitializePlayers();
        StartCoroutine(StartTurn());
    }

    void InitializePlayers()
    {
        players = new Player[numberOfPlayers];

        for (int i = 0; i < numberOfPlayers; i++)
        {
            players[i] = new Player("Player " + (i + 1));
        }
    }

    IEnumerator StartTurn()
    {
        while (true)
        {
            yield return new WaitForSeconds(turnDuration);

            if (autoPlayEnabled || players[currentPlayerIndex].HasActions())
            {
                StartPlayerTurn();
            }
            else if (players[currentPlayerIndex].CanMove())
            {
                players[currentPlayerIndex].Move();
            }
            else
            {
                PassTurn();
            }
        }
    }

    void StartPlayerTurn()
    {
        isTakingTurn = true;
        Debug.Log("Turno di " + players[currentPlayerIndex].playerName);

        //1. Apri la mano del giocatore

        // Implementa qui la gestione delle azioni del giocatore corrente
        // ...

        // Simula un evento che mette in pausa il turno
        if (Random.Range(0f, 1f) < 0.2f) // Esempio: 20% di probabilità di evento
        {

        }
        else
        {
            // Nessun evento, continua con il turno successivo
            PassTurn();
        }
    }

    public void PassTurn()
    {
        isTakingTurn = false;
        currentPlayerIndex = (currentPlayerIndex + 1) % numberOfPlayers;
        StartCoroutine(StartTurn());
    }
}
