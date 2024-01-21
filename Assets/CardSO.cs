using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Card", menuName = "Custom/Card")]
public class CardSO : ScriptableObject
{
    [SerializeField] public Sprite cardImage;
    [SerializeField] public CardType cardType;
    [SerializeField] public string cardName;
    [SerializeField] public string cardDescription;
    [SerializeField] public Effect cardActiveEffect;
    [SerializeField] public Effect cardPassiveEffect;
}

public enum CardType
{
    map,
    ship_armament,
    crew,
    island,
    captain
}

[System.Serializable]
public class Effect
{
    public string effectDescription;

}

/* Esempi di effetti passivi:
 * La barca è più veloce.
 * Puoi virare di 2 tacche
 * Quando peschi una carta, prendine 2 e scegline 1
 * Quando devi consumare rum, ne consumi 1 in meno
 * 
 * 
 * 
 * 
 * Esempi di effetti attivi: (diciamo che devo ancora definire meglio le meccaniche di combattimento affinchè capire come procedere)
 * Se giocata contro un cannone, distruggi anche il cannone e guadagna 100 di reputazione
 * Se giocata contro un cannone, puoi decidere di perdere 100 reputazione per salvare la carta
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 */