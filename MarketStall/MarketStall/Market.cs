using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using MarketStall.UI;
using MarketStall.Utility;
using UnityEngine;
using YamlDotNet.Serialization;

namespace MarketStall.MarketStall;

public class Market : MonoBehaviour, Hoverable, Interactable
{
    public static readonly int _MarketName = "marketname".GetStableHashCode();
    private static readonly int _MarketData = "marketdata".GetStableHashCode();
    public static readonly int _MarketValue = "marketvalue".GetStableHashCode();

    public bool m_inUse;
    
    private static readonly Vector3 SpawnDistance = new(0f, 0f, 1f);
    public static Market? m_currentMarket;

    public ZNetView _znv = null!;
    public static List<MarketData.MarketTradeItem> GetMarketData(ZNetView znv)
    {
        string data = znv.GetZDO().GetString(_MarketData);
        if (data.IsNullOrWhiteSpace()) return new();
        try
        {
            IDeserializer deserializer = new DeserializerBuilder().Build();
            return deserializer.Deserialize<List<MarketData.MarketTradeItem>>(data);
        }
        catch
        {
            return new();
        }
    }
    private void RPC_UpdateMarket(long sender, string data) => _znv.GetZDO().Set(_MarketData, data);
    private void RPC_UpdateRevenue(long sender, int value) => _znv.GetZDO().Set(_MarketValue, value);
    
    private string Owner
    {
        get => _znv.m_zdo.GetString(_MarketName);
        set => _znv.m_zdo.Set(_MarketName, value);
    }
    private void Awake()
    {
        _znv = GetComponent<ZNetView>();
        if (!_znv.IsValid()) return;
        if (_znv.IsOwner() && Owner.IsNullOrWhiteSpace())
        {
            Owner = Player.m_localPlayer.GetPlayerName();
        }
        _znv.Register<string>(nameof(RPC_UpdateMarket),RPC_UpdateMarket);
        _znv.Register<int>(nameof(RPC_UpdateRevenue),RPC_UpdateRevenue);
        _znv.Register<bool>(nameof(RPC_SetInUse),RPC_SetInUse);
    }

    public static void AddMarketItem(ItemDrop item, ItemDrop currency, ItemDrop.ItemData ItemData, int stack, int price, ZNetView znv)
    {
        if (!znv.IsOwner()) return;
        List<MarketData.MarketTradeItem> data = GetMarketData(znv);
        ISerializer serializer = new SerializerBuilder().Build();
        var customData = serializer.Serialize(ItemData.m_customData);
        data.Add(new ()
        {
            m_prefab = item.name,
            m_price = price,
            m_stack = stack,
            m_quality = ItemData.m_quality,
            m_crafterID = ItemData.m_crafterID,
            m_crafter = ItemData.m_crafterName,
            m_currency = currency.name,
            m_customData = customData
        }); 
        string MarketData = serializer.Serialize(data);
        znv.InvokeRPC(nameof(RPC_UpdateMarket), MarketData);
    }
    public static bool BuyMarketItem(ZNetView znv, MarketData.MarketTradeItem MarketItem)
    {
        List<MarketData.MarketTradeItem> data = GetMarketData(znv);
        MarketData.MarketTradeItem match = data.Find(x => x.m_prefab == MarketItem.m_prefab && x.m_quality == MarketItem.m_quality && x.m_stack == MarketItem.m_stack);
        if (match == null) return false;
        if (!data.Remove(match)) return false;
        ISerializer serializer = new SerializerBuilder().Build();
        string MarketData = serializer.Serialize(data);
        znv.InvokeRPC(nameof(RPC_UpdateMarket), MarketData);
        AddRevenue(znv, MarketItem.m_price);
        return true;
    }
    public static bool CollectValue(ZNetView znv) 
    {
        int value = znv.GetZDO().GetInt(_MarketValue);
        if (value <= 0) return false;
        GameObject item = Methods.TryGetPrefab(MarketStallPlugin._Currency.Value);
        if (item.TryGetComponent(out ItemDrop component))
        {
            int MaxStackSize = component.m_itemData.m_shared.m_maxStackSize;
            int ExtractableValue = Mathf.Min(value, MaxStackSize);
            if (!Player.m_localPlayer.GetInventory().AddItem(item, ExtractableValue))
            {
                GameObject spawn = Instantiate(item, Player.m_localPlayer.transform.position + SpawnDistance, Quaternion.identity);
                if (spawn.TryGetComponent(out ItemDrop itemDrop)) itemDrop.m_itemData.m_stack = MaxStackSize;
            }

            Marketplace.RevenueValue.text = (value - ExtractableValue).ToString(CultureInfo.CurrentCulture);
            znv.InvokeRPC(nameof(RPC_UpdateRevenue), value - ExtractableValue);
            return true;
        }

        return false;
    }
    private static void AddRevenue(ZNetView znv, int amount)
    {
        int CurrentValue = znv.GetZDO().GetInt(_MarketValue);
        znv.InvokeRPC(nameof(RPC_UpdateRevenue), CurrentValue + amount);
    }

    public string GetHoverText()
    {
        if (GetComponent<Piece>().IsCreator())
        {
            return Localization.instance.Localize("<b>$market_label</b>")
                   + "\n"
                   + Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $market_sell")
                   + "\n"
                   + Localization.instance.Localize("[<color=yellow><b>L.Shift + $KEY_Use</b></color>] $market_buy");
        }

        return Localization.instance.Localize("$market_owner") + ": " + Owner
               + "\n"
               + Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $market_buy");
    }

    public string GetHoverName()
    {
        return Localization.instance.Localize($"{Owner} $market_label");
    }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold) return false;
        if (IsInUse())
        {
            user.Message(MessageHud.MessageType.Center, "$msg_inuse");
            return false;
        }
        Marketplace.ShowGUI(_znv, !alt && GetComponent<Piece>().IsCreator());
        _znv.InvokeRPC(nameof(RPC_SetInUse), true);
        m_currentMarket = this;
        return true;
    }

    public void SetInUse(bool use) => _znv.InvokeRPC(nameof(RPC_SetInUse), use);

    public void RPC_SetInUse(long sender, bool use)
    {
        m_inUse = use;
    }

    private bool IsInUse() => m_inUse;

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}