using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarketStall;

public class Currency(GameObject go)
{
    public readonly GameObject root = go;
    public readonly Image icon = go.transform.Find("Icon").GetComponent<Image>();
    private readonly TextMeshProUGUI text = go.GetComponentInChildren<TextMeshProUGUI>();
    public readonly UITooltip tooltip = go.GetComponentInChildren<UITooltip>();

    public void SetText(int amount) => text.text = $"x{amount}";
    public void SetTooltip(string tip)
    {
        if (tooltip == null) return;
        tooltip.m_text = Localization.instance.Localize(tip);
    }
}