using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using MarketStall.Managers;
using MarketStall.MarketStall;
using MarketStall.UI;
using UnityEngine;
using YamlDotNet.Serialization;

namespace MarketStall.Utility;

public static class Patches
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
    private static class MarketStallDropResources
    {
        private static void Postfix(Piece __instance)
        {
            if (!__instance) return;
            if (!MarketStallPieces.isMarketStall(__instance.name.Replace("(Clone)", string.Empty))) return;

            List<MarketData.MarketTradeItem> PieceMarketData = Market.GetMarketData(__instance.m_nview);
            int revenueTotal = __instance.m_nview.GetZDO().GetInt(Market._MarketValue);
            GameObject currency = Methods.TryGetPrefab(MarketStallPlugin._Currency.Value);
            int currencyMax = currency.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize;
            int total = revenueTotal;
            while (total > 0)
            {
                if (total > currencyMax)
                {
                    PieceMarketData.Add(new MarketData.MarketTradeItem()
                    {
                        m_prefab = currency.name,
                        m_price = 0,
                        m_currency = "",
                        m_stack = currencyMax,
                        m_quality = 1,
                        m_crafter = ""
                    });
                    total -= currencyMax;
                }
                else
                {
                    PieceMarketData.Add(new MarketData.MarketTradeItem()
                    {
                        m_prefab = currency.name,
                        m_price = 0,
                        m_currency = "",
                        m_stack = total,
                        m_quality = 1,
                        m_crafter = ""
                    });
                    total = 0;
                }
            }
            if (PieceMarketData.Count <= 0) return;
            if (CacheAssets.CargoCrate == null) return;
            if (PieceMarketData.Count > 4)
            {
                int CargoCount = Mathf.CeilToInt(PieceMarketData.Count / 4f);
                for (int i = 0; i < CargoCount; ++i)
                {
                    SpawnCargoCrate(PieceMarketData, __instance, i);
                }
            }
            else
            {
                SpawnCargoCrate(PieceMarketData, __instance);
            }
        }
    }

    private static void SpawnCargoCrate(List<MarketData.MarketTradeItem> PieceMarketData, Piece __instance, int i = 0)
    {
        GameObject? Cargo = Object.Instantiate(CacheAssets.CargoCrate, __instance.transform.position, Quaternion.identity);
        IDeserializer deserializer = new DeserializerBuilder().Build();
        if (Cargo == null) return;
        for (int index = i * 4; index < PieceMarketData.Count; index++)
        {
            MarketData.MarketTradeItem MarketItem = PieceMarketData[index];
            GameObject prefab = ObjectDB.instance.GetItemPrefab(MarketItem.m_prefab);
            if (!prefab) continue;
            if (!prefab.TryGetComponent(out ItemDrop itemDrop)) continue;
            var data = itemDrop.m_itemData.Clone();
            data.m_stack = MarketItem.m_stack;
            data.m_quality = MarketItem.m_quality;
            data.m_crafterName = MarketItem.m_crafter;
            if (!MarketItem.m_customData.IsNullOrWhiteSpace())
            {
                data.m_customData = deserializer.Deserialize<Dictionary<string, string>>(MarketItem.m_customData);
            }
            itemDrop.m_itemData = data;
            data.m_dropPrefab = prefab;
            
            if (Cargo.TryGetComponent(out Container component))
            {
                var inventory = component.GetInventory();
                if (inventory == null)
                {
                    SpawnItem(prefab, __instance.transform.position, data);
                    continue;
                }
                if (inventory.AddItem(data)) continue;
                SpawnItem(prefab, __instance.transform.position, data);
            }
            else
            {
                SpawnItem(prefab, __instance.transform.position, data);
            }
        }
    }

    private static void SpawnItem(GameObject prefab, Vector3 pos, ItemDrop.ItemData data)
    {
        GameObject spawned = Object.Instantiate(prefab, pos, Quaternion.identity);
        if (!spawned.TryGetComponent(out ItemDrop component)) return;
        component.m_itemData = data;

    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.OnInventoryChanged))]
    private static class OnInventoryChangePatch
    {
        private static void Postfix(Player __instance)
        {
            if (Marketplace.IsMarketVisible())
            {
                if (Marketplace.MarketTypeIs is Marketplace.MarketType.Buy) return;
                Marketplace.UpdateSellMarket(Marketplace.CurrentMarket, __instance);
            }

            if (Marketplace.IsCommunityMarketVisible())
            {
                if (Marketplace.MarketTypeIs is Marketplace.MarketType.Buy) return;
                Marketplace.UpdateSellMarket(Marketplace.CurrentCommunityMarket, __instance, "", true);
            }
        }
    }

    public static void UpdateMarketGUI()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && (Marketplace.IsMarketVisible() || Marketplace.IsCommunityMarketVisible()))
        {
            // Marketplace.ToggleMarketGUI();
            Marketplace.HideGUI();
        } 
    }
    
    [HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
    private static class TextInputIsVisiblePatch
    {
        private static void Postfix(TextInput __instance, ref bool __result)
        {
            if (!__instance) return;
            __result |= Marketplace.IsMarketVisible() || Marketplace.IsCommunityMarketVisible();
        }
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.IsVisible))]
    private static class StoreGUIIsVisiblePatch
    {
        private static void Postfix(ref bool __result)
        {
            __result |= Marketplace.IsMarketVisible() || Marketplace.IsCommunityMarketVisible();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    private static class InventoryGUIHidePatch
    {
        private static bool Prefix() => !Marketplace.IsMarketVisible() || !Marketplace.IsCommunityMarketVisible();
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FixedUpdate))]
    private static class PlayerControllerPatch
    {
        private static bool Prefix() => !Marketplace.IsMarketVisible() || !Marketplace.IsCommunityMarketVisible();
    }
    
    [HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
    private static class MarketStallRemoved
    {
        [UsedImplicitly]
        private static void Postfix(Piece __instance, ref bool __result)
        {
            string PrefabName = Utils.GetPrefabName(__instance.gameObject);
            if (MarketStallPieces.isMarketStall(PrefabName))
            {
                if (!__instance.IsCreator())
                {
                    __result = false;
                    
                }
            }

            if (MarketStallPieces.isCommunityMarketStall(PrefabName))
            {
                if (!Terminal.m_cheat)
                {
                    __result = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    private static class ZoneSystemStartPatch
    {
        private static void Postfix() => Filters.InitServerIgnoreList();
    }
}