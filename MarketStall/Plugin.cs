using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using MarketStall.Managers;
using ServerSync;
using UnityEngine;

namespace MarketStall;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class MarketStallPlugin : BaseUnityPlugin
{
    internal const string ModName = "MarketStall";
    internal const string ModVersion = "2.0.0";
    internal const string Author = "RustyMods";
    private const string ModGUID = Author + "." + ModName;
    private static readonly string ConfigFileName = ModGUID + ".cfg";
    private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
    internal static string ConnectionError = "";
    private readonly Harmony _harmony = new(ModGUID);
    public static readonly ManualLogSource MarketStallLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
    public static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    public static ConfigEntry<Toggle> transparentPanel;
    public static ConfigEntry<string> filters;
    public static ConfigEntry<Vector2> panelPos;
    public static ConfigEntry<Toggle> creatorLocked;
    public static ConfigEntry<string> defaultModel;
    public static ConfigEntry<Toggle> onlyAdminCanChangeModel;
    public void Awake()
    {
        Localizer.Load();
        
        _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
            "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
        transparentPanel = config("2 - UI", "Transparent Panel", Toggle.Off,
            "If on, market stall panel will become transparent", false);
        transparentPanel.SettingChanged += (_, _) => MarketGUI.instance?.UpdatePanelBackground();
        filters = config("3 - Settings", "Filters", "SwordCheat,SledgeCheat",
            new ConfigDescription("List items that cannot be put up for sale", null, StringListConfig.attributes));
        panelPos = config("2 - UI", "Panel Position", new Vector2(960, 540), "Set panel position");
        panelPos.SettingChanged += (_, _) =>
        {
            if (MarketGUI.instance)
            {
                MarketGUI.instance.transform.position = panelPos.Value;
            }
        };
        creatorLocked = config("3 - Settings", "Only Creator Can Sell", Toggle.Off,
            "If on, only creator can put items for sale");
        defaultModel = config("3 - Settings", "Default Model", "Haldor", "Set default model");
        onlyAdminCanChangeModel = config("3 - Settings", "Only Admin Can Change Model", Toggle.Off,
            "If on, only admin can change models");
        
        Assembly assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        SetupWatcher();
    }

    private void OnDestroy() => Config.Save();

    private void SetupWatcher()
    {
        FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFileFullPath)) return;
        try
        {
            MarketStallLogger.LogDebug("ReadConfigValues called");
            Config.Reload();
        }
        catch
        {
            MarketStallLogger.LogError($"There was an issue loading your {ConfigFileName}");
            MarketStallLogger.LogError("Please check your config entries for spelling and format!");
        }
    }
    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription =
            new(
                description.Description +
                (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }
}

public enum Toggle { On = 1, Off = 0 }


public class ConfigurationManagerAttributes
{
    [UsedImplicitly] public int? Order = null!;
    [UsedImplicitly] public bool? Browsable = null!;
    [UsedImplicitly] public string? Category = null!;
    [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
}
