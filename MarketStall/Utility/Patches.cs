using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using MarketStall.MarketStall;
using MarketStall.UI;
using UnityEngine;

namespace MarketStall.Utility;

public static class Patches
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
    private static class MarketStallDropResources
    {
        private static void Postfix(Piece __instance)
        {
            if (!__instance) return;
            if (__instance.name.Replace("(Clone)", "") != "MarketStall") return;

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
        if (Cargo == null) return;
        for (int index = i * 4; index < PieceMarketData.Count; index++)
        {
            MarketData.MarketTradeItem MarketItem = PieceMarketData[index];
            GameObject prefab = ObjectDB.instance.GetItemPrefab(MarketItem.m_prefab);
            if (!prefab) continue;
            if (Cargo.TryGetComponent(out Container component))
            {
                if (component.GetInventory().AddItem(prefab, MarketItem.m_stack)) continue;
                GameObject spawned = Object.Instantiate(prefab, __instance.transform.position, Quaternion.identity);
                if (!spawned.TryGetComponent(out ItemDrop itemDrop)) continue;
                itemDrop.m_itemData.m_stack = MarketItem.m_stack;
                itemDrop.m_itemData.m_quality = MarketItem.m_quality;
                itemDrop.m_itemData.m_crafterName = MarketItem.m_crafter;
            }
            else
            {
                GameObject spawned = Object.Instantiate(prefab, __instance.transform.position, Quaternion.identity);
                if (!spawned.TryGetComponent(out ItemDrop itemDrop)) continue;
                itemDrop.m_itemData.m_stack = MarketItem.m_stack;
                itemDrop.m_itemData.m_quality = MarketItem.m_quality;
                itemDrop.m_itemData.m_crafterName = MarketItem.m_crafter;
            }
        }
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.OnInventoryChanged))]
    private static class OnInventoryChangePatch
    {
        private static void Postfix(Player __instance)
        {
            if (!Marketplace.IsMarketVisible()) return;
            if (Marketplace.MarketTypeIs is Marketplace.MarketType.Buy) return;
            Marketplace.UpdateSellMarket(Marketplace.CurrentMarket, __instance);
        }
    }

    public static void UpdateMarketGUI()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && Marketplace.IsMarketVisible())
        {
            Marketplace.ToggleMarketGUI();
        } 
    }
    
    [HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
    private static class TextInputIsVisiblePatch
    {
        private static void Postfix(TextInput __instance, ref bool __result)
        {
            if (!__instance) return;
            __result |= Marketplace.IsMarketVisible();
        }
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.IsVisible))]
    private static class StoreGUIIsVisiblePatch
    {
        private static void Postfix(ref bool __result)
        {
            __result |= Marketplace.IsMarketVisible();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    private static class InventoryGUIHidePatch
    {
        private static bool Prefix()
        {
            return !Marketplace.IsMarketVisible();
        }
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FixedUpdate))]
    private static class PlayerControllerPatch
    {
        private static bool Prefix() => !Marketplace.IsMarketVisible();
    }
    
    [HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
    private static class MarketStallRemoved
    {
        [UsedImplicitly]
        private static void Postfix(Piece __instance, ref bool __result)
        {
            string PrefabName = Utils.GetPrefabName(__instance.gameObject);
            if (PrefabName == "MarketStall" && !__instance.IsCreator())
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    private static class ZoneSystemStartPatch
    {
        private static void Postfix() => Filters.InitServerIgnoreList();
    }
}