using MarketStall.MarketStall;
using MarketStall.Utility;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarketStall.UI;

public class SellElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image m_icon = null!;
    public Text m_stack = null!;
    public Text m_name = null!;
    public Image m_currency = null!;
    public Text? m_merchant;
    public RectTransform m_starRoot = null!;
    public Button m_button = null!;
    public InputField m_priceInput = null!;
    public ItemDrop.ItemData? m_itemData;
    public void Awake()
    {
        m_icon = Utils.FindChild(transform, "$part_item_icon").GetComponent<Image>();
        m_stack = Utils.FindChild(transform, "$part_item_stack").GetComponent<Text>();
        m_name = Utils.FindChild(transform, "$part_item_name").GetComponent<Text>();
        m_currency = Utils.FindChild(transform, "$part_price_icon").GetComponent<Image>();
        if (Utils.FindChild(transform, "$part_merchant_name") is { } merchant && merchant.TryGetComponent(out Text component))
        {
            m_merchant = component;
        }

        m_starRoot = Utils.FindChild(transform, "$part_stars_root").GetComponent<RectTransform>();
        m_button = Utils.FindChild(transform, "$part_sell_button").GetComponent<Button>();
        m_priceInput = Utils.FindChild(transform, "$part_price").GetComponent<InputField>();
    }

    public void Setup(Humanoid user, ItemDrop.ItemData data, UnityAction onSell, bool community = false)
    {
        GameObject Currency = Methods.TryGetPrefab(MarketStallPlugin._Currency.Value);
        if (!Currency.TryGetComponent(out ItemDrop currencyComponent)) return;
        m_icon.sprite = data.GetIcon();
        m_stack.text = data.m_stack.ToString();
        m_name.text = Localization.instance.Localize(data.m_shared.m_name);
        m_currency.sprite = currencyComponent.m_itemData.GetIcon();
        if (community && m_merchant is not null)
        {
            m_merchant.text = user.GetHoverName();
        }

        if (data.m_quality > 1)
        {
            for(int index = 0; index < data.m_quality; ++index) Object.Instantiate(Marketplace.MarketStar, m_starRoot);
        }

        m_priceInput.characterLimit = currencyComponent.m_itemData.m_shared.m_maxStackSize.ToString().Length;
        m_priceInput.onValueChanged.AddListener(Marketplace.SetPrice);
        m_button.onClick.AddListener(onSell);
        m_itemData = data;

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        InventoryGUI_Patches.m_overrideUpdateRecipe = true;
        InventoryGUI_Patches.m_selectedItemData = m_itemData;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryGUI_Patches.m_overrideUpdateRecipe = false;
    }
}