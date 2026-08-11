using System;
using System.Collections.Generic;
using System.Linq;
using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarketStall;

public class MarketGUI : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public static MarketGUI instance;
    private static Sprite panelSprite;

    public Image panelImage;
    private TextMeshProUGUI header;
    
    private ItemView view;
    private ButtonElement switchButton;
    private ButtonElement revenueButton;
    private ButtonElement renameButton;
    private GuiInputField inputField;
    public Stall currentMarket;
    public bool isSelling;
    public bool isCollecting;
    public Vector3 mouseOffset;

    public void Awake()
    {
        instance = this;
        
        panelImage = transform.Find("Panel").GetComponent<Image>();
        header = transform.Find("Panel/Header").GetComponent<TextMeshProUGUI>();
        view = new ItemView(transform.Find("Panel/Items").gameObject);
        switchButton = new ButtonElement(transform.Find("Panel/Buttons/ItemList/BuyButton").gameObject);
        revenueButton = new ButtonElement(transform.Find("Panel/Buttons/ItemList/CollectButton").gameObject);
        renameButton = new ButtonElement(transform.Find("Panel/Buttons/ItemList/RenameButton").gameObject);
        inputField = transform.Find("Panel/Buttons/ItemList/TextField").GetComponent<GuiInputField>();
        
        header.alignment = TextAlignmentOptions.Center;
        header.color = new Color(1f, 0.75f, 0f, 1f);
        
        LoadAssets();
        
        switchButton.OnClick(OnSwitch);
        switchButton.SetLabel("Buy/Sell");
        renameButton.OnClick(OnRename);
        renameButton.SetLabel("$hud_rename");
        revenueButton.OnClick(OnCollect);
        revenueButton.SetLabel("$revenue_label");
        inputField.onValueChanged.AddListener(OnSearch);
        gameObject.SetActive(false);

        transform.position = MarketStallPlugin.panelPos.Value;
    }
    
    public bool IsSearching() => inputField.isFocused;

    public void OnSearch(string input)
    {
        view.OnSearch(input);
    }
    public void OnRename()
    {
        if (currentMarket == null) return;
        TextInput.instance.RequestText(currentMarket, "$hud_rename", 100);
    }

    public void OnCollect()
    {
        Show(currentMarket, isSelling, true);
    }

    private void SetupRevenue(Revenue revenue)
    {
        if (revenue.Count > 0)
        {
            revenueButton.SetInteractable(true);
            revenueButton.SetTooltip(
                $"{Player.m_localPlayer.GetPlayerName()} $revenue_label", 
                revenue.GetTooltip());
        }
        else
        {
            revenueButton.SetInteractable(false);
        }
    }

    public void SetHeader(string text) => header.text = text;

    public void Show(Stall market, bool sell, bool revenue = false)
    {
        if (!Player.m_localPlayer || Player.m_localPlayer.IsDead() || Player.m_localPlayer.InCutscene() ||
            Player.m_localPlayer.IsTeleporting()) return;
        
        inputField.SetTextWithoutNotify("");
        currentMarket = market;
        gameObject.SetActive(true);
        SetHeader(market.GetHoverName());

        view.Clear();
        view.SetSelected(null);
        
        if (market.IsCreator()) renameButton.Show();
        else renameButton.Hide();

        if (MarketStallPlugin.creatorLocked.Value is Toggle.On && !market.IsCreator())
        {
            sell = false;
            switchButton.Hide();
        }
        else
        {
            switchButton.Show();
        }
        
        InventoryGui.instance.Show(null);

        if (revenue)
        {
            ShowRevenue(market);
        }
        else if (sell)
        {
            ShowSell(market);
        }
        else
        {
            ShowBuy(market);
        }
        Player.m_localPlayer.GetInventory().m_onChanged -= OnPlayerInventoryChanged;
        Player.m_localPlayer.GetInventory().m_onChanged += OnPlayerInventoryChanged;
    }

    public void OnPlayerInventoryChanged() => Show(currentMarket, isSelling, isCollecting);

    private void ShowSell(Stall market)
    {
        var playerItems = Player.m_localPlayer.GetInventory().GetAllItemsInGridOrder();
        var filter = new StringListConfig(MarketStallPlugin.filters.Value).list;
        for (int i = 0; i < playerItems.Count; ++i)
        {
            var itemData = playerItems[i];
            if (filter.Contains(itemData.m_dropPrefab.name)) continue;
            var coins = ObjectDB.instance.GetItemPrefab("Coins");
            var coinsData = coins.GetComponent<ItemDrop>().m_itemData.Clone();
            coinsData.m_dropPrefab = coins;
                
            var stallItem = new MarketInventory.MarketItem
            {
                item = itemData,
                merchant = Player.m_localPlayer.GetPlayerName(),
                currency = coinsData
            };
            view.Create(stallItem, true);
        }

        List<MarketInventory.MarketItem> marketInventory = market.m_inventory.m_inventory.Values.ToList();
        for (int i = 0; i < marketInventory.Count; ++i)
        {
            MarketInventory.MarketItem marketItem = marketInventory[i];
            if (marketItem.merchant == Player.m_localPlayer.GetPlayerName())
            {
                view.Create(marketItem, true);
            }
        }
        
        revenueButton.Show();
        Revenue revenue = market.GetRevenue(Player.m_localPlayer.GetPlayerName());
        SetupRevenue(revenue);
            
        isSelling = true;
        isCollecting = false;
        switchButton.SetLabel("$market_buy");
        revenueButton.SetInteractable(true);

    }

    private void ShowBuy(Stall market)
    {
        var marketInventory = market.m_inventory.m_inventory.Values.ToList();
        for (int i = 0; i < marketInventory.Count; ++i)
        {
            var stallItem = marketInventory[i];
            view.Create(stallItem, false);
        }
            
        switchButton.SetLabel("$market_sell");
        isSelling = false;
        isCollecting = false;
        revenueButton.SetInteractable(true);

    }

    private void ShowRevenue(Stall market)
    {
        var revenue = market.GetRevenue(Player.m_localPlayer.GetPlayerName());
        for (int i = 0; i < revenue.Count; ++i)
        {
            var data = revenue[i];
            view.Create(data);
        }

        isCollecting = true;
        revenueButton.SetInteractable(false);
    }
    
    public void OnSwitch() => Show(currentMarket, !isSelling);

    public void Update()
    {
        if (!Player.m_localPlayer || Player.m_localPlayer.IsDead() || Player.m_localPlayer.InCutscene() ||
            Player.m_localPlayer.IsTeleporting() || ZInput.GetKey(KeyCode.Escape))
        {
            Hide();
        }
    }
    
    public static bool IsVisible() => instance?.gameObject.activeSelf ?? false;

    public void Hide()
    {
        if (IsSearching() || TextInput.instance.m_inputField.isFocused) return;
        
        gameObject.SetActive(false);
        if (currentMarket != null)
        {
            currentMarket.SetInUse(false);
            currentMarket = null;
        }
        if (Player.m_localPlayer)
        {
            Player.m_localPlayer.GetInventory().m_onChanged -= OnPlayerInventoryChanged;
        }
    }
    
    private void LoadAssets()
    {
        var inventoryButton = InventoryGui.instance.m_craftButton;
        var buttonBkg = inventoryButton.GetComponent<Image>().sprite;
        switchButton.SetupBackground(inventoryButton, buttonBkg);
        revenueButton.SetupBackground(inventoryButton, buttonBkg);
        renameButton.SetupBackground(inventoryButton, buttonBkg);
        
        view.SetupBackground(inventoryButton, buttonBkg);
        var resources = Resources.FindObjectsOfTypeAll<Sprite>();
        var panelBkg = resources.FirstOrDefault(r => r.name == "crafting_panel_bkg");
        panelImage.sprite = panelBkg;
        panelSprite = panelImage.sprite;
        panelImage.material = InventoryGui.instance.m_trophiesPanel.transform.Find("TrophiesFrame/border (1)").GetComponent<Image>().material;

        UpdatePanelBackground();
    }

    public void UpdatePanelBackground()
    {
        if (MarketStallPlugin.transparentPanel.Value is Toggle.On)
        {
            panelImage.sprite = null;
            panelImage.color = new Color(0f, 0f, 0f, 0.5f);
        }
        else
        {
            panelImage.sprite = panelSprite;
            panelImage.color = Color.white;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Vector2 pos = eventData.position;
        mouseOffset = transform.position - new Vector3(pos.x, pos.y, 0);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!Input.GetKey(KeyCode.LeftAlt)) return;
        transform.position = Input.mousePosition + mouseOffset;
    }
}