using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using ServerSync;
using UnityEngine;
namespace MarketStall.Utility;

public static class Filters
{
    public static readonly CustomSyncedValue<List<string>> ServerIgnoreList = new(MarketStallPlugin.ConfigSync, "ServerIgnoreList", new());

    private static readonly string MarketFolderPath = Paths.ConfigPath + Path.DirectorySeparatorChar + "MarketStall";
    private static readonly string IgnoreListPath = MarketFolderPath + Path.DirectorySeparatorChar + "IgnoreList.yml";
    
    public static void InitServerIgnoreList()
    {
        if (!ZNet.instance) return;
        if (ZNet.instance.IsServer())
        {
            MarketStallPlugin.MarketStallLogger.LogDebug("Server: Initializing ignore list");
            if (!Directory.Exists(MarketFolderPath))
            {
                Directory.CreateDirectory(MarketFolderPath);
            }

            if (!File.Exists(IgnoreListPath))
            {
                File.WriteAllLines(IgnoreListPath, GetDefaultIgnoreList());
            }
            
            ServerIgnoreList.Value = ValidateIgnoreList();
            
            FileSystemWatcher FilterWatcher = new FileSystemWatcher()
            {
                Filter = "*.yml",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                SynchronizingObject = ThreadingHelper.SynchronizingObject,
                NotifyFilter = NotifyFilters.LastWrite
            };
            FilterWatcher.Changed += OnFilterChange;
            FilterWatcher.Created += OnFilterChange;
        }
        else
        {
            MarketStallPlugin.MarketStallLogger.LogDebug("Client: Awaiting server ignore list");
            ServerIgnoreList.ValueChanged += OnServerIgnoreListChange;
        }
        
    }

    private static void OnServerIgnoreListChange()
    {
        MarketStallPlugin.MarketStallLogger.LogDebug("Client: Received server ignore list");
    }

    private static void OnFilterChange(object sender, FileSystemEventArgs e)
    {
        ServerIgnoreList.Value = ValidateIgnoreList();
    }

    private static List<string> ValidateIgnoreList()
    {
        List<string> ValidatedPrefabs = new();
        foreach (string item in File.ReadAllLines(IgnoreListPath).ToList())
        {
            if (item.StartsWith("#")) continue;
            GameObject prefab = ObjectDB.instance.GetItemPrefab(item);
            if (prefab == null)
            {
                MarketStallPlugin.MarketStallLogger.LogDebug("Failed to find prefab: " + item);
                continue;
            }

            if (!prefab.TryGetComponent(out ItemDrop component))
            {
                MarketStallPlugin.MarketStallLogger.LogDebug("Failed to find prefab item drop Component: " + item);
                continue;
            }
            ValidatedPrefabs.Add(component.m_itemData.m_shared.m_name);
        }

        return ValidatedPrefabs;
    }

    private static List<string> GetDefaultIgnoreList()
    {
        return new()
        {
            "#Add Item names below to block prefabs: ",
            "SwordCheat",
            "Coins"
        };
    }
}