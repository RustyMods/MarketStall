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
    public static ZNetView? CurrentMarket;
    
    private static GameObject CommunityMarketGUI = null!;
    private static GameObject m_communityBuyItem = null!;
    private static GameObject m_communitySellItem = null!;
    private static RectTransform CommunityListRoot = null!;
    private static GameObject CommunityMerchantRevenue = null!;
    private static Text CommunityRevenueText = null!;
    public static Text CommunityRevenueValue = null!;
    private static Text CommunityMarketTitle = null!;
    private static Image CommunityMarketBackground = null!;
    private static RectTransform CommunityMarketGUIRect = null!;

    public static ZNetView? CurrentCommunityMarket;
    public enum MarketType { Buy, Sell }
    public static MarketType MarketTypeIs;
    private static int price = 1;
    private static void UpdateRevenue(ZNetView znv)
    {
        float value = znv.GetZDO().GetInt(Market._MarketValue);
        RevenueValue.text = value.ToString("0");
    }

    private static void UpdateCommunityRevenue(ZNetView znv, Player player)
    {
        int value = CommunityMarket.GetPlayerRevenue(znv, player.GetPlayerID());
        CommunityRevenueValue.text = value.ToString("0");
    }
    public static void ToggleMarketGUI() => MarketGUI.SetActive(!IsMarketVisible()); 
    public static void ShowGUI(ZNetView znv, bool isCreator, bool community = false)
    {
        MarketGUI.SetActive(!community);
        CommunityMarketGUI.SetActive(community);
        znv.ClaimOwnership();
        if (!znv.IsValid()) return;
        if (community)
        {
            CurrentCommunityMarket = znv;
            CurrentMarket = null;
        }
        else
        {
            CurrentMarket = znv;
            CurrentCommunityMarket = null;
        }
        OpenMarket(znv, Player.m_localPlayer, isCreator, community);
        MerchantRevenue.SetActive(isCreator || community);
        if (isCreator || community)
        {
            if (community)
            {
                UpdateCommunityRevenue(znv, Player.m_localPlayer);
            }
            else
            {
                UpdateRevenue(znv);
            }
        }
        MarketBackground.color = MarketStallPlugin._TransparentBackground.Value is MarketStallPlugin.Toggle.On
            ? Color.clear
            : Color.white;
        CommunityMarketBackground.color = MarketStallPlugin._TransparentBackground.Value is MarketStallPlugin.Toggle.On
            ? Color.clear
            : Color.white;
        InventoryGui.instance.Show(null);
        if (!community)
        {
            string name = znv.GetZDO().GetString(Market._MarketName);
            MarketTitle.text = Localization.instance.Localize($"{name} $market_label");
        }
    }
    private static void OpenMarket(ZNetView znv, Humanoid user, bool isCreator, bool community = false)
    {
        if (znv == null || !znv.IsValid()) return;
        if (isCreator)
        {
            UpdateSellMarket(znv, user, "", community);
        }
        else
        {
            UpdateBuyMarket(znv, user, "", community);
        }
    }
    public static bool IsMarketVisible() => MarketGUI && MarketGUI.activeInHierarchy;

    public static bool IsCommunityMarketVisible() => CommunityMarketGUI && CommunityMarketGUI.activeInHierarchy;
    public static void UpdateSellMarket(ZNetView znv, Humanoid user, string filter = "", bool community = false)
    {
        DestroyMarketItems();
        DestroyCommunityMarketItems();
        MarketTypeIs = MarketType.Sell;
        List<ItemDrop.ItemData> inventory = user.GetInventory().m_inventory;
        foreach (ItemDrop.ItemData data in inventory)
        {
            if (Filters.ServerIgnoreList.Value.Contains(data.m_shared.m_name)) continue;
            string LocalizedName = Localization.instance.Localize(data.m_shared.m_name);
            if (!LocalizedName.ToLower().Contains(filter.ToLower())) continue;
            GameObject element = Object.Instantiate(community ? m_communitySellItem : m_SellItem, community ? CommunityListRoot : ListRoot);
            
            GameObject Currency = Methods.TryGetPrefab(MarketStallPlugin._Currency.Value);
            if (!Currency.TryGetComponent(out ItemDrop currencyComponent)) continue;
            Transform StarsRoot = Utils.FindChild(element.transform, "$part_stars_root");
            
            Utils.FindChild(element.transform, "$part_item_icon").GetComponent<Image>().sprite = data.GetIcon();;
            Utils.FindChild(element.transform, "$part_item_stack").GetComponent<Text>().text = data.m_stack.ToString(CultureInfo.CurrentCulture);
            Utils.FindChild(element.transform, "$part_item_name").GetComponent<Text>().text = LocalizedName;;
            Utils.FindChild(element.transform, "$part_price_icon").GetComponent<Image>().sprite = currencyComponent.m_itemData.GetIcon();
            if (community)
            {
                Utils.FindChild(element.transform, "$part_merchant_name").GetComponent<Text>().text = user.GetHoverName();
            }
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

                    int MarketCount = community 
                        ? CurrentCommunityMarket == null ? 0 : CommunityMarket.GetMarketData(CurrentCommunityMarket).Count 
                        : CurrentMarket == null ? 0 : Market.GetMarketData(CurrentMarket).Count;
                    if (MarketCount >= MarketStallPlugin._MaxSales.Value)
                    {
                        Methods.ShowMessage(user, "<color=red>$maximum_items</color>");
                        return;
                    }

                    if (!SellItem(user, component, data, data.m_stack, price, currencyComponent, znv, community)) return;
                    UpdateSellMarket(znv, user, filter, community);
                    price = 1;
                });
            }
        }
    }
    private static void SetPrice(string input)
    {
        price = int.TryParse(input, out int value) ? value : 1;
    }
    private static bool SellItem(Humanoid user, ItemDrop item, ItemDrop.ItemData data, int stack, int cost, ItemDrop currency, ZNetView znv, bool community = false)
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
        if (community)
        {
            CommunityMarket.AddMarketItem(user.GetHoverName(), item, currency, data, stack, cost, znv);
        }
        else
        {
            Market.AddMarketItem(item, currency, data, stack, cost, znv);
        }
        return true;
    }
    private static void UpdateBuyMarket(ZNetView znv, Humanoid user, string filter = "", bool community = false)
    {
        DestroyMarketItems();
        DestroyCommunityMarketItems();
        MarketTypeIs = MarketType.Buy;
        List<MarketData.MarketTradeItem> data = community 
            ? CommunityMarket.GetMarketData(znv).OrderBy(x => x.m_price).ToList()
            : Market.GetMarketData(znv).OrderBy(x => x.m_price).ToList();
        foreach (MarketData.MarketTradeItem item in data)
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(item.m_prefab);
            ItemDrop prefabItemDrop = prefab.GetComponent<ItemDrop>();
            string LocalizedName = Localization.instance.Localize(prefabItemDrop.m_itemData.m_shared.m_name);

            if (!LocalizedName.ToLower().Contains(filter.ToLower())) continue;
            
            GameObject element = Object.Instantiate(community ? m_communityBuyItem : m_BuyItem, community ? CommunityListRoot : ListRoot);
            GameObject currencyPrefab = ObjectDB.instance.GetItemPrefab(item.m_currency);
            if (!currencyPrefab.TryGetComponent(out ItemDrop currencyItemDrop)) continue;
            Transform StarsRoot = Utils.FindChild(element.transform, "$part_stars_root");
            
            Utils.FindChild(element.transform, "$part_item_icon").GetComponent<Image>().sprite = prefabItemDrop.m_itemData.GetIcon();
            Utils.FindChild(element.transform, "$part_item_name").GetComponent<Text>().text = LocalizedName;
            Utils.FindChild(element.transform, "$part_price_icon").GetComponent<Image>().sprite = currencyItemDrop.m_itemData.GetIcon();
            Utils.FindChild(element.transform, "$part_price").GetComponent<Text>().text = item.m_price == 0 ? Localization.instance.Localize("$market_free") : item.m_price.ToString(CultureInfo.CurrentCulture);
            Utils.FindChild(element.transform, "$part_buy_button").GetComponent<Button>().onClick.AddListener(() =>
            {
                if (!BuyItem(user, prefabItemDrop, item, currencyItemDrop, znv, community)) return;
                UpdateBuyMarket(znv, user, filter, community);
            });;
            Utils.FindChild(element.transform, "$part_item_stack").GetComponent<Text>().text = item.m_stack.ToString(CultureInfo.CurrentCulture);

            if (community)
            {
                Utils.FindChild(element.transform, "$part_merchant_name").GetComponent<Text>().text = item.m_merchant;
            }
            
            if (item.m_quality > 1)
            {
                for (int index = 0; index < item.m_quality; ++index) Object.Instantiate(MarketStar, StarsRoot);
            }
        }
    }
    private static bool BuyItem(Humanoid user, ItemDrop item, MarketData.MarketTradeItem data, ItemDrop currency, ZNetView znv, bool community = false)
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

        return community ? CommunityMarket.BuyMarketItem(znv, data) : Market.BuyMarketItem(znv, data);
    }
    private static void DestroyMarketItems()
    {
        foreach(Transform child in ListRoot) Object.Destroy(child.gameObject);
    }
    private static void DestroyCommunityMarketItems()
    {
        foreach(Transform child in CommunityListRoot) Object.Destroy(child.gameObject);
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
        CommunityMarketGUI = Object.Instantiate(LoadAssets._assets.LoadAsset<GameObject>("CommunityMarket_GUI"), instance.transform);
        m_communityBuyItem = Object.Instantiate(LoadAssets._assets.LoadAsset<GameObject>("Community_Market_Item"));
        m_communitySellItem = Object.Instantiate(LoadAssets._assets.LoadAsset<GameObject>("Community_Market_Sell"));
        CommunityMarketGUI.SetActive(false);
        CommunityListRoot = Utils.FindChild(CommunityMarketGUI.transform, "$part_Content").GetComponent<RectTransform>();

        CommunityMarketGUIRect = CommunityMarketGUI.GetComponent<RectTransform>();
        CommunityMarketGUIRect.anchoredPosition = MarketStallPlugin._CommunityPanelPos.Value;
        MarketStallPlugin._CommunityPanelPos.SettingChanged += (sender, args) => CommunityMarketGUIRect.anchoredPosition = MarketStallPlugin._CommunityPanelPos.Value;
        CommunityMarketGUIRect.sizeDelta = MarketStallPlugin._CommunityPanelSize.Value;
        MarketStallPlugin._CommunityPanelSize.SettingChanged += (sender, args) => CommunityMarketGUIRect.sizeDelta = MarketStallPlugin._CommunityPanelSize.Value;
        
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

        GameObject CommunityCloseButton = Utils.FindChild(CommunityMarketGUI.transform, "$part_CloseButton").gameObject;
        CommunityCloseButton.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        if (CommunityCloseButton.TryGetComponent(out Button CommunityCloseComponent))
        {
            CommunityCloseComponent.transition = Selectable.Transition.SpriteSwap;
            CommunityCloseComponent.spriteState = VanillaButtonComponent.spriteState;
            CommunityCloseComponent.onClick.AddListener(HideGUI);
        }

        Transform SellButton = Utils.FindChild(m_SellItem.transform, "$part_sell_button");
        SellButton.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        SellButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$market_sell");
        if (SellButton.TryGetComponent(out Button SellComponent))
        {
            SellComponent.transition = Selectable.Transition.SpriteSwap;
            SellComponent.spriteState = VanillaButtonComponent.spriteState;
        }

        Transform CommunitySellButton = Utils.FindChild(m_communitySellItem.transform, "$part_sell_button");
        CommunitySellButton.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        CommunitySellButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$market_sell");
        if (CommunitySellButton.TryGetComponent(out Button CommunitySellComponent))
        {
            CommunitySellComponent.transition = Selectable.Transition.SpriteSwap;
            CommunitySellComponent.spriteState = VanillaButtonComponent.spriteState;
        }
        
        Transform BuyButton = Utils.FindChild(m_BuyItem.transform, "$part_buy_button");
        BuyButton.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        BuyButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$market_buy");
        if (BuyButton.TryGetComponent(out Button BuyComponent))
        {
            BuyComponent.transition = Selectable.Transition.SpriteSwap;
            BuyComponent.spriteState = VanillaButtonComponent.spriteState;
        }

        Transform CommunityBuyButton = Utils.FindChild(m_communityBuyItem.transform, "$part_buy_button");
        CommunityBuyButton.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        CommunityBuyButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$market_buy");
        if (CommunityBuyButton.TryGetComponent(out Button CommunityBuyComponent))
        {
            CommunityBuyComponent.transition = Selectable.Transition.SpriteSwap;
            CommunityBuyComponent.spriteState = VanillaButtonComponent.spriteState;
        }

        MerchantRevenue = Utils.FindChild(MarketGUI.transform, "$part_seller").gameObject;
        RevenueText = Utils.FindChild(MerchantRevenue.transform, "$part_revenue").GetComponent<Text>();
        RevenueValue = Utils.FindChild(MerchantRevenue.transform, "$part_money_value").GetComponent<Text>();
        MarketTitle = Utils.FindChild(MarketGUI.transform, "$part_title").GetComponent<Text>();

        CommunityMerchantRevenue = Utils.FindChild(CommunityMarketGUI.transform, "$part_seller").gameObject;
        CommunityRevenueText = Utils.FindChild(CommunityMerchantRevenue.transform, "$part_revenue").GetComponent<Text>();
        CommunityRevenueValue = Utils.FindChild(CommunityMerchantRevenue.transform, "$part_money_value").GetComponent<Text>();
        CommunityMarketTitle = Utils.FindChild(CommunityMarketGUI.transform, "$part_title").GetComponent<Text>();

        GameObject RevenueButton = Utils.FindChild(MerchantRevenue.transform, "$part_money_button").gameObject;
        RevenueButton.AddComponent<ButtonSfx>().m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        if (RevenueButton.TryGetComponent(out Button RevenueComponent))
        {
            RevenueComponent.onClick.AddListener(CollectRevenue);
            RevenueComponent.transition = Selectable.Transition.SpriteSwap;
            RevenueComponent.spriteState = VanillaButtonComponent.spriteState;
        }

        GameObject CommunityRevenueButton = Utils.FindChild(CommunityMerchantRevenue.transform, "$part_money_button").gameObject;
        CommunityRevenueButton.AddComponent<ButtonSfx>().m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        if (CommunityRevenueButton.TryGetComponent(out Button CommunityRevenueComponent))
        {
            CommunityRevenueComponent.onClick.AddListener(CollectCommunityRevenue);
            CommunityRevenueComponent.transition = Selectable.Transition.SpriteSwap;
            CommunityRevenueComponent.spriteState = VanillaButtonComponent.spriteState;
        }

        RevenueText.text = Localization.instance.Localize("$revenue_label");
        MarketTitle.text = Localization.instance.Localize("$market_label");
        
        CommunityRevenueText.text = Localization.instance.Localize("$revenue_label  ");
        CommunityMarketTitle.text = Localization.instance.Localize("$market_label");
        
        Transform PartFilter = Utils.FindChild(MarketGUI.transform, "$part_filter");
        if (PartFilter.TryGetComponent(out InputField FilterField))
        {
            FilterField.onValueChanged.AddListener(OnFilterChange);
        }

        Transform CommunityPartFilter = Utils.FindChild(CommunityMarketGUI.transform, "$part_filter");
        if (CommunityPartFilter.TryGetComponent(out InputField CommunityFilterField))
        {
            CommunityFilterField.onValueChanged.AddListener(OnFilterChange);
        }
        
        // Add material to images so they are affected by time of day
        Image vanillaBackground = instance.m_trophiesPanel.transform.Find("TrophiesFrame/border (1)").GetComponent<Image>();
        MarketBackground = MarketGUI.transform.Find("Panel").GetComponent<Image>();
        MarketBackground.material = vanillaBackground.material;
        PartFilter.GetComponent<Image>().material = vanillaBackground.material;

        CommunityMarketBackground = CommunityMarketGUI.transform.Find("Panel").GetComponent<Image>();
        CommunityMarketBackground.material = vanillaBackground.material;
        CommunityPartFilter.GetComponent<Image>().material = vanillaBackground.material;
        
        Font? NorseFont = GetFont("Norse");
        Font? NorseBold = GetFont("Norsebold");
        
        Text[] MarketGUITexts = MarketGUI.GetComponentsInChildren<Text>();
        Text[] BuyItemTexts = m_BuyItem.GetComponentsInChildren<Text>();
        Text[] SellItemTexts = m_SellItem.GetComponentsInChildren<Text>();

        Text[] CommunityMarketGUITexts = CommunityMarketGUI.GetComponentsInChildren<Text>();
        Text[] CommunityBuyItemTexts = m_communityBuyItem.GetComponentsInChildren<Text>();
        Text[] CommunitySellItemTexts = m_communitySellItem.GetComponentsInChildren<Text>();
        
        AddFonts(MarketGUITexts, NorseBold, NorseFont);
        AddFonts(BuyItemTexts, NorseBold, NorseFont);
        AddFonts(SellItemTexts, NorseBold, NorseFont);
        
        AddFonts(CommunityMarketGUITexts, NorseBold, NorseFont);
        AddFonts(CommunityBuyItemTexts, NorseBold, NorseFont);
        AddFonts(CommunitySellItemTexts, NorseBold, NorseFont);
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
    public static void HideGUI()
    {
        MarketGUI.SetActive(false);
        CommunityMarketGUI.SetActive(false);
        if (Market.m_currentMarket != null)
        {
            Market.m_currentMarket._znv.InvokeRPC(nameof(Market.RPC_SetInUse), false);
            Market.m_currentMarket = null;
        }

        if (CommunityMarket.m_currentCommunityMarket != null)
        {
            CommunityMarket.m_currentCommunityMarket.m_nview.InvokeRPC(nameof(Market.RPC_SetInUse), false);
            CommunityMarket.m_currentCommunityMarket = null;
        }
    }
    private static void CollectRevenue()
    {
        if (CurrentMarket == null) return;
        if (!Market.CollectValue(CurrentMarket)) return;
        UpdateSellMarket(CurrentMarket, Player.m_localPlayer);
    }

    private static void CollectCommunityRevenue()
    {
        if (CurrentCommunityMarket == null) return;
        if (!CommunityMarket.CollectValue(CurrentCommunityMarket, Player.m_localPlayer)) return;
        UpdateSellMarket(CurrentCommunityMarket, Player.m_localPlayer, "", true);
    }
    private static void OnFilterChange(string input)
    {
        if (CurrentMarket != null)
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

        if (CurrentCommunityMarket != null)
        {
            if (MarketTypeIs is MarketType.Buy)
            {
                UpdateBuyMarket(CurrentCommunityMarket, Player.m_localPlayer, input, true);
            }
            else
            {
                UpdateSellMarket(CurrentCommunityMarket, Player.m_localPlayer, input, true);
            }
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