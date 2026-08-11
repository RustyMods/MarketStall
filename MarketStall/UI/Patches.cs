using GUIFramework;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarketStall;

public static class Patches
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    private static class InventoryGui_Awake_Patch
    {
        private static readonly GameObject go = BundleManager.Load<GameObject>("marketstall_bundle", "MarketStall_UI");
        
        private static void Postfix(InventoryGui __instance)
        {
            var averia = FontManager.GetTMPFont(FontManager.TMP_FontOptions.AveriaSansLibreOutline);

            var tmp = go.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmp.Length; ++i)
            {
                var text = tmp[i];
                text.font = averia.font;
                text.fontSharedMaterial = averia.material;
            }
            
            var uiTooltip = __instance.transform.parent.GetComponentInChildren<StoreGui>().m_listElement
                .GetComponent<UITooltip>();
            var tooltips = go.GetComponentsInChildren<UITooltip>(true);
            for (int i = 0; i < tooltips.Length; ++i)
            {
                var tooltip =  tooltips[i];
                tooltip.m_tooltipPrefab = uiTooltip.m_tooltipPrefab;
            }

            var buttons = go.GetComponentsInChildren<Button>(true);
            var sfx = __instance.GetComponentInChildren<ButtonSfx>(true);
            for (int i = 0; i < buttons.Length; ++i)
            {
                var button = buttons[i];
                button.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = sfx.m_sfxPrefab;
            }
            
            var textInput = __instance.transform.parent.GetComponentInChildren<GuiInputField>(true);
            go.GetComponentInChildren<GuiInputField>(true).spriteState = textInput.spriteState;

            var border = __instance.m_crafting.Find("TabsButtons/TabBorder").GetComponent<Image>();
            var line = go.transform.Find("Panel/Line").GetComponent<Image>();
            line.material = border.material;
            line.sprite = border.sprite;
            line.type = border.type;
            line.fillCenter = border.fillCenter;
            
            if (!go.GetComponent<MarketGUI>()) go.AddComponent<MarketGUI>();
            Object.Instantiate(go, __instance.transform);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    private static class InventoryGui_Hide_Patch
    {
        private static bool Prefix()
        {
            if (MarketGUI.instance.IsSearching()) return false;
            if (MarketGUI.IsVisible() && TextInput.instance.m_inputField.isFocused) return false;
            return true;
        }
        private static void Postfix() => MarketGUI.instance.Hide();
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.IsVisible))]
    private static class StoreGui_IsVisible_Patch
    {
        private static void Postfix(ref bool __result) => 
            __result |= MarketGUI.IsVisible();
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
    private static class PlayerController_TakeInput_Patch
    {
        private static void Postfix(ref bool __result) =>
            __result &= !MarketGUI.IsVisible();
    }
}