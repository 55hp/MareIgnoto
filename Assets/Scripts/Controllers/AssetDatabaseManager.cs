using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AssetDatabaseManager : MonoBehaviour
{
    public static AssetDatabaseManager Instance;
    [SerializeField] public Sprite[] cardBacks;
    [SerializeField] public Sprite cardBg;
    [SerializeField] Color[] cardBgColors; 
    [SerializeField] CardSO[] allCards; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    internal CardSO GetRandomCard()
    {
        int r = UnityEngine.Random.Range(0,allCards.Length);
        return allCards[r];
    }

    internal Sprite GetCardBg()
    {
        return cardBg;
    }

    internal Color GetColorBgByType(CardType cardType)
    {
        int index = 0;
        switch (cardType)
        {
            case CardType.map:
                index = 1;
                break;
            case CardType.ship_armament:
                index = 2;
                break;
            case CardType.crew:
                index = 3;
                break;
            case CardType.captain:
                index = 4;
                break;
            case CardType.island:
                index = 5;
                break;
        }

        return cardBgColors[index];
    }

    internal Sprite GetCardBackByType(CardType cardType)
    {
        int index = -1;
        switch (cardType)
        {
            case CardType.map:
                index = 1;
                break;
            case CardType.ship_armament:
                index = 2;
                break;
            case CardType.crew:
                index = 3;
                break;
            case CardType.captain:
                index = 3;
                break;
            case CardType.island:
                index = 4;
                break;
        }

        if (index < 0)
            return null;

        return cardBacks[index];
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
