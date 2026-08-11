using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace MarketStall;

public static class StallVars
{
    public static readonly int inventory = "Stall.Inventory".GetStableHashCode();
    public static readonly int owner = "Stall.Owner".GetStableHashCode();
    public static readonly int revenue = "Stall.Revenue".GetStableHashCode();
    public static readonly int name = "Stall.Name".GetStableHashCode();
    public static readonly int inUse = "Stall.InUse".GetStableHashCode();
}

public class Stall : MonoBehaviour, Hoverable, Interactable, TextReceiver
{
    public ZNetView m_nview;
    public Piece m_piece;
    public NPCSelect m_npcSelect;
    public readonly MarketInventory m_inventory = new();

    public string m_lastPkg;

    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        m_piece = GetComponent<Piece>();
        m_npcSelect = GetComponent<NPCSelect>();
        SetAsPieceLayer();
        if (!m_nview.IsValid()) return;

        LoadInventory();
        m_inventory.m_onChanged += OnChanged;
        
        InvokeRepeating(nameof(LoadInventory), 10f, 10f);
    }

    public void SetAsPieceLayer()
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; ++i)
        {
            var collider = colliders[i];
            collider.gameObject.layer = 10;
        }
    }

    public void OnChanged()
    {
        var pkg = new ZPackage();
        m_inventory.Save(pkg);
        var base64 = pkg.GetBase64();
        m_nview.GetZDO().Set(StallVars.inventory, base64);
        m_lastPkg = base64;
    }

    public void LoadInventory()
    {
        if (!m_nview.IsValid()) return;
        var data = m_nview.GetZDO().GetString(StallVars.inventory);
        if (m_lastPkg == data) return;
        m_inventory.m_inventory.Clear();

        if (!string.IsNullOrEmpty(data))
        {
            var pkg = new ZPackage(data);
            m_inventory.Load(pkg);
        }

        m_lastPkg = data;
    }

    public string GetCreatorName() => m_nview.GetZDO().GetString(StallVars.owner);
    public bool IsCreator() => m_piece.IsCreator();
    
    public Revenue GetRevenue(string merchant = "")
    {
        Revenue result = [];
        var base64 = m_nview.GetZDO().GetString(StallVars.revenue);
        if (string.IsNullOrEmpty(base64)) return result;
        var pkg = new ZPackage(base64);
        result.Load(pkg, merchant);
        return result;
    }

    public void AddRevenue(ItemDrop.ItemData item, string merchant = "")
    {
        Revenue revenue = GetRevenue();
        revenue.Add(new RevenueData(item, merchant));
        SetRevenue(revenue);
    }

    public void RemoveRevenue(ItemDrop.ItemData item, string merchant)
    {
        Revenue revenue = GetRevenue();
        RevenueData match = revenue.Find(r =>
            r.item.m_shared.m_name == item.m_shared.m_name && 
            r.item.m_stack == item.m_stack && 
            r.merchant == merchant);
        if (match == null) return;
        revenue.Remove(match);
        SetRevenue(revenue);
    }

    public void SetRevenue(Revenue revenue)
    {
        var pkg = new ZPackage();
        revenue.Save(pkg);
        m_nview.GetZDO().Set(StallVars.revenue, pkg.GetBase64());
    }

    public void SetInUse(bool inUse)
    {
        m_nview.GetZDO().Set(StallVars.inUse, inUse);
    }

    public string GetHoverText()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(GetHoverName());
        sb.Append("\n");
        sb.Append("[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open");
        if (m_npcSelect != null && m_npcSelect.CanEditModel()) sb.Append("\n" + m_npcSelect.GetHoverText());
        return Localization.instance.Localize(sb.ToString());
    }

    public string GetHoverName()
    {
        string savedName = GetText();
        if (string.IsNullOrEmpty(savedName))
        {
            return GetCreatorName() + Localization.instance.Localize(" $market_label");
        }
        return savedName;
    }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (m_nview.GetZDO().GetBool(StallVars.inUse))
        {
            user.Message(MessageHud.MessageType.Center, "$msg_inuse");
            return false;
        }

        if (alt && m_npcSelect != null && m_npcSelect.CanEditModel())
        {
            TextInput.instance.RequestText(m_npcSelect, "$market_editmodel", 100);
            return false;
        }
        
        LoadInventory();

        SetInUse(true);

        MarketGUI.instance.Show(this, false);
        
        return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    public void DropInventory()
    {
        GameObject crate = ZNetScene.instance.GetPrefab("CargoCrate");
        List<ItemDrop.ItemData> items = [];
        foreach (var kvp in m_inventory.m_inventory)
        {
            items.Add(kvp.Value.item);
        }

        items.AddRange(GetRevenue().Select(r => r.item));

        GameObject instance = Instantiate(crate, transform.position, Quaternion.identity);
        Container container = instance.GetComponent<Container>();
        
        while (items.Count > 0)
        {
            if (!container.GetInventory().HaveEmptySlot())
            {
                instance = Instantiate(crate, transform.position, Quaternion.identity);
                container = instance.GetComponent<Container>();
            }
            
            ItemDrop.ItemData item = items.First();
            if (item.m_stack > item.m_shared.m_maxStackSize)
            {
                int diff = item.m_stack - item.m_shared.m_maxStackSize;
                ItemDrop.ItemData other = item.Clone();
                item.m_dropPrefab = item.m_dropPrefab;
                item.m_stack = diff;
                items.Add(other);
                item.m_stack = item.m_shared.m_maxStackSize;
            }

            container.GetInventory().AddItem(item);
            items.Remove(item);
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
    private static class Stall_DropInventory
    {
        private static void Postfix(Piece __instance)
        {
            if (__instance.TryGetComponent(out Stall component))
            {
                component.DropInventory();
            }
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
    private static class Stall_CanBeRemoved_Patch
    {
        private static void Postfix(Piece __instance, ref bool __result)
        {
            if (!__instance.GetComponent<Stall>()) return;
            __result &= __instance.IsCreator() || ZNet.instance.LocalPlayerIsAdminOrHost();
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
    private static class Piece_SetCreator_Patch
    {
        private static void Postfix(Piece __instance)
        {
            if (!__instance.GetComponent<Stall>()) return;
            __instance.m_nview.GetZDO().Set(StallVars.owner, Player.m_localPlayer.GetPlayerName());
        }
    }

    public string GetText() => m_nview.GetZDO().GetString(StallVars.name);

    public void SetText(string text)
    {
        m_nview.GetZDO().Set(StallVars.name, text);
        if (MarketGUI.instance.currentMarket == this)
        {
            MarketGUI.instance.SetHeader(text);
        }
    }
}

