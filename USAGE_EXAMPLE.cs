// Example: How to use JSONLocalizeAPI in your Necropolis mod
// Based on real-world implementation from ParryMechanic mod
//
// 1. Add [BepInDependency("komikoza.necropolis.jsonlocalizeapi")] to your plugin
// 2. Reference JSONLocalizeAPI.dll in your project
// 3. Create translations.json in your mod's folder (same directory as your DLL)
// 4. Use the API methods as shown below

using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KoMiKoZa.Necropolis.JSONLocalizeAPI;

namespace KoMiKoZa.Necropolis.ParryMechanic
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("komikoza.necropolis.jsonlocalizeapi")] // REQUIRED
    public class ParryPlugin : BaseUnityPlugin
    {
        private const string PLUGIN_GUID = "komikoza.necropolis.parrymechanic";
        private const string PLUGIN_NAME = "Parry Mechanic";
        private const string PLUGIN_VERSION = "1.1.0";

        internal static ManualLogSource Logger;
        internal static ConfigEntry<bool> ModEnabled;

        void Awake()
        {
            Logger = base.Logger;

            // Config setup
            ModEnabled = Config.Bind("General", "ModEnabled", true,
                "Enable or disable parry mechanics");

            if (!ModEnabled.Value)
            {
                Logger.LogInfo(string.Format("[{0}] Disabled", PLUGIN_NAME));
                return;
            }

            // Harmony patching
            try
            {
                var harmony = new Harmony(PLUGIN_GUID);
                harmony.PatchAll();
                Logger.LogInfo(string.Format("[{0}] v{1} loaded!", PLUGIN_NAME, PLUGIN_VERSION));
            }
            catch (Exception ex)
            {
                Logger.LogError(string.Format("[{0}] Failed to load: {1}", PLUGIN_NAME, ex));
            }
        }

        // Example 1: Random toast message from array
        // Called when player successfully parries an attack
        public static void ShowParryToast()
        {
            string message = JSONLocalizeAPI.GetRandomFromArray(
                Assembly.GetExecutingAssembly(),
                "toast_messages",
                "PARRY!" // fallback if translation not found
            );

            // Display using Necropolis UI system
            LazySingletonBehavior<UIManager>.Instance.CreateToast(message, null, null);
        }

        // Example 2: Single string from object (10 parries milestone)
        // Called when player reaches 10 parries in a run
        public static void ShowBrazenHead10Dialogue()
        {
            string speech = JSONLocalizeAPI.GetFromObject(
                Assembly.GetExecutingAssembly(),
                "brazen_head_10_parry",
                "I've seen this a thousand times, you're not special, but fine, I noticed."
            );

            // Trigger Brazen Head conversation
            MainHUD.StartConversation(speech, false, AudioState_ActiveSpeaker.BrazenHead);
        }

        // Example 3: Random dialogue from array (5 parries milestone)
        // Called when player reaches 5 parries in a run
        public static void ShowBrazenHead5Dialogue()
        {
            string speech = JSONLocalizeAPI.GetRandomFromArray(
                Assembly.GetExecutingAssembly(),
                "brazen_head_5_parry",
                "Impressive."
            );

            // Trigger Brazen Head conversation with random line
            MainHUD.StartConversation(speech, false, AudioState_ActiveSpeaker.BrazenHead);
        }

        // Example 4: Indexed array access (if you need sequential messages)
        public static void ShowTutorialStep(int step)
        {
            string message = JSONLocalizeAPI.GetFromArrayIndex(
                Assembly.GetExecutingAssembly(),
                "tutorial_steps",
                step,
                "Tutorial step"
            );

            LazySingletonBehavior<UIManager>.Instance.CreateToast(message, null, null);
        }
    }

    // Example Harmony patch showing API usage in actual game hooks
    [HarmonyPatch(typeof(Player_Attack), "OnSuccessfulParry")]
    static class Player_Attack_OnSuccessfulParry_Patch
    {
        static void Postfix()
        {
            try
            {
                if (!ParryPlugin.ModEnabled.Value) return;

                // Show localized toast message
                ParryPlugin.ShowParryToast();
            }
            catch (Exception ex)
            {
                ParryPlugin.Logger.LogError("[PARRY] Error in parry hook: " + ex);
            }
        }
    }
}

/* translations.json for this example (from ParryMechanic mod):
{
  "toast_messages": [
    {
      "en": "Oh, look at you. A parry. How impressive.",
      "ru": "Ого, смотри-ка. Парирование. Очень впечатляет.",
    },
    {
      "en": "A parry? That was definitely skill and not luck.",
      "ru": "Парирование? Ну конечно же навык, а не просто везение."
    },
    {
      "en": "Blocked it. Good for you.",
      "ru": "Заблокировал. Молодец."
    }
  ],

  "brazen_head_5_parry": [
    {
      "en": "Your defensive prowess is... adequate.",
      "ru": "Твои навыки защиты... приемлемы."
    },
    {
      "en": "Favoring deflection over aggression. A coward's approach, some might say.",
      "ru": "Отражение вместо нападения. Некоторые назвали бы это трусостью."
    }
  ],

  "brazen_head_10_parry": {
    "en": "I've seen this a thousand times, you're not special, but fine, I noticed.",
    "ru": "Я видел это тысячу раз, ничего особенного, но что ж, похвалю тебя за старания."
  },

  "tutorial_steps": [
    {
      "en": "Right-click just before an enemy attack to parry",
      "ru": "ПКМ перед атакой врага для парирования"
    },
    {
      "en": "Successful parries stagger enemies",
      "ru": "Успешное парирование оглушает врагов"
    }
  ]
}
*/

