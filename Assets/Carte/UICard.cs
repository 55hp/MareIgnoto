using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class UICard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Outline cardOutline;

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
