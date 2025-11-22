using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SimpleJSON;

namespace KoMiKoZa.Necropolis.JSONLocalizeAPI
{
    /// <summary>
    /// Translation cache for a single mod assembly
    /// </summary>
    internal class TranslationCache
    {
        public Dictionary<string, List<Dictionary<string, string>>> ArrayTranslations;
        public Dictionary<string, Dictionary<string, string>> ObjectTranslations;
        public Dictionary<string, int> LastIndexByArray;
        public string CurrentLang;

        public TranslationCache()
        {
            ArrayTranslations = new Dictionary<string, List<Dictionary<string, string>>>();
            ObjectTranslations = new Dictionary<string, Dictionary<string, string>>();
            LastIndexByArray = new Dictionary<string, int>();
            CurrentLang = "en";
        }
    }

    /// <summary>
    /// JSON-based localization API for Necropolis mods.
    /// Provides runtime language detection and per-mod translation support.
    /// </summary>
    public static class JSONLocalizeAPI
    {
        private static Dictionary<Assembly, TranslationCache> translationsByAssembly = new Dictionary<Assembly, TranslationCache>();

        /// <summary>
        /// Get a random string from an array-based translation set.
        /// Avoids repeating the same index consecutively.
        /// </summary>
        /// <param name="callingAssembly">Assembly.GetExecutingAssembly() - used to find translations.json</param>
        /// <param name="arrayKey">Key in translations.json (e.g., "toast_messages")</param>
        /// <param name="fallback">Fallback text if translation not found</param>
        /// <returns>Localized string</returns>
        public static string GetRandomFromArray(Assembly callingAssembly, string arrayKey, string fallback)
        {
            try
            {
                if (callingAssembly == null)
                {
                    LogWarning("GetRandomFromArray: callingAssembly is null");
                    return fallback;
                }

                TranslationCache cache = GetOrLoadTranslations(callingAssembly);

                if (cache == null || !cache.ArrayTranslations.ContainsKey(arrayKey))
                {
                    if (JSONLocalizePlugin.DebugMode.Value)
                    {
                        LogInfo(string.Format("Array key '{0}' not found in {1}", arrayKey, callingAssembly.GetName().Name));
                    }
                    return fallback;
                }

                var list = cache.ArrayTranslations[arrayKey];
                if (list.Count == 0)
                {
                    LogWarning(string.Format("Array '{0}' is empty in {1}", arrayKey, callingAssembly.GetName().Name));
                    return fallback;
                }

                int index;
                int lastIndex = cache.LastIndexByArray.ContainsKey(arrayKey) ? cache.LastIndexByArray[arrayKey] : -1;

                if (list.Count > 1 && lastIndex >= 0)
                {
                    // Avoid repeating the same index
                    do
                    {
                        index = UnityEngine.Random.Range(0, list.Count);
                    } while (index == lastIndex);
                }
                else
                {
                    index = UnityEngine.Random.Range(0, list.Count);
                }

                cache.LastIndexByArray[arrayKey] = index;

                string result = GetLocalizedString(cache, list[index], fallback);

                if (JSONLocalizePlugin.DebugMode.Value)
                {
                    LogInfo(string.Format("GetRandomFromArray({0}): index={1}, result={2}", arrayKey, index, result));
                }

                return result;
            }
            catch (Exception ex)
            {
                LogError(string.Format("GetRandomFromArray error: {0}", ex));
                return fallback;
            }
        }

