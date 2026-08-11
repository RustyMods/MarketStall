using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MarketStall;

public class ItemView
{
    private readonly GameObject _element;
    private readonly VerticalLayoutGroup layoutGroup;
    private readonly Scrollbar scrollbar;
    private readonly ScrollRectEnsureVisible scrollRectEnsureVisible;
    private readonly ScrollRect scrollRect;

    private readonly List<ItemElement> elements = [];
    private ItemElement selectedElement;

    public ItemView(GameObject go)
    {
        _element = go.transform.Find("ItemElement").gameObject;
        _element.AddComponent<HoverSelectable>();
        layoutGroup = go.GetComponentInChildren<VerticalLayoutGroup>();
        scrollbar = go.GetComponentInChildren<Scrollbar>();
        scrollRect = go.GetComponentInChildren<ScrollRect>();
        scrollRectEnsureVisible = go.GetComponent<ScrollRectEnsureVisible>();

        scrollRect.scrollSensitivity *= 700;
    }

    public void OnSearch(string input)
    {
        for (int i = 0; i < elements.Count; ++i)
        {
            var element = elements[i];
            element.OnSearch(input);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < layoutGroup.transform.childCount; ++i)
        {
            var child = layoutGroup.transform.GetChild(i);
            Object.Destroy(child.gameObject);
        }
        
        elements.Clear();
    }

    public void Create(MarketInventory.MarketItem marketItem, bool isSelling)
    {
        var go = Object.Instantiate(_element, layoutGroup.transform);
        go.SetActive(true);
        ItemElement element = new ItemElement(go);
        element.Set(marketItem, isSelling, () =>
        {
            selectedElement = selectedElement == element ? null : element;
            UpdateSelected();
        });
        elements.Add(element);
    }

    public void Create(RevenueData revenue)
    {
        var go = Object.Instantiate(_element, layoutGroup.transform);
        go.SetActive(true);
        ItemElement element = new ItemElement(go);
        element.Set(revenue, () =>
        {
            selectedElement = selectedElement == element ? null : element;
            UpdateSelected();
        });
        elements.Add(element);
    }

    private void UpdateSelected()
    {
        for (int i = 0; i < elements.Count; ++i)
        {
            var element = elements[i];
            element.SetActive(selectedElement == element);
        }
    }
    public void SetupBackground(Button original, Sprite bkg)
    {
        ItemElement element = new ItemElement(_element);
        element.SetupBackground(original, bkg);
    }
    
    public void SetSelected(ItemElement element) => selectedElement = element;
    
    public ItemElement GetSelected() => selectedElement;
}