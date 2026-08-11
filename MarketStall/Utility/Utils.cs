using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace MarketStall;

public static class Utils
{
    public static Dictionary<string, GameObject> itemsBySharedName = new();
    public static Dictionary<string, GameObject> itemByLocalizedName = new();

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    private static class ObjectDB_Awake_Patch
    {
        private static void Postfix(ObjectDB __instance)
        {
            for (int i = 0; i < __instance.m_items.Count; ++i)
            {
                var item = __instance.m_items[i];
                if (!item.TryGetComponent(out ItemDrop component)) continue;
                if (component.m_itemData.m_shared.m_icons.Length <= 0) continue;
                itemsBySharedName[component.m_itemData.m_shared.m_name] = item;
                itemByLocalizedName[Localization.instance.Localize(component.m_itemData.m_shared.m_name)] = item;
            }

            Localization.OnLanguageChange += () =>
            {
                itemByLocalizedName.Clear();
                for (int i = 0; i < __instance.m_items.Count; ++i)
                {
                    var item = __instance.m_items[i];
                    if (!item.TryGetComponent(out ItemDrop component)) continue;
                    if (component.m_itemData.m_shared.m_icons.Length <= 0) continue;
                    itemByLocalizedName[Localization.instance.Localize(component.m_itemData.m_shared.m_name)] = item;
                }
            };
        }
    }

    public static bool HasItem(this Player player, string sharedName, int count)
    {
        var inventory = player.GetInventory();
        var amount = inventory.CountItems(sharedName);
        return amount >= count;
    }
    
    public static bool GetRarity(string json, out int rarity)
    {
        string key = "\"Rarity\":";
        int start = json.IndexOf(key, StringComparison.Ordinal) + key.Length;
        int end = json.IndexOf(',', start);
        string value = json.Substring(start, end - start).Trim();
        return int.TryParse(value, out rarity);
    }
}