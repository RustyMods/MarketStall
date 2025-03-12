using HarmonyLib;

namespace MarketStall.MarketStall;

public static class InventoryGUI_Patches
{
    public static bool m_overrideUpdateRecipe;
    public static ItemDrop.ItemData? m_selectedItemData = null;
    public static MarketData.MarketTradeItem? m_item = null;
    
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    private static class InventoryGUI_UpdateRecipe_Patch
    {
        private static bool Prefix(InventoryGui __instance)
        {
            if (!m_overrideUpdateRecipe || m_selectedItemData is null) return true;
            __instance.m_craftingStationName.text = "Market Stall";
            __instance.m_craftingStationIcon.gameObject.SetActive(false);
            __instance.m_craftingStationLevelRoot.gameObject.SetActive(false);
            __instance.m_recipeIcon.enabled = true;
            __instance.m_recipeName.enabled = true;
            __instance.m_recipeIcon.sprite = m_selectedItemData.GetIcon();
            __instance.m_recipeDecription.enabled = true;
            __instance.m_recipeName.text = Localization.instance.Localize(m_selectedItemData.m_shared.m_name);
            var itemData = m_selectedItemData.Clone();
            if (m_item is not null) itemData.m_crafterName = m_item.m_crafter;
            if (m_item is not null) itemData.m_crafterID = m_item.m_crafterID;
            __instance.m_recipeDecription.text = Localization.instance.Localize(
                ItemDrop.ItemData.GetTooltip(itemData,m_item?.m_quality ?? itemData.m_quality, false,
                    Game.m_worldLevel, m_item?.m_stack ?? itemData.m_stack));
            __instance.m_itemCraftType.gameObject.SetActive(false);
            __instance.m_variantButton.gameObject.SetActive(false);
            __instance.m_minStationLevelIcon.gameObject.SetActive(false);
            __instance.m_minStationLevelText.text = "";
            __instance.m_reqList.Clear();
            foreach (var req in __instance.m_recipeRequirementList)
            {
                InventoryGui.HideRequirement(req.transform);
            }

            return false;
        }
    }
}