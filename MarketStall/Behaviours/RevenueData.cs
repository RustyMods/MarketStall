using System.Collections.Generic;
using System.Text;

namespace MarketStall;

public class RevenueData(ItemDrop.ItemData item, string merchant)
{
    public readonly ItemDrop.ItemData item = item;
    public readonly string merchant = merchant;
}

public class Revenue : List<RevenueData>
{
    public void Save(ZPackage pkg)
    {
        pkg.Write(Count);
        for (int i = 0; i < Count; ++i)
        {
            var data = this[i];
            pkg.Write(data.item.m_dropPrefab.name);
            pkg.Write(data.item.m_stack);
            pkg.Write(data.merchant);
        }
    }

    public void Load(ZPackage pkg, string merchant = "")
    {
        var count = pkg.ReadInt();
        for (int i = 0; i < count; ++i)
        {
            var prefabName = pkg.ReadString();
            var stack = pkg.ReadInt();
            var seller = pkg.ReadString();
            if (!string.IsNullOrEmpty(merchant) && merchant != seller) continue;
            var prefab = ObjectDB.instance.GetItemPrefab(prefabName);
            if (prefab == null || !prefab.TryGetComponent(out ItemDrop itemDrop)) continue;
            var item = itemDrop.m_itemData.Clone();
            item.m_dropPrefab = prefab;
            item.m_stack = stack;
            Add(new RevenueData(item, seller));
        }
    }

    public string GetTooltip()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < Count; ++i)
        {
            RevenueData data = this[i];
            sb.Append($"{data.item.m_shared.m_name} <color=orange>x{data.item.m_stack}</color>");
            if (i != Count - 1) sb.Append("\n");
        }
        return Localization.instance.Localize(sb.ToString());
    }
}