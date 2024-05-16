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
using Patches = MarketStall.Utility.Patches;

namespace MarketStall
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class MarketStallPlugin : BaseUnityPlugin
    {
        internal const string ModName = "MarketStall";
        internal const string ModVersion = "1.0.7";
        internal const string Author = "RustyMods";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource MarketStallLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        public static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        public enum Toggle { On = 1, Off = 0 }

        public void Awake()
        {
            Localizer.Load();
            InitConfigs();
            Utility.LoadAssets.InitAssetBundle();
            MarketStallPieces.InitPieces();

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        public void Update()
        {
            Patches.UpdateMarketGUI();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        #region ConfigOptions
        #region Config Variables
        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        
        public static ConfigEntry<string> _Currency = null!;
        public static ConfigEntry<int> _MaxSales = null!;

        public static ConfigEntry<Toggle> _UseSalesTax = null!;
        public static ConfigEntry<float> _Fee = null!;
        public static ConfigEntry<int> _MinimumFee = null!;

        public static ConfigEntry<Toggle> _TransparentBackground = null!;
        public static ConfigEntry<int> _MessageIncrement = null!;
        public static ConfigEntry<MessageColor> _MessageColor = null!;

        public enum MessageColor{White, Orange, Yellow, Red, Green}
        #endregion
        private void InitConfigs()
        {
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            _Currency = config("2 - Settings", "Currency", "Coins", "Set the prefab name of the currency. ex: Coins");
            _MaxSales = config("2 - Settings", "Max Sales", 20,
                new ConfigDescription("Amount of items allowed to put up for sale at the same time",
                    new AcceptableValueRange<int>(1, 200)));

            _UseSalesTax = config("3 - Sales Tax", "Enabled", Toggle.On, "If on, sales tax is applied to market");
            _Fee = config("3 - Sales Tax", "Fee Percentage", 5f,
                new ConfigDescription("Set the selling fee, a percentage added to the total cost",
                    new AcceptableValueRange<float>(0f, 100f)));
            _MinimumFee = config("3 - Sales Tax", "Minimum Fee", 1,
                new ConfigDescription("Minimum required fee to put an item up for sale",
                    new AcceptableValueRange<int>(1, 200)));

            _TransparentBackground = config("1 - General", "Transparent Background", Toggle.Off,
                "If on, market background is transparent");
            _MessageIncrement = config("1 - General", "Message Position", 20,
                new ConfigDescription("Set the position of heads up message, higher number lowers placement",
                    new AcceptableValueRange<int>(0, 40)), false);
            _MessageColor = config("1 - General", "Message Color", MessageColor.Orange,
                "Set the color of the pop-up message", false);

        }
        #region Config Methods
        
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
        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
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

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string? Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }
        #endregion
        #endregion
    }
}