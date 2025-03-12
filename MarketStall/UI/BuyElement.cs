using HarmonyLib;
using MarketStall.MarketStall;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarketStall.UI;

public class BuyElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image m_icon = null!;
    public Text m_name = null!;
    public Image m_currency = null!;
    public Text m_price = null!;
    public Text m_stack = null!;
    public Button m_button = null!;
    public RectTransform m_starRoot = null!;
    public Text? m_merchant;

    public MarketData.MarketTradeItem? m_item;
    public ItemDrop.ItemData? m_itemData;

    public void Awake()
    {
        m_icon = Utils.FindChild(transform, "$part_item_icon").GetComponent<Image>();
        m_name = Utils.FindChild(transform, "$part_item_name").GetComponent<Text>();
        m_currency = Utils.FindChild(transform, "$part_price_icon").GetComponent<Image>();
        m_price = Utils.FindChild(transform, "$part_price").GetComponent<Text>();
        m_button = Utils.FindChild(transform, "$part_buy_button").GetComponent<Button>();
        m_stack = Utils.FindChild(transform, "$part_item_stack").GetComponent<Text>();
        m_starRoot = Utils.FindChild(transform, "$part_stars_root").GetComponent<RectTransform>();
        if (Utils.FindChild(transform, "$part_merchant_name") is { } merchantName &&
            merchantName.TryGetComponent(out Text component))
        {
            m_merchant = component;
        }
    }

    public void Setup(ItemDrop.ItemData data, Sprite? currency, MarketData.MarketTradeItem item, UnityAction onClick, bool community = false)
    {
        m_icon.sprite = data.GetIcon();
        m_name.text = Localization.instance.Localize(data.m_shared.m_name);
        m_currency.sprite = currency;
        m_stack.text = item.m_stack.ToString();
        m_price.text = item.m_price == 0 ? Localization.instance.Localize("$market_free") : item.m_price.ToString();
        m_button.onClick.AddListener(onClick);
        if (community && m_merchant is not null) m_merchant.text = item.m_merchant;
        if (item.m_quality > 1)
        {
            for (int index = 0; index < item.m_quality; ++index) Object.Instantiate(Marketplace.MarketStar, m_starRoot);
        }

        m_item = item;
        m_itemData = data;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_item is null || m_itemData is null) return;
        InventoryGUI_Patches.m_item = m_item;
        InventoryGUI_Patches.m_selectedItemData = m_itemData;
        InventoryGUI_Patches.m_overrideUpdateRecipe = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryGUI_Patches.m_overrideUpdateRecipe = false;
    }

}