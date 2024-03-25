using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using MarketStall.MarketStall;
using MarketStall.Utility;
using UnityEngine;
using UnityEngine.UI;
using YamlDotNet.Serialization;
using Object = UnityEngine.Object;

namespace MarketStall.UI;

public static class Marketplace
{
    private static GameObject MarketGUI = null!;
    private static GameObject m_BuyItem = null!;
    private static GameObject m_SellItem = null!;
    private static RectTransform ListRoot = null!;

    private static GameObject MerchantRevenue = null!;
    private static Text RevenueText = null!;
    public static Text RevenueValue = null!;
    private static Text MarketTitle = null!;
    private static Image MarketBackground = null!;
    private static GameObject MarketStar = null!;
    public static ZNetView CurrentMarket = null!;
    public enum MarketType { Buy, Sell }
    public static MarketType MarketTypeIs;
    private static int price = 1;

    private static void UpdateRevenue(ZNetView znv)
    {
        float value = znv.GetZDO().GetInt(Market._MarketValue);
        RevenueValue.text = value.ToString("0");
    }
    public static void ToggleMarketGUI() => MarketGUI.SetActive(!IsMarketVisible()); 
    public static void ShowGUI(ZNetView znv, bool isCreator)
    {
        MarketGUI.SetActive(true);
        znv.ClaimOwnership();
        if (!znv.IsValid()) return;
        CurrentMarket = znv;
        OpenMarket(znv, Player.m_localPlayer, isCreator);
        MerchantRevenue.SetActive(isCreator);
        if (isCreator) UpdateRevenue(znv);
        MarketBackground.color = MarketStallPlugin._TransparentBackground.Value is MarketStallPlugin.Toggle.On
            ? Color.clear
            : Color.white;
        InventoryGui.instance.Show(null);
        string name = znv.GetZDO().GetString(Market._MarketName);
        MarketTitle.text = Localization.instance.Localize($"{name} $market_label");
    }
    private static void OpenMarket(ZNetView znv, Humanoid user, bool isCreator)
    {
        if (znv == null || !znv.IsValid()) return;
        if (isCreator)
        {
            UpdateSellMarket(znv, user);
        }
        else
        {
            UpdateBuyMarket(znv, user);
        }
    }
    public static bool IsMarketVisible() => MarketGUI && MarketGUI.activeInHierarchy;
    public static void UpdateSellMarket(ZNetView znv, Humanoid user, string filter = "")
    {
        DestroyMarketItems();
        MarketTypeIs = MarketType.Sell;
        List<ItemDrop.ItemData> inventory = user.GetInventory().m_inventory;
        foreach (ItemDrop.ItemData data in inventory)
        {
            if (Filters.ServerIgnoreList.Value.Contains(data.m_shared.m_name)) continue;
            string LocalizedName = Localization.instance.Localize(data.m_shared.m_name);
            if (!LocalizedName.ToLower().Contains(filter.ToLower())) continue;
            GameObject element = Object.Instantiate(m_SellItem, ListRoot);
            
            GameObject Currency = Methods.TryGetPrefab(MarketStallPlugin._Currency.Value);
            if (!Currency.TryGetComponent(out ItemDrop currencyComponent)) continue;
            Transform StarsRoot = Utils.FindChild(element.transform, "$part_stars_root");
            
            Utils.FindChild(element.transform, "$part_item_icon").GetComponent<Image>().sprite = data.GetIcon();;
            Utils.FindChild(element.transform, "$part_item_stack").GetComponent<Text>().text = data.m_stack.ToString(CultureInfo.CurrentCulture);
            Utils.FindChild(element.transform, "$part_item_name").GetComponent<Text>().text = LocalizedName;;
            Utils.FindChild(element.transform, "$part_price_icon").GetComponent<Image>().sprite = currencyComponent.m_itemData.GetIcon();
            if (data.m_quality > 1)
            {
                for (int index = 0; index < data.m_quality; ++index) Object.Instantiate(MarketStar, StarsRoot);
            }
            if (Utils.FindChild(element.transform, "$part_price").TryGetComponent(out InputField ItemPrice))
            {
                ItemPrice.characterLimit = currencyComponent.m_itemData.m_shared.m_maxStackSize.ToString().Length;
                ItemPrice.onValueChanged.AddListener(SetPrice);
            }

            if (Utils.FindChild(element.transform, "$part_sell_button").TryGetComponent(out Button SellButton))
            {
                SellButton.onClick.AddListener(() =>
                {
                    if (!data.m_dropPrefab) return;
                    if (!data.m_dropPrefab.TryGetComponent(out ItemDrop component)) return;
                    int MarketCount = Market.GetMarketData(CurrentMarket).Count;
                    if (MarketCount >= MarketStallPlugin._MaxSales.Value)
                    {
                        Methods.ShowMessage(user, "<color=red>$maximum_items</color>");
                        return;
                    }

                    if (!SellItem(user, component, data, data.m_stack, price, currencyComponent, znv)) return;
                    UpdateSellMarket(znv, user);
                    price = 1;
                });
            }
        }
    }

