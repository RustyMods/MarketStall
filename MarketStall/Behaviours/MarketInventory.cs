using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarketStall;

public class MarketInventory
{
    public event Action m_onChanged;
    public readonly Dictionary<string, MarketItem> m_inventory = new();

    public void Save(ZPackage pkg)
    {
        var inventory = m_inventory.Values.ToList();
        pkg.Write(inventory.Count);
        for (int i = 0; i < inventory.Count; ++i)
        {
            var stallItem = inventory[i];
            if (stallItem.item.m_dropPrefab == null)
            {
                pkg.Write("");
            }
            else
            {
                pkg.Write(stallItem.item.m_dropPrefab.name);
            }
            pkg.Write(stallItem.item.m_stack);
            pkg.Write(stallItem.item.m_durability);
            pkg.Write(stallItem.item.m_quality);
            pkg.Write(stallItem.item.m_variant);
            pkg.Write(stallItem.item.m_crafterID);
            pkg.Write(stallItem.item.m_crafterName);
            pkg.Write(stallItem.item.m_customData.Count);
            foreach (var kvp in stallItem.item.m_customData)
            {
                pkg.Write(kvp.Key);
                pkg.Write(kvp.Value);
            }

            pkg.Write(stallItem.item.m_worldLevel);
            pkg.Write(stallItem.price);
            if (stallItem.currency.m_dropPrefab == null)
            {
                pkg.Write("");
            }
            else
            {
                pkg.Write(stallItem.currency.m_dropPrefab.name);
            }
            pkg.Write(stallItem.merchant);
            pkg.Write(stallItem.uniqueID);
        }
    }
    public void Load(ZPackage pkg)
    {
        var count = pkg.ReadInt();
        for (int i = 0; i < count; ++i)
        {
            var itemPrefabName = pkg.ReadString();
            var stack = pkg.ReadInt();
            var durability = pkg.ReadSingle();
            var quality = pkg.ReadInt();
            var variant = pkg.ReadInt();
            var crafterID = pkg.ReadLong();
            var crafterName = pkg.ReadString();
            var customDataCount = pkg.ReadInt();
            var customData = new Dictionary<string, string>();
            for (int k = 0; k < customDataCount; ++k)
            {
                var key = pkg.ReadString();
                var value = pkg.ReadString();
                customData[key] = value;
            }

            var worldLevel = pkg.ReadInt();
            var price = pkg.ReadInt();
            var currencyPrefabName = pkg.ReadString();
            var merchant = pkg.ReadString();
            var uniqueID = pkg.ReadString();

            var itemPrefab = ObjectDB.instance.GetItemPrefab(itemPrefabName);
            if (itemPrefab == null) continue;
            var currencyPrefab = ObjectDB.instance.GetItemPrefab(currencyPrefabName);
            if (currencyPrefab == null) continue;
            if (!itemPrefab.TryGetComponent(out ItemDrop itemDrop)) continue;
            if (!currencyPrefab.TryGetComponent(out ItemDrop currencyDrop)) continue;

            var item = itemDrop.m_itemData.Clone();
            var currency = currencyDrop.m_itemData.Clone();

            item.m_dropPrefab = itemPrefab;
            currency.m_dropPrefab = currencyPrefab;
            
            item.m_stack = stack;
            item.m_durability = durability;
            item.m_quality = quality;
            item.m_variant = variant;
            item.m_crafterID = crafterID;
            item.m_crafterName = crafterName;
            item.m_customData = customData;
            item.m_worldLevel = worldLevel;
            
            AddItem(item, price, currency, merchant, uniqueID);
        }
    }
    public void RemoveItem(string uniqueID)
    {
        m_inventory.Remove(uniqueID);
        if (m_onChanged != null) m_onChanged.Invoke();
    }

    public void AddItem(MarketItem stallItem)
    {
        if (string.IsNullOrEmpty(stallItem.uniqueID))
        {
            stallItem.uniqueID = Guid.NewGuid().ToString();
        }
        m_inventory[stallItem.uniqueID] = stallItem;
        if (m_onChanged != null) m_onChanged.Invoke();
    }
    
    public void AddItem(ItemDrop.ItemData item, int price, ItemDrop.ItemData currency, string merchant, string uniqueID = null)
    {
        var stallItem = new MarketItem()
        {
            item = item,
            price = price,
            currency = currency,
            merchant = merchant,
            uniqueID = string.IsNullOrEmpty(uniqueID) ? Guid.NewGuid().ToString() : uniqueID,
        };
        
        AddItem(stallItem);
    }
    
    public class MarketItem
    {
        public ItemDrop.ItemData item;
        public int price;
        public ItemDrop.ItemData currency;
        public string merchant;
        public string uniqueID;
    }
}