        /// <summary>
        /// Get a string from an object-based translation.
        /// For single strings that don't need randomization.
        /// </summary>
        /// <param name="callingAssembly">Assembly.GetExecutingAssembly() - used to find translations.json</param>
        /// <param name="objectKey">Key in translations.json (e.g., "achievement_unlocked")</param>
        /// <param name="fallback">Fallback text if translation not found</param>
        /// <returns>Localized string</returns>
        public static string GetFromObject(Assembly callingAssembly, string objectKey, string fallback)
        {
            try
            {
                if (callingAssembly == null)
                {
                    LogWarning("GetFromObject: callingAssembly is null");
                    return fallback;
                }

                TranslationCache cache = GetOrLoadTranslations(callingAssembly);

                if (cache == null || !cache.ObjectTranslations.ContainsKey(objectKey))
                {
                    if (JSONLocalizePlugin.DebugMode.Value)
                    {
                        LogInfo(string.Format("Object key '{0}' not found in {1}", objectKey, callingAssembly.GetName().Name));
                    }
                    return fallback;
                }

                string result = GetLocalizedString(cache, cache.ObjectTranslations[objectKey], fallback);

                if (JSONLocalizePlugin.DebugMode.Value)
                {
                    LogInfo(string.Format("GetFromObject({0}): result={1}", objectKey, result));
                }

                return result;
            }
            catch (Exception ex)
            {
                LogError(string.Format("GetFromObject error: {0}", ex));
                return fallback;
            }
        }

        /// <summary>
        /// Get a specific indexed string from an array.
        /// </summary>
        /// <param name="callingAssembly">Assembly.GetExecutingAssembly()</param>
        /// <param name="arrayKey">Key in translations.json</param>
        /// <param name="index">Zero-based index</param>
        /// <param name="fallback">Fallback text if not found</param>
        /// <returns>Localized string</returns>
        public static string GetFromArrayIndex(Assembly callingAssembly, string arrayKey, int index, string fallback)
        {
            try
            {
                if (callingAssembly == null)
                {
                    LogWarning("GetFromArrayIndex: callingAssembly is null");
                    return fallback;
                }

                TranslationCache cache = GetOrLoadTranslations(callingAssembly);

                if (cache == null || !cache.ArrayTranslations.ContainsKey(arrayKey))
                {
                    if (JSONLocalizePlugin.DebugMode.Value)
                    {
                        LogInfo(string.Format("Array key '{0}' not found in {1}", arrayKey, callingAssembly.GetName().Name));
                    }
                    return fallback;
                }

                var list = cache.ArrayTranslations[arrayKey];
                if (index < 0 || index >= list.Count)
                {
                    LogWarning(string.Format("Index {0} out of range for array '{1}' (count: {2}) in {3}",
                        index, arrayKey, list.Count, callingAssembly.GetName().Name));
                    return fallback;
                }

                string result = GetLocalizedString(cache, list[index], fallback);

                if (JSONLocalizePlugin.DebugMode.Value)
                {
                    LogInfo(string.Format("GetFromArrayIndex({0}, {1}): result={2}", arrayKey, index, result));
                }

                return result;
            }
            catch (Exception ex)
            {
                LogError(string.Format("GetFromArrayIndex error: {0}", ex));
                return fallback;
            }
        }

