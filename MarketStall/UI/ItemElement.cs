using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MarketStall;

public class ItemElement : TextReceiver
{
    private MarketInventory.MarketItem _marketItem;
    private RevenueData _revenue;
    private readonly ItemElement _itemElement;
    private bool isUpdating;

    private GameObject instance;
    private readonly Image background;
    private readonly Image selected;
    private readonly Image magicBackground;
    private readonly Image icon;
    private readonly TextMeshProUGUI name;
    private readonly Image currency;
    private readonly TextMeshProUGUI price;
    private readonly ButtonElement triggerButton;
    private readonly Button mainButton;
    private readonly UITooltip tooltip;

    public ItemElement(GameObject go)
    {
        instance = go;
        _itemElement = this;
        background = go.transform.Find("Background").GetComponent<Image>();
        selected = go.transform.Find("Selected").GetComponent<Image>();
        magicBackground = go.transform.Find("MagicBG").GetComponent<Image>();
        icon = go.transform.Find("Icon").GetComponent<Image>();
        name = go.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        currency = go.transform.Find("Currency").GetComponent<Image>();
        price = go.transform.Find("Price").GetComponent<TextMeshProUGUI>();
        triggerButton = new ButtonElement(go.transform.Find("Button").gameObject);
        mainButton = go.GetComponent<Button>();
        tooltip = go.GetComponent<UITooltip>();
        
        magicBackground.gameObject.SetActive(false);
    }

    public void OnSearch(string input)
    {
        instance.SetActive(name.text.ToLower().Contains(input.ToLower()));
    }
    
    public void SetActive(bool active) => selected.gameObject.SetActive(active);

