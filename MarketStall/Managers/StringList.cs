using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace MarketStall;

public class StringListConfig
{
    public static ConfigurationManagerAttributes attributes = new() { CustomDrawer = Draw };
    public readonly List<string> list;
    public StringListConfig(List<string> items) => list = items;
    public StringListConfig(string items) => list = items.Split(',').ToList();
    public static void Draw(ConfigEntryBase cfg)
    {
        bool locked = cfg.Description.Tags
            .Select(a =>
                a.GetType().Name == "ConfigurationManagerAttributes"
                    ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a)
                    : null).FirstOrDefault(v => v != null) ?? false;
        bool wasUpdated = false;
        List<string> strings = new();
        GUILayout.BeginVertical();
        foreach (var prefab in new StringListConfig((string)cfg.BoxedValue).list)
        {
            GUILayout.BeginHorizontal();
            var prefabName = prefab;
            var nameField = GUILayout.TextField(prefab);
            if (nameField != prefab && !locked)
            {
                wasUpdated = true;
                prefabName = nameField;
            }

            if (GUILayout.Button("x", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }) && !locked)
            {
                wasUpdated = true;
            }
            else
            {
                strings.Add(prefabName);
            }

            if (GUILayout.Button("+", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }) && !locked)
            {
                strings.Add("");
                wasUpdated = true;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        if (wasUpdated)
        {
            cfg.BoxedValue = new StringListConfig(strings).ToString();
        }
    }

    public override string ToString() => string.Join(",", list);
}