# JSONLocalizeAPI

### A lightweight JSON-based localization library for Necropolis mods. Provides easy multi-language support for mod-specific content.

## What Is This?

JSONLocalizeAPI is a **library mod** (API mod) that other mods can use to add localization to their custom Necropolis content. It handles:

- Loading translations from JSON files
- Runtime language detection (follows Necropolis' language setting)
- Fallback chains (current language → English → first available)
- Per-mod translation isolation (each mod has its own `translations.json`)
- Translation caching for performance

## Core Features

### Language Support
- **8 Languages (Necropolis Default)**: English, Russian, French, German, Spanish, Italian, Polish, Portuguese (Brazilian.
- **Runtime Detection**: Automatically follows game's language setting
- **Partial Translations**: Missing languages gracefully fall back to English

### Translation Types
1. **Random Arrays**: `GetRandomFromArray()` - Toast messages, random dialogue
2. **Single Strings**: `GetFromObject()` - UI labels etc.
3. **Indexed Arrays**: `GetFromArrayIndex()` - Sequential messages, tutorials

### Performance
- **Lazy Loading**: Translations load only when first needed
- **Assembly Caching**: Each mod's JSON is parsed once and cached
- **No Repeated I/O**: Dictionary-based lookups (O(1) performance)

You can safely call API methods frequently without performance concerns.

### Security
- **Graceful Fallbacks**: Never crashes - always returns valid string
- **Per-Mod Isolation**: Each mod has its own translations (no conflicts)
- **Debug Mode**: Detailed logging for troubleshooting

### Developer Friendly
- **Simple API**: 3 methods cover all use cases
- **No Setup Required**: Just call the API
- **BepInEx 5.x**: Standard modding framework

## Installation

### For Players

Install via your mod manager (r2modman, Thunderstore Mod Manager, etc.):

1. Search for "JSONLocalizeAPI"
2. Click Install
3. The mod manager will handle dependencies automatically

### For Mod Developers

#### Development Setup

1. **Reference the DLL** in your Visual Studio project:
   - Download JSONLocalizeAPI from Thunderstore
   - Extract `JSONLocalizeAPI.dll`
   - Add as reference in your mod project

2. **Add dependency** in your plugin code:
   ```csharp
   [BepInPlugin("komikoza.necropolis.parrymechanic", "Parry Mechanic", "1.1.0")]
   [BepInDependency("komikoza.necropolis.jsonlocalizeapi")]
   public class ParryPlugin : BaseUnityPlugin
   {
       // ...
   }
   ```

3. **Add to manifest.json**:
   ```json
   {
     "dependencies": [
       "BepInEx-BepInExPack_Necropolis-5.4.2304",
       "Komikoza-JSONLocalizeAPI-1.0.0"
     ]
   }
   ```

## Usage

### Quick Start

Create a `translations.json` file in your mod's folder (same directory as your DLL).

**Example from ParryMechanic mod:**

```json
{
  "toast_messages": [
    {
      "en": "Oh, look at you. A parry. How impressive.",
      "ru": "Ого, смотри-ка. Парирование. Очень впечатляет.",
    },
    {
      "en": "A parry? That was definitely skill and not luck.",
      "ru": "Парирование? Ну конечно же навык, а не просто везение."
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
  }
}
```

### API Methods

**Example from ParryMechanic mod:**

```csharp
using System.Reflection;
using KoMiKoZa.Necropolis.JSONLocalizeAPI;

// Example 1: Random parry toast message
void ShowParryToast()
{
    string message = JSONLocalizeAPI.GetRandomFromArray(
        Assembly.GetExecutingAssembly(),
        "toast_messages",
        "PARRY!" // fallback if not found
    );

    LazySingletonBehavior<UIManager>.Instance.CreateToast(message, null, null);
}

// Example 2: Single Brazen Head dialogue (10 parries)
void ShowBrazenHead10Dialogue()
{
    string speech = JSONLocalizeAPI.GetFromObject(
        Assembly.GetExecutingAssembly(),
        "brazen_head_10_parry",
        "I've seen this a thousand times, you're not special, but fine, I noticed."
    );

    MainHUD.StartConversation(speech, false, AudioState_ActiveSpeaker.BrazenHead);
}

// Example 3: Random Brazen Head dialogue (5 parries)
void ShowBrazenHead5Dialogue()
{
    string speech = JSONLocalizeAPI.GetRandomFromArray(
        Assembly.GetExecutingAssembly(),
        "brazen_head_5_parry",
        "Impressive."
    );

    MainHUD.StartConversation(speech, false, AudioState_ActiveSpeaker.BrazenHead);
}
```

### JSON Format

**Array-based translations** (for random selection):
```json
{
  "your_key_name": [
    {
      "en": "English text 1",
      "ru": "Russian text 1"
    },
    {
      "en": "English text 2",
      "ru": "Russian text 2"
    }
  ]
}
```

**Object-based translations** (single strings):
```json
{
  "your_key_name": {
    "en": "English text",
    "ru": "Russian text",
    "de": "German text"
  }
}
```

### Supported Languages

The game natively supports these language codes:

- `en` - English
- `ru` - Russian
- `fr` - French
- `de` - German
- `es` - Spanish
- `it` - Italian
- `pl` - Polish
- `pt-BR` - Portuguese (Brazilian)

**You don't need to translate to all languages!** The API will:
1. Try to use the player's current language
2. Fall back to English if not available
3. Fall back to first available language
4. Fall back to your hardcoded fallback string

### Best Practices

1. **Always provide English (`en`)** as baseline
2. **Use descriptive keys**: `"parry_toast_messages"` not `"messages1"`
3. **Include fallback strings**: Never pass empty/null fallbacks
4. **Test missing translations**: Delete `translations.json` and verify fallbacks work
5. **Keep translations short**: UI space is limited

### Example: Migrating Existing Code

**ParryMechanic Before (v1.0.0):**
```csharp
// Had custom ParryLocalization class
ParryLocalization.Initialize();

// In code:
string message = ParryLocalization.GetRandomToast();
LazySingletonBehavior<UIManager>.Instance.CreateToast(message, null, null);
```

**ParryMechanic After (v1.0.1):**
```csharp
// Add dependency
[BepInDependency("komikoza.necropolis.jsonlocalizeapi")]

// Remove ParryLocalization.Initialize() - API handles it

// In code:
string message = JSONLocalizeAPI.GetRandomFromArray(
    Assembly.GetExecutingAssembly(),
    "toast_messages",
    "PARRY!" // fallback
);
LazySingletonBehavior<UIManager>.Instance.CreateToast(message, null, null);
```

**Result:** Removed ~300 lines of custom localization code, simplified maintenance!

## File Structure

Your mod's file structure should look like:

```
BepInEx/plugins/
├── JSONLocalizeAPI/
│   └── JSONLocalizeAPI.dll    ← API (auto-installed via dependency)
│
└── ParryMechanic/
    ├── ParryMechanic.dll
    ├── translations.json       ← Your mod's translations
    └── README.txt
```

## Debug Mode

Enable debug logging in BepInEx config:

1. Open `BepInEx/config/komikoza.necropolis.jsonlocalizeapi.cfg`
2. Set `DebugMode = true`
3. Check `BepInEx/LogOutput.log` for detailed translation info

Debug logs show:
- Translation file loading
- Language changes
- Which translations were used
- Missing keys/files

## Troubleshooting

### "No translations.json found for [ModName]"

**Solution:** Ensure `translations.json` is in the same folder as your mod's DLL.

### Translations not changing when I switch language in-game

**Cause:** Language detection happens at runtime. If translations don't update:
1. Check that you're calling the API methods (not caching strings)
2. Verify DebugMode shows language changes in the log

### Always getting fallback strings

**Causes:**
1. `translations.json` is missing or in wrong location
2. JSON syntax error (check logs for parse errors)
3. Key name mismatch (check spelling/case-sensitivity)
4. Language code not present in your JSON

**Solution:** Enable DebugMode to see exactly what's happening.

### JSON parse errors

**Common issues:**
- Missing commas between array items
- Trailing commas (not allowed in JSON)
- Missing quotes around keys/values
- Unescaped special characters

Use a JSON validator (jsonlint.com) to check syntax.

## FAQ

> **Q: Can I use this to retranslate the game's content?**
- A: Not really. This is only efficient for new content added by your mod - things like button/setting names, dialogue pieces, item descriptions if it's one or two objects. For large scale translations and big amounts of text it's best to use CSVPatcher.

> **Q: Do I need to bundle JSONLocalizeAPI.dll with my mod?**
- A: No. Just add it as a dependency in `manifest.json`. Players will download it automatically.

> **Q: Can multiple mods share one translations.json?**
- A: No. Each mod should have its own. The API keeps them isolated by assembly.

> **Q: What if my translations.json is missing?**
- A: The API gracefully falls back to your hardcoded strings. Your mod won't crash.

> **Q: Can I add custom languages not in the game?**
- A: Yes, if another mod adds support for it. The API reads from `Necro.UI.Strings.CurrentLang`.

> **Q: Does this work with CSVPatcher mods?**
- A: Yes, they're complementary.

## Changelog

### 1.0.0
- Initial Thunderstore release

## Credits

- **Author**: KoMiKoZa  
**Discord**: komikoza
- **SimpleJSON:** Bunny83 (MIT License)

## Support

- **Website**: [Necropolis Mod Hub](https://komikoza.github.io/necropolis/mods/jsonlocalizeapi.html)
- **Source Code & Documentation**: [GitHub](https://github.com/KoMiKoZa/Necropolis-JSONLocalizeAPI)
- **Issues**: Report bugs on [Discord](https://discord.gg/2233yFQ) or [GitHub Issues](https://github.com/KoMiKoZa/Necropolis-JSONLocalizeAPI/issues)