    private static void SetPrice(string input)
    {
        try
        {
            price = int.Parse(input);
        }
        catch
        {
            price = 1;
        }
    }

    private static bool SellItem(Humanoid user, ItemDrop item, ItemDrop.ItemData data, int stack, int cost, ItemDrop currency, ZNetView znv)
    {
        Inventory inventory = user.GetInventory();
        string LocalizedCurrency = Localization.instance.Localize(currency.m_itemData.m_shared.m_name);
        string LocalizedItem = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
        if (MarketStallPlugin._Fee.Value > 0 && MarketStallPlugin._UseSalesTax.Value is MarketStallPlugin.Toggle.On)
        {
            int fee = Mathf.Max(Mathf.CeilToInt(cost * (MarketStallPlugin._Fee.Value / 100f)), MarketStallPlugin._MinimumFee.Value);
            if (!inventory.HaveItem(currency.m_itemData.m_shared.m_name))
            {
                Methods.ShowMessage(user, $"$require_a_fee {fee} {LocalizedCurrency}", item.m_itemData.GetIcon());
                return false;
            }

            ItemDrop.ItemData prefab = inventory.GetItem(currency.m_itemData.m_shared.m_name, isPrefabName: false);
            if (prefab.m_stack < fee)
            {
                Methods.ShowMessage(user, $"$not_enough {LocalizedCurrency}: {fee}/{prefab.m_stack}", item.m_itemData.GetIcon());
                return false;
            }

            inventory.RemoveItem(currency.m_itemData.m_shared.m_name, fee);
            Methods.ShowMessage(user, $"$market_placed {LocalizedItem} $market_x {stack} $market_for_a_fee {fee}", item.m_itemData.GetIcon());
        }

        inventory.RemoveItem(item.m_itemData.m_shared.m_name, stack, data.m_quality);
        Market.AddMarketItem(item, currency, data, stack, cost, znv);
        return true;
    }

