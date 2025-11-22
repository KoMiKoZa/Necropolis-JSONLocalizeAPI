using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace KoMiKoZa.Necropolis.JSONLocalizeAPI
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class JSONLocalizePlugin : BaseUnityPlugin
    {
        private const string GUID = "komikoza.necropolis.jsonlocalizeapi";
        private const string NAME = "JSONLocalizeAPI";
        private const string VERSION = "1.0.0";

        internal static ManualLogSource Logger;
        internal static ConfigEntry<bool> DebugMode;

        public static JSONLocalizePlugin Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            // System mod - NO ModEnabled toggle (other mods depend on it)
            DebugMode = Config.Bind("Debug", "DebugMode", false,
                "Enable debug logging for translation loading and lookups");

            Logger.LogInfo("================================================");
            Logger.LogInfo(string.Format("[{0}] v{1} loaded!", NAME, VERSION));
            Logger.LogInfo(" - JSON-based localization for mod content");
            Logger.LogInfo(" - Runtime language detection");
            Logger.LogInfo(" - Per-mod translation support");
            Logger.LogInfo("================================================");
        }
    }
}
