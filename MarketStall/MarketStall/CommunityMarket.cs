using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BepInEx;
using MarketStall.UI;
using MarketStall.Utility;
using UnityEngine;
using YamlDotNet.Serialization;

namespace MarketStall.MarketStall;

public class CommunityMarket : MonoBehaviour, Hoverable, Interactable
{
    private static readonly int _MarketData = "communitymarketdata".GetStableHashCode();
    public static readonly int _MarketValue = "communitymarketvalue".GetStableHashCode();
    
    public ZNetView m_nview = null!;
    public string m_name = "Community Market Stall";
    public bool m_inUse;
    private static readonly Vector3 SpawnDistance = new(0f, 0f, 1f);
    public static CommunityMarket? m_currentCommunityMarket;
    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        if (!m_nview.IsValid()) return;
        m_nview.Register<string>(nameof(RPC_UpdateMarket),RPC_UpdateMarket);
        m_nview.Register<long, string, int>(nameof(RPC_UpdateRevenue),RPC_UpdateRevenue);
        m_nview.Register<bool>(nameof(RPC_SetInUse), RPC_SetInUse);
    }

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

    public void RPC_SetInUse(long sender, bool use) => m_inUse = use;
    private void RPC_UpdateMarket(long sender, string data) => m_nview.GetZDO().Set(_MarketData, data);

    public static int GetPlayerRevenue(ZNetView znv, long playerID)
    {
        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var data = deserializer.Deserialize<Dictionary<long, CommunityData>>(znv.GetZDO()
                .GetString(_MarketValue));
            if (data.TryGetValue(playerID, out CommunityData playerData))
            {
                return playerData.Revenue;
            }
        }
        catch
        {
            return 0;
        }

        return 0;
    }
    private void RPC_UpdateRevenue(long sender, long playerID, string playerName, int value)
    {
        string info = m_nview.GetZDO().GetString(_MarketValue);
        var data = new Dictionary<long, CommunityData>();
        if (!info.IsNullOrWhiteSpace())
        {
            try
            {
                var deserializer = new DeserializerBuilder().Build();
                data = deserializer.Deserialize<Dictionary<long, CommunityData>>(info);
            }
            catch
            {
                //
            }
        }
        var serializer = new SerializerBuilder().Build();
        if (data.TryGetValue(playerID, out CommunityData playerData))
        {
            playerData.Revenue = value;
        }
        else
        {
            data[playerID] = new CommunityData()
            {
                PlayerID = playerID,
                PlayerName = playerName,
                Revenue = value
            };
        }
        m_nview.GetZDO().Set(_MarketValue, serializer.Serialize(data));
    }

    private bool IsInUse() => m_inUse;
    
    public static void AddMarketItem(string merchant, ItemDrop item, ItemDrop currency, ItemDrop.ItemData ItemData, int stack, int price, ZNetView znv)
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
            m_crafter = ItemData.m_crafterName,
            m_currency = currency.name,
            m_customData = customData,
            m_merchant = merchant
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
    
    public static bool CollectValue(ZNetView znv, Player player)
    {
        long playerID = player.GetPlayerID();
        string playerName = player.GetPlayerName();

        string data = znv.GetZDO().GetString(_MarketValue);
        if (data.IsNullOrWhiteSpace()) return false;
        var deserializer = new DeserializerBuilder().Build();
        try
        {
            var sales = deserializer.Deserialize<Dictionary<long, CommunityData>>(data);
            if (sales.TryGetValue(playerID, out CommunityData playerData))
            {
                int value = playerData.Revenue;
                if (value <= 0) return false;
                GameObject item = Methods.TryGetPrefab(MarketStallPlugin._Currency.Value);
                if (item.TryGetComponent(out ItemDrop component))
                {
                    int MaxStackSize = component.m_itemData.m_shared.m_maxStackSize;
                    int ExtractableValue = Mathf.Min(value, MaxStackSize);
                    if (!Player.m_localPlayer.GetInventory().AddItem(item, ExtractableValue))
                    {
                        GameObject spawn = Instantiate(item, Player.m_localPlayer.transform.position + SpawnDistance,
                            Quaternion.identity);
                        if (spawn.TryGetComponent(out ItemDrop itemDrop)) itemDrop.m_itemData.m_stack = MaxStackSize;
                    }

                    Marketplace.CommunityRevenueValue.text = (value - ExtractableValue).ToString(CultureInfo.CurrentCulture);
                    znv.InvokeRPC(nameof(RPC_UpdateRevenue), playerID, playerName, value - ExtractableValue);
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }
        return false;
    }
    private static void AddRevenue(ZNetView znv, int amount)
    {
        int CurrentValue = GetPlayerRevenue(znv, Player.m_localPlayer.GetPlayerID());
        znv.InvokeRPC(nameof(RPC_UpdateRevenue), Player.m_localPlayer.GetPlayerID(), Player.m_localPlayer.GetPlayerName(), CurrentValue + amount);
    }
    
    public string GetHoverText()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("<b>$market_label</b>\n");
        stringBuilder.Append("[<color=yellow><b>$KEY_Use</b></color> $market_sell\n");
        stringBuilder.Append("[<color=yellow><b>L.Shift + $KEY_Use</b></color>] $market_buy");
        return Localization.instance.Localize(stringBuilder.ToString());
    }

    public string GetHoverName() => m_name;

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold) return false;
        if (IsInUse())
        {
            user.Message(MessageHud.MessageType.Center, "$msg_inuse");
            return false;
        }
        Marketplace.ShowGUI(m_nview, !alt, true);
        m_nview.InvokeRPC(nameof(RPC_SetInUse), true);
        m_currentCommunityMarket = this;
        return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    [Serializable]
    public class CommunityData
    {
        public long PlayerID = 0L;
        public string PlayerName = "";
        public int Revenue = 0;
    }
}