    private static void UpdateBuyMarket(ZNetView znv, Humanoid user, string filter = "")
    {
        DestroyMarketItems();
        MarketTypeIs = MarketType.Buy;
        List<MarketData.MarketTradeItem> data = Market.GetMarketData(znv).OrderBy(x => x.m_price).ToList();
        foreach (MarketData.MarketTradeItem item in data)
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(item.m_prefab);
            ItemDrop prefabItemDrop = prefab.GetComponent<ItemDrop>();
            string LocalizedName = Localization.instance.Localize(prefabItemDrop.m_itemData.m_shared.m_name);

            if (!LocalizedName.ToLower().Contains(filter.ToLower())) continue;
            
            GameObject element = Object.Instantiate(m_BuyItem, ListRoot);
            GameObject currencyPrefab = ObjectDB.instance.GetItemPrefab(item.m_currency);
            if (!currencyPrefab.TryGetComponent(out ItemDrop currencyItemDrop)) continue;
            Transform StarsRoot = Utils.FindChild(element.transform, "$part_stars_root");
            
            Utils.FindChild(element.transform, "$part_item_icon").GetComponent<Image>().sprite = prefabItemDrop.m_itemData.GetIcon();
            Utils.FindChild(element.transform, "$part_item_name").GetComponent<Text>().text = LocalizedName;
            Utils.FindChild(element.transform, "$part_price_icon").GetComponent<Image>().sprite = currencyItemDrop.m_itemData.GetIcon();
            Utils.FindChild(element.transform, "$part_price").GetComponent<Text>().text = item.m_price == 0 ? Localization.instance.Localize("$market_free") : item.m_price.ToString(CultureInfo.CurrentCulture);
            Utils.FindChild(element.transform, "$part_buy_button").GetComponent<Button>().onClick.AddListener(() =>
            {
                if (!BuyItem(user, prefabItemDrop, item, currencyItemDrop, znv)) return;
                UpdateBuyMarket(znv, user);
            });;
            Utils.FindChild(element.transform, "$part_item_stack").GetComponent<Text>().text = item.m_stack.ToString(CultureInfo.CurrentCulture);
            if (item.m_quality > 1)
            {
                for (int index = 0; index < item.m_quality; ++index) Object.Instantiate(MarketStar, StarsRoot);
            }
        }
    }
    
    private static bool BuyItem(Humanoid user, ItemDrop item, MarketData.MarketTradeItem data, ItemDrop currency, ZNetView znv)
    {
        string currencyName = Localization.instance.Localize(currency.m_itemData.m_shared.m_name);
        if (!user.GetInventory().HaveItem(currency.m_itemData.m_shared.m_name))
        {
            Methods.ShowMessage(user, $"$market_you_need {currencyName} $market_to_purchase", currency.m_itemData.GetIcon());
            return false;
        };
        string itemName = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
        int userCurrency = user.GetInventory().GetItem(currency.m_itemData.m_shared.m_name, isPrefabName: false).m_stack;
        if (userCurrency < data.m_price)
        {
            Methods.ShowMessage(user, $"$you_do_not_have_enough {currencyName} $market_to_purchase", currency.m_itemData.GetIcon());
            return false;
        }

        if (!user.GetInventory().HaveEmptySlot())
        {
            Methods.ShowMessage(user, "$your_inventory_is_full");
            return false;
        }
        ItemDrop.ItemData PurchasedItem = user.GetInventory().AddItem(item.name, data.m_stack, data.m_quality, item.m_itemData.m_variant, 0L, data.m_crafter);
        if (PurchasedItem == null) return false;
        var deserializer = new DeserializerBuilder().Build();
        var customData = deserializer.Deserialize<Dictionary<string, string>>(data.m_customData);
        PurchasedItem.m_customData = customData;
        
        Methods.ShowMessage(user, $"$market_you_purchased {itemName} $market_for {data.m_price} {currencyName}", item.m_itemData.GetIcon());
        user.GetInventory().RemoveItem(currency.m_itemData.m_shared.m_name, data.m_price);

        return Market.BuyMarketItem(znv, data);
    }
    private static void DestroyMarketItems()
    {
        foreach(Transform child in ListRoot) Object.Destroy(child.gameObject);
    }
    private static void SetMarketGUI(InventoryGui instance)
    {
        if (!instance) return;
        
        MarketGUI = Object.Instantiate(LoadAssets._assets.LoadAsset<GameObject>("Market_GUI"), instance.transform);
        m_BuyItem = Object.Instantiate(LoadAssets._assets.LoadAsset<GameObject>("Market_Item"));
        m_SellItem = Object.Instantiate(LoadAssets._assets.LoadAsset<GameObject>("Market_Sell"));
        MarketStar = Object.Instantiate(LoadAssets._assets.LoadAsset<GameObject>("Market_Star"));
        
        MarketGUI.SetActive(false);
        
        ListRoot = Utils.FindChild(MarketGUI.transform, "$part_Content").GetComponent<RectTransform>();
        Transform VanillaCloseButton = instance.m_trophiesPanel.transform.Find("TrophiesFrame/Closebutton");
        Button VanillaButtonComponent = VanillaCloseButton.GetComponent<Button>();
        ButtonSfx VanillaButtonSFX = VanillaCloseButton.GetComponent<ButtonSfx>();
        
        GameObject CloseButton = Utils.FindChild(MarketGUI.transform, "$part_CloseButton").gameObject;
        CloseButton.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        if (CloseButton.TryGetComponent(out Button CloseComponent))
        {
            CloseComponent.transition = Selectable.Transition.SpriteSwap;
            CloseComponent.spriteState = VanillaButtonComponent.spriteState;
            CloseComponent.onClick.AddListener(HideGUI);
        }

        Transform SellButton = Utils.FindChild(m_SellItem.transform, "$part_sell_button");
        SellButton.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        SellButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$market_sell");
        if (SellButton.TryGetComponent(out Button SellComponent))
        {
            SellComponent.transition = Selectable.Transition.SpriteSwap;
            SellComponent.spriteState = VanillaButtonComponent.spriteState;
        }
        
        Transform BuyButton = Utils.FindChild(m_BuyItem.transform, "$part_buy_button");
        BuyButton.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        BuyButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$market_buy");
        if (BuyButton.TryGetComponent(out Button BuyComponent))
        {
            BuyComponent.transition = Selectable.Transition.SpriteSwap;
            BuyComponent.spriteState = VanillaButtonComponent.spriteState;
        }

        MerchantRevenue = Utils.FindChild(MarketGUI.transform, "$part_seller").gameObject;

        RevenueText = Utils.FindChild(MerchantRevenue.transform, "$part_revenue").GetComponent<Text>();
        RevenueValue = Utils.FindChild(MerchantRevenue.transform, "$part_money_value").GetComponent<Text>();
        MarketTitle = Utils.FindChild(MarketGUI.transform, "$part_title").GetComponent<Text>();

        GameObject RevenueButton = Utils.FindChild(MerchantRevenue.transform, "$part_money_button").gameObject;
        RevenueButton.AddComponent<ButtonSfx>().m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        if (RevenueButton.TryGetComponent(out Button RevenueComponent))
        {
            RevenueComponent.onClick.AddListener(CollectRevenue);
            RevenueComponent.transition = Selectable.Transition.SpriteSwap;
            RevenueComponent.spriteState = VanillaButtonComponent.spriteState;
        }

        RevenueText.text = Localization.instance.Localize("$revenue_label");
        MarketTitle.text = Localization.instance.Localize("$market_label");

        Transform PartFilter = Utils.FindChild(MarketGUI.transform, "$part_filter");
        if (PartFilter.TryGetComponent(out InputField FilterField))
        {
            FilterField.onValueChanged.AddListener(OnFilterChange);
        }
        // Add material to images so they are affected by time of day
        Image vanillaBackground = instance.m_trophiesPanel.transform.Find("TrophiesFrame/border (1)").GetComponent<Image>();
        MarketBackground = MarketGUI.transform.Find("Panel").GetComponent<Image>();
        MarketBackground.material = vanillaBackground.material;
        PartFilter.GetComponent<Image>().material = vanillaBackground.material;

        Font? NorseFont = GetFont("Norse");
        Font? NorseBold = GetFont("Norsebold");
        
        Text[] MarketGUITexts = MarketGUI.GetComponentsInChildren<Text>();
        Text[] BuyItemTexts = m_BuyItem.GetComponentsInChildren<Text>();
        Text[] SellItemTexts = m_SellItem.GetComponentsInChildren<Text>();
        
        AddFonts(MarketGUITexts, NorseBold, NorseFont);
        AddFonts(BuyItemTexts, NorseBold, NorseFont);
        AddFonts(SellItemTexts, NorseBold, NorseFont);
        
    }

    private static void AddFonts(Text[] array, Font? NorseBold, Font? NorseFont)
    {
        foreach (Text text in array)
        {
            text.font = text.name == "$part_title" ? NorseBold : NorseFont;
        }
    }

    private static Font? GetFont(string name)
    {
        Font[]? fonts = Resources.FindObjectsOfTypeAll<Font>();
        return fonts.FirstOrDefault(x => x.name == name);
    }
    private static void HideGUI() => MarketGUI.SetActive(false);
    private static void CollectRevenue()
    {
        if (!Market.CollectValue(CurrentMarket)) return;
        UpdateSellMarket(CurrentMarket, Player.m_localPlayer);
    }
    private static void OnFilterChange(string input)
    {
        if (MarketTypeIs is MarketType.Buy)
        {
            UpdateBuyMarket(CurrentMarket, Player.m_localPlayer, input);
        }
        else
        {
            UpdateSellMarket(CurrentMarket, Player.m_localPlayer, input);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    private static class SetMarketplaceEffects
    {
        private static void Postfix(InventoryGui __instance)
        {
            if (!__instance) return;
            SetMarketGUI(__instance);
        }
    }
}