using UnityEngine;
using UnityEngine.EventSystems;

public class UIRosaVenti : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        //gameObject.transform.localPosition += new Vector3(0, -50, 0);
        gameObject.transform.localScale *= 3f;
    }

    // Funzione chiamata quando il cursore esce dall'oggetto UI
    public void OnPointerExit(PointerEventData eventData)
    {
        //gameObject.transform.localPosition -= new Vector3(0, -50, 0);
        gameObject.transform.localScale /= 3f;
    }
}
