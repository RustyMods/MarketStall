using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarketStall;

public class HoverSelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image bkg;
    public void Awake()
    {
        bkg = transform.Find("Background").gameObject?.GetComponent<Image>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (bkg != null)
        {
            bkg.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (bkg != null)
        {
            bkg.color = new Color(0f, 0f, 0f, 0.5f);
        }
    }
}