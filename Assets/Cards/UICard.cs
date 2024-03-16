using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class UICard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TextMeshProUGUI cardName;
    [SerializeField] TextMeshProUGUI cardDescription;
    [SerializeField] TextMeshProUGUI cardActive;
    [SerializeField] TextMeshProUGUI cardPassive;

    [SerializeField] Image cardImage;
    [SerializeField] Image cardBg;
    [SerializeField] Image cardBack;
    [SerializeField] Outline cardOutline;


    public bool isPlayable;

    private CardSO cardSO;
    public void Setup(CardSO card)
    {
        cardSO = card;
        SetupGraphics();
        SetupTexts();
        SetIsPlayable(false);
        

    }

    private void SetupGraphics()
    {
        if (cardSO.cardImage != null)
            cardImage.sprite = cardSO.cardImage;
        else
            cardImage.gameObject.SetActive(false);

        cardBack.sprite = AssetDatabaseManager.Instance.GetCardBackByType(cardSO.cardType);
        cardBg.sprite = AssetDatabaseManager.Instance.GetCardBg();
        cardBg.color = AssetDatabaseManager.Instance.GetColorBgByType(cardSO.cardType);
    }

    private void SetupTexts()
    {
        cardName.text = cardSO.cardName;


        //if(cardSO.cardActiveEffect != null)
        //{
        //    cardActive.gameObject.transform.parent.gameObject.SetActive(true);
        //    cardActive.text = cardSO.cardActiveEffect?.effectDescription;
        //}
        //
        //if (cardSO.cardPassiveEffect != null)
        //{
        //    cardPassive.gameObject.transform.parent.gameObject.SetActive(true);
        //    cardPassive.text = cardSO.cardPassiveEffect?.effectDescription;
        //}
        //
        //if(cardSO.cardDescription != null && cardSO.cardPassiveEffect == null)
        //{
        //    cardDescription.GetComponentInParent<Transform>().gameObject.SetActive(true);
        //    cardDescription.text = cardSO.cardDescription;
        //}

    }

    public bool GetIsPlayable()
    {
        //verifica le condizioni e setta is playable prima di restituire


        return isPlayable;
    }

    internal void SetIsPlayable(bool value)
    {
        isPlayable = value;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        cardOutline.enabled = true;
        cardOutline.gameObject.transform.localPosition += new Vector3(0, 40, 0);
        cardOutline.gameObject.transform.localScale *= 1.5f;
    }

    // Funzione chiamata quando il cursore esce dall'oggetto UI
    public void OnPointerExit(PointerEventData eventData)
    {
        cardOutline.enabled = false;
        cardOutline.gameObject.transform.localPosition -= new Vector3(0, 40, 0);
        cardOutline.gameObject.transform.localScale /= 1.5f;
    }
}