    public void Set(RevenueData data, UnityAction onClick = null)
    {
        _revenue = data;
        if (onClick != null) mainButton.onClick.AddListener(onClick);
        icon.sprite = data.item.GetIcon();
        name.text = Localization.instance.Localize(data.item.m_shared.m_name);
        if (data.item.m_stack > 1) name.text += $" x{data.item.m_stack}";
        currency.enabled = false;
        price.enabled = false;
        tooltip.Set(data.item.m_shared.m_name, data.item.GetTooltip());
        triggerButton.Show();
        triggerButton.SetLabel("$market_collect");
        triggerButton.OnClick(() =>
        {
            if (!Player.m_localPlayer.GetInventory().CanAddItem(data.item))
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noroom");
                return;
            }
            MarketGUI.instance.currentMarket.RemoveRevenue(data.item, data.merchant);
            Player.m_localPlayer.GetInventory().AddItem(data.item);
        });
    }
    public void Set(MarketInventory.MarketItem marketItem, bool isSelling, UnityAction onClick = null)
    {
        _marketItem = marketItem;

        if (onClick != null) mainButton.onClick.AddListener(onClick);
        icon.sprite = marketItem.item.GetIcon();
        name.text = Localization.instance.Localize(marketItem.item.m_shared.m_name);
        if (marketItem.item.m_stack > 1) name.text += $" x{marketItem.item.m_stack}";
        currency.sprite = marketItem.currency.GetIcon();
        price.text = marketItem.price.ToString();
        if (marketItem.item.m_customData.TryGetValue("randyknapp.mods.epicloot#EpicLoot.MagicItemComponent", out string json))
        {
            if (!string.IsNullOrEmpty(json) && Utils.GetRarity(json, out int rarity))
            {
                magicBackground.gameObject.SetActive(true);
                switch (rarity)
                {
                    case 0: // Magic - blue
                        magicBackground.color = new Color(0.20f, 0.55f, 0.95f, 1f);
                        break;
                    case 1: // Rare - yellow
                        magicBackground.color = new Color(1.00f, 0.85f, 0.10f, 1f);
                        break;
                    case 2: // Epic - purple
                        magicBackground.color = new Color(0.55f, 0.15f, 0.85f, 1f);
                        break;
                    case 3: // Legendary - teal
                        magicBackground.color = new Color(0.10f, 0.85f, 0.65f, 1f);
                        break;
                    case 4: // Mythic - gold
                        magicBackground.color = new Color(0.85f, 0.65f, 0.13f, 1f);
                        break;

                    default:
                        magicBackground.gameObject.SetActive(false);
                        break;
                }
            }
        }
        tooltip.Set(
            marketItem.item.m_shared.m_name, 
            marketItem.item.GetTooltip() + 
            $"\n$market_price: <color=yellow>{marketItem.currency.m_shared.m_name} x{marketItem.price}</color>" + 
            $"\n$market_merchant: <color=orange>{marketItem.merchant}</color>");
        
        if (isSelling)
        {
            triggerButton.Show();
            if (string.IsNullOrEmpty(marketItem.uniqueID))
            {
                triggerButton.SetLabel("$market_setprice");
                triggerButton.OnClick(() =>
                {
                    TextInput.instance.RequestText(_itemElement, "$market_setprice", 100);
                    isUpdating = false;
                });
            }
            else
            {
                triggerButton.SetLabel("$market_update");
                triggerButton.OnClick(() =>
                {
                    TextInput.instance.RequestText(_itemElement, "$market_updateprice", 100);
                    isUpdating = true;
                });
            }
        }
        else
        {
            triggerButton.Show();
            triggerButton.SetLabel("$market_buy");
            var playerCount = Player.m_localPlayer.GetInventory().CountItems(GetCurrencySharedName());
            triggerButton.SetInteractable(playerCount >= GetPrice());
            triggerButton.OnClick(() =>
            {
                if (MarketGUI.instance.currentMarket == null) return;
                if (playerCount >= GetPrice())
                {
                    MarketGUI.instance.currentMarket.m_inventory.RemoveItem(_marketItem.uniqueID);
                    Player.m_localPlayer.GetInventory().AddItem(_marketItem.item);
                    ItemDrop.ItemData stallRevenue = _marketItem.currency.Clone();
                    stallRevenue.m_dropPrefab = _marketItem.currency.m_dropPrefab;
                    stallRevenue.m_stack = GetPrice();
                    MarketGUI.instance.currentMarket.AddRevenue(stallRevenue, _marketItem.merchant);
                    Player.m_localPlayer.GetInventory().RemoveItem(GetCurrencySharedName(), GetPrice());
                }
            });
        }
    }

    public void SetupBackground(Button original, Sprite bkg) => triggerButton.SetupBackground(original, bkg);

    public string GetText() => $"{_marketItem.currency.m_dropPrefab.name}, {price.text}";

    public void SetText(string text)
    {
        if (!isUpdating && !Player.m_localPlayer.GetInventory().ContainsItem(_marketItem.item)) return;
        
        var strArray = text.Split(',');
        if (strArray.Length > 1)
        {
            var itemPrefabName = strArray.First().Trim();
            var cost = int.TryParse(strArray.Last().Trim(), out int c) ? c : 0;
            var itemPrefab = ObjectDB.instance.GetItemPrefab(itemPrefabName);
            if (itemPrefab == null)
            {
                if (!Utils.itemByLocalizedName.TryGetValue(itemPrefabName, out itemPrefab)) return;
            }
            if (!itemPrefab.TryGetComponent(out ItemDrop component)) return;
            
            currency.sprite = component.m_itemData.GetIcon();
            _marketItem.currency = component.m_itemData.Clone();
            _marketItem.currency.m_dropPrefab = itemPrefab;
            _marketItem.price = cost;
        }
        else
        {
            if (!int.TryParse(text.Trim(), out int cost)) return;
            _marketItem.price = cost;
        }
        
        price.text = _marketItem.price.ToString();
        tooltip.Set(_marketItem.item.m_shared.m_name, _marketItem.item.GetTooltip() + $"\n\n$market_price: <color=yellow>{_marketItem.currency.m_shared.m_name} x{_marketItem.price}</color>");
        MarketGUI.instance.currentMarket.m_inventory.AddItem(_marketItem);
        Player.m_localPlayer.GetInventory().RemoveItem(_marketItem.item);
    }

    public string GetCurrencySharedName() => _marketItem.currency.m_shared.m_name;
    public int GetPrice() => _marketItem.price;
}