        /// <summary>
        /// Get or load translations for the specified assembly.
        /// Caches results to avoid repeated file I/O.
        /// </summary>
        private static TranslationCache GetOrLoadTranslations(Assembly assembly)
        {
            if (assembly == null)
            {
                return null;
            }

            // Check cache first
            if (translationsByAssembly.ContainsKey(assembly))
            {
                return translationsByAssembly[assembly];
            }

            // Load translations for this assembly
            TranslationCache cache = new TranslationCache();
            translationsByAssembly[assembly] = cache;

            try
            {
                string assemblyPath = assembly.Location;
                string assemblyDir = Path.GetDirectoryName(assemblyPath);
                string jsonPath = Path.Combine(assemblyDir, "translations.json");

                string assemblyName = assembly.GetName().Name;

                if (JSONLocalizePlugin.DebugMode.Value)
                {
                    LogInfo(string.Format("Looking for translations at: {0}", jsonPath));
                }

                if (!File.Exists(jsonPath))
                {
                    LogWarning(string.Format("No translations.json found for {0}, using fallbacks", assemblyName));
                    return cache;
                }

                string jsonContent = File.ReadAllText(jsonPath);

                if (JSONLocalizePlugin.DebugMode.Value)
                {
                    LogInfo(string.Format("JSON content length for {0}: {1} chars", assemblyName, jsonContent.Length));
                }

                JSONNode root = JSON.Parse(jsonContent);

                if (root == null)
                {
                    LogError(string.Format("Failed to parse translations.json for {0}", assemblyName));
                    return cache;
                }

                LogInfo(string.Format("Successfully parsed translations.json for {0}", assemblyName));

                // Load array-based and object-based translations
                foreach (KeyValuePair<string, JSONNode> section in root)
                {
                    if (section.Value.IsArray)
                    {
                        var list = new List<Dictionary<string, string>>();

                        foreach (JSONNode item in section.Value.AsArray)
                        {
                            var dict = new Dictionary<string, string>();
                            foreach (KeyValuePair<string, JSONNode> langPair in item)
                            {
                                dict[langPair.Key] = langPair.Value.Value;
                            }
                            list.Add(dict);
                        }

                        cache.ArrayTranslations[section.Key] = list;

                        if (JSONLocalizePlugin.DebugMode.Value)
                        {
                            LogInfo(string.Format("Loaded array '{0}' with {1} items for {2}",
                                section.Key, list.Count, assemblyName));
                        }
                    }
                    else if (section.Value.IsObject)
                    {
                        var dict = new Dictionary<string, string>();
                        foreach (KeyValuePair<string, JSONNode> langPair in section.Value)
                        {
                            dict[langPair.Key] = langPair.Value.Value;
                        }
                        cache.ObjectTranslations[section.Key] = dict;

                        if (JSONLocalizePlugin.DebugMode.Value)
                        {
                            LogInfo(string.Format("Loaded object '{0}' with {1} languages for {2}",
                                section.Key, dict.Count, assemblyName));
                        }
                    }
                }

                if (JSONLocalizePlugin.DebugMode.Value)
                {
                    LogInfo(string.Format("Loaded translations for {0}: {1} arrays, {2} objects",
                        assemblyName, cache.ArrayTranslations.Count, cache.ObjectTranslations.Count));
                }
            }
            catch (Exception ex)
            {
                LogError(string.Format("Error loading translations for {0}: {1}", assembly.GetName().Name, ex));
            }

            return cache;
        }

        /// <summary>
        /// Get localized string from dictionary with fallback chain:
        /// current language -> English -> first available -> fallback string
        /// </summary>
        private static string GetLocalizedString(TranslationCache cache, Dictionary<string, string> dict, string fallback)
        {
            if (dict == null || dict.Count == 0)
            {
                return fallback;
            }

            // Get current language at runtime
            string runtimeLang = GetCurrentLanguage();

            // Log language change
            if (runtimeLang != cache.CurrentLang)
            {
                cache.CurrentLang = runtimeLang;
                if (JSONLocalizePlugin.DebugMode.Value)
                {
                    LogInfo(string.Format("Language changed to: {0}", runtimeLang));
                }
            }

            // Try current language
            if (dict.ContainsKey(runtimeLang))
            {
                string value = dict[runtimeLang];
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            // Fallback to English
            if (dict.ContainsKey("en"))
            {
                string value = dict["en"];
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            // Return first available
            foreach (var kvp in dict)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    return kvp.Value;
                }
            }

            return fallback;
        }

        /// <summary>
        /// Get current language from game's string system.
        /// Defaults to English if unavailable.
        /// </summary>
        private static string GetCurrentLanguage()
        {
            try
            {
                string lang = Necro.UI.Strings.CurrentLang;
                if (string.IsNullOrEmpty(lang))
                {
                    return "en";
                }
                return lang;
            }
            catch
            {
                // Fallback if game's string system isn't available
                return "en";
            }
        }

        // Logging helpers
        private static void LogInfo(string message)
        {
            if (JSONLocalizePlugin.Logger != null)
            {
                JSONLocalizePlugin.Logger.LogInfo(string.Format("[LOCALIZE] {0}", message));
            }
        }

        private static void LogWarning(string message)
        {
            if (JSONLocalizePlugin.Logger != null)
            {
                JSONLocalizePlugin.Logger.LogWarning(string.Format("[LOCALIZE] {0}", message));
            }
        }

        private static void LogError(string message)
        {
            if (JSONLocalizePlugin.Logger != null)
            {
                JSONLocalizePlugin.Logger.LogError(string.Format("[LOCALIZE] {0}", message));
            }
        }
    }
}
