using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Custom/Card")]
public class CardSO : ScriptableObject
{
    [SerializeField] public Sprite cardImage;
    [SerializeField] public CardType cardType;
    [SerializeField] public string cardName;
    [SerializeField] public string cardDescription;
    [SerializeField] public Effect[] effects;
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
    public EffectType effectType;
    public int effectId;
    public string effectDescription;

}

public enum EffectType
{
    //Fight effects
    kill_crew,          //Se interfacciata con una carta crew, la uccide
    kill_or_steal,      //Se interfacciata con una carta crew, l'avversario lancia una moneta, se esce testa la uccide se esce croce ruba 2 bottiglie di rum
    kill_or_kidnap,     //Se interfacciata con una carta crew, l'avversario lancia una moneta, se esce testa la uccide se esce croce ruba 2 bottiglie di rum

    //Islands                   //Quando attracchi su un isola scegli una carta crew da far scendere - una volta salpato quella carte non può essere usata nè sotto coperta nè sopraccoperta per 1 turno
    trade,              //Commerciante : apre il negozio
    dockers_pub,        //Puoi rifornirti di rum
    hidden_treasure,    //Ottieni un tesoro nascoso (può essere una cassa di rum o altro)
    surprise_fight,     //Combatti

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