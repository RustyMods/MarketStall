using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MarketStall;

public class ButtonElement(GameObject go)
{
    private readonly Image background = go.GetComponent<Image>();
    private readonly Button button = go.GetComponent<Button>();
    private readonly TextMeshProUGUI text = go.GetComponentInChildren<TextMeshProUGUI>();
    private readonly UITooltip tooltip = go.GetComponent<UITooltip>();

    public void SetLabel(string label) => text.text = Localization.instance.Localize(label);
    public void OnClick(UnityAction action) => button.onClick.AddListener(action);
    public void SetupBackground(Button original, Sprite bkg)
    {
        background.sprite = bkg;
        button.spriteState = original.spriteState;
        button.transition = original.transition;
        if (original.GetComponentInChildren<TextMeshProUGUI>() is { } tmp)
        {
            text.color = tmp.color;
        }
    }

    public void SetTooltip(string topic, string content)
    {
        if (tooltip == null) return;
        tooltip.Set(topic, content);
    }

    public void Show() => button.gameObject.SetActive(true);
    public void Hide() => button.gameObject.SetActive(false);
    public void SetInteractable(bool enable) => button.interactable = enable;
}