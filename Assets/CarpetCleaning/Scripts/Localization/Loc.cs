using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>Which font draws a language (Docs/LOCALIZATION.md 4). Base = Unity's built-in Inter (Latin, Greek, Cyrillic,
    /// Vietnamese); the others are the subset Noto Sans fonts in UI/Fonts (HudLocalizer puts them on the HUD root).</summary>
    public enum LocScript { Base = 0, Arabic = 1, Thai = 2, Japanese = 3, Korean = 4, SimplifiedChinese = 5, TraditionalChinese = 6 }

    public sealed class LocLanguage
    {
        /// <summary>File name in Resources/Loc and the saved PlayerPrefs value.</summary>
        public readonly string code;
        public readonly string englishName;
        /// <summary>The name the drop-down shows: always in the language itself.</summary>
        public readonly string nativeName;
        /// <summary>Steam API language (SteamApps.GetCurrentGameLanguage), Steamworks' "API language code".</summary>
        public readonly string steamName;
        public readonly LocScript script;
        /// <summary>Application.systemLanguage that picks this language when there is no Steam; Unknown = never from the OS.</summary>
        public readonly SystemLanguage system;

        public LocLanguage(string code, string englishName, string nativeName, string steamName, LocScript script, SystemLanguage system)
        {
            this.code = code; this.englishName = englishName; this.nativeName = nativeName;
            this.steamName = steamName; this.script = script; this.system = system;
        }

        public bool Rtl { get { return script == LocScript.Arabic; } }
    }

    /// <summary>
    /// UI localization (Docs/LOCALIZATION.md). Keys are the English text exactly as the game shows it (the UXML text, the
    /// C# literal, the Credits.txt line), so English needs no table and an untranslated key simply shows in English.
    /// Resources/Loc/en.json lists every key (HudLocalizer registers only UXML labels whose text is one); every other
    /// language is Resources/Loc/&lt;code&gt;.json, flat { "English": "translation" }, checked by Tools/loc/loc_tool.py.
    ///
    /// The language is, in order: the player's choice in the pause menu (PlayerPrefs refluff.language), else the language
    /// Steam runs the game in, else the OS language, else English. An automatic pick is not saved, so a player who never
    /// chose follows Steam's per-game language setting. Changed fires after every switch; the HUD re-texts itself live.
    /// </summary>
    public static class Loc
    {
        public const string PrefKey = "refluff.language";
        const string Folder = "Loc/";
        static readonly CultureInfo C = CultureInfo.InvariantCulture;

        /// <summary>Drop-down order = Steamworks' list: the big five, then alphabetical by English name. Keep in step with
        /// LANGS in Tools/loc/loc_tool.py.</summary>
        public static readonly LocLanguage[] Languages =
        {
            new LocLanguage("en", "English", "English", "english", LocScript.Base, SystemLanguage.English),
            new LocLanguage("fr", "French", "Français", "french", LocScript.Base, SystemLanguage.French),
            new LocLanguage("it", "Italian", "Italiano", "italian", LocScript.Base, SystemLanguage.Italian),
            new LocLanguage("de", "German", "Deutsch", "german", LocScript.Base, SystemLanguage.German),
            new LocLanguage("es", "Spanish (Spain)", "Español (España)", "spanish", LocScript.Base, SystemLanguage.Spanish),
            new LocLanguage("ar", "Arabic", "العربية", "arabic", LocScript.Arabic, SystemLanguage.Arabic),
            new LocLanguage("bg", "Bulgarian", "Български", "bulgarian", LocScript.Base, SystemLanguage.Bulgarian),
            new LocLanguage("cs", "Czech", "Čeština", "czech", LocScript.Base, SystemLanguage.Czech),
            new LocLanguage("da", "Danish", "Dansk", "danish", LocScript.Base, SystemLanguage.Danish),
            new LocLanguage("nl", "Dutch", "Nederlands", "dutch", LocScript.Base, SystemLanguage.Dutch),
            new LocLanguage("fi", "Finnish", "Suomi", "finnish", LocScript.Base, SystemLanguage.Finnish),
            new LocLanguage("el", "Greek", "Ελληνικά", "greek", LocScript.Base, SystemLanguage.Greek),
            new LocLanguage("hu", "Hungarian", "Magyar", "hungarian", LocScript.Base, SystemLanguage.Hungarian),
            new LocLanguage("id", "Indonesian", "Bahasa Indonesia", "indonesian", LocScript.Base, SystemLanguage.Indonesian),
            new LocLanguage("ja", "Japanese", "日本語", "japanese", LocScript.Japanese, SystemLanguage.Japanese),
            new LocLanguage("ko", "Korean", "한국어", "koreana", LocScript.Korean, SystemLanguage.Korean),
            new LocLanguage("ms", "Malay", "Bahasa Melayu", "malay", LocScript.Base, SystemLanguage.Unknown),
            new LocLanguage("no", "Norwegian", "Norsk", "norwegian", LocScript.Base, SystemLanguage.Norwegian),
            new LocLanguage("pl", "Polish", "Polski", "polish", LocScript.Base, SystemLanguage.Polish),
            new LocLanguage("pt-BR", "Portuguese (Brazil)", "Português (Brasil)", "brazilian", LocScript.Base, SystemLanguage.Portuguese),
            new LocLanguage("pt-PT", "Portuguese (Portugal)", "Português (Portugal)", "portuguese", LocScript.Base, SystemLanguage.Unknown),
            new LocLanguage("ro", "Romanian", "Română", "romanian", LocScript.Base, SystemLanguage.Romanian),
            new LocLanguage("ru", "Russian", "Русский", "russian", LocScript.Base, SystemLanguage.Russian),
            new LocLanguage("zh-Hans", "Simplified Chinese", "简体中文", "schinese", LocScript.SimplifiedChinese, SystemLanguage.ChineseSimplified),
            new LocLanguage("es-419", "Spanish (Latin America)", "Español (Latinoamérica)", "latam", LocScript.Base, SystemLanguage.Unknown),
            new LocLanguage("sv", "Swedish", "Svenska", "swedish", LocScript.Base, SystemLanguage.Swedish),
            new LocLanguage("th", "Thai", "ไทย", "thai", LocScript.Thai, SystemLanguage.Thai),
            new LocLanguage("zh-Hant", "Traditional Chinese", "繁體中文", "tchinese", LocScript.TraditionalChinese, SystemLanguage.ChineseTraditional),
            new LocLanguage("tr", "Turkish", "Türkçe", "turkish", LocScript.Base, SystemLanguage.Turkish),
            new LocLanguage("uk", "Ukrainian", "Українська", "ukrainian", LocScript.Base, SystemLanguage.Ukrainian),
            new LocLanguage("vi", "Vietnamese", "Tiếng Việt", "vietnamese", LocScript.Base, SystemLanguage.Vietnamese),
        };

        /// <summary>Fires after the language changed (never for a no-op switch).</summary>
        public static event Action Changed;

        static bool _init;
        static LocLanguage _current;
        static Dictionary<string, string> _table;   // null for English: the key is the text
        static HashSet<string> _keys;
        static string _source = "";

        // domain reload is disabled: a play session must not inherit the last one's subscribers or table
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Changed = null;
            _init = false;
            _current = null;
            _table = null;
            _keys = null;
            _source = "";
        }

        public static LocLanguage Current { get { EnsureInit(); return _current; } }
        public static string Code { get { return Current.code; } }
        /// <summary>How the current language was chosen: cmdline (-language), saved, steam, system, default, or set (a debug call).</summary>
        public static string Source { get { EnsureInit(); return _source; } }
        public static int KeyCount { get { EnsureInit(); return _keys.Count; } }
        /// <summary>Keys the current language file lacks (they show in English). 0 for English.</summary>
        public static int MissingCount
        {
            get
            {
                EnsureInit();
                if (_table == null) return 0;
                int n = 0;
                foreach (var k in _keys) if (!_table.ContainsKey(k)) n++;
                return n;
            }
        }

        /// <summary>The text for `key` in the current language; the key itself (= English) when it has no translation.</summary>
        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key)) return key ?? "";
            EnsureInit();
            if (_table != null && _table.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v)) return v;
            return key;
        }

        /// <summary>Marks a literal as a key that reaches T through a variable (it returns `key` unchanged), so
        /// Tools/loc/loc_tool.py finds it in the sources.</summary>
        public static string Key(string key) { return key; }

        /// <summary>T(key) formatted with the invariant culture ({0}, {1}); a translation with a broken placeholder falls
        /// back to the English pattern instead of throwing.</summary>
        public static string F(string key, params object[] args)
        {
            string pattern = T(key);
            try { return string.Format(C, pattern, args); }
            catch (FormatException)
            {
                Debug.LogWarning("[Loc] bad placeholder in the " + Code + " text for '" + key + "': " + pattern);
                return string.Format(C, key, args);
            }
        }

        /// <summary>Is `text` a key of Resources/Loc/en.json (i.e. something the tables translate)?</summary>
        public static bool IsKey(string text)
        {
            EnsureInit();
            return !string.IsNullOrEmpty(text) && _keys.Contains(text);
        }

        public static LocLanguage Find(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            foreach (var l in Languages) if (string.Equals(l.code, code, StringComparison.OrdinalIgnoreCase)) return l;
            return null;
        }

        public static LocLanguage FromSteam(string steamName)
        {
            if (string.IsNullOrEmpty(steamName)) return null;
            foreach (var l in Languages) if (string.Equals(l.steamName, steamName, StringComparison.OrdinalIgnoreCase)) return l;
            return null;
        }

        public static LocLanguage FromSystem(SystemLanguage sl)
        {
            if (sl == SystemLanguage.Unknown) return null;
            if (sl == SystemLanguage.Chinese) sl = SystemLanguage.ChineseSimplified;
            foreach (var l in Languages) if (l.system == sl) return l;
            return null;
        }

        /// <summary>The automatic choice: Steam's game language, else the OS language, else English.</summary>
        public static LocLanguage Detect(out string source)
        {
            var steam = FromSteam(SteamService.GameLanguage);
            if (steam != null) { source = "steam"; return steam; }
            var sys = FromSystem(Application.systemLanguage);
            if (sys != null) { source = "system"; return sys; }
            source = "default";
            return Languages[0];
        }

        public static string SavedCode { get { return PlayerPrefs.GetString(PrefKey, ""); } }

        /// <summary>Switches the language. remember = the player picked it (saved, wins over Steam from now on);
        /// false = a test or capture that must not overwrite the player's choice.</summary>
        public static bool SetLanguage(string code, bool remember = true)
        {
            EnsureInit();
            var lang = Find(code);
            if (lang == null) { Debug.LogWarning("[Loc] unknown language '" + code + "'"); return false; }
            if (remember)
            {
                PlayerPrefs.SetString(PrefKey, lang.code);
                PlayerPrefs.Save();
            }
            if (lang == _current) { if (remember) _source = "saved"; return true; }
            Apply(lang, remember ? "saved" : "set");
            Changed?.Invoke();
            return true;
        }

        /// <summary>Forgets the saved choice and goes back to the automatic one (debug / tests).</summary>
        public static void ClearSaved()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
            EnsureInit();
            var lang = Detect(out string src);
            bool changed = lang != _current;
            Apply(lang, src);
            if (changed) Changed?.Invoke();
        }

        static void EnsureInit()
        {
            if (_init) return;
            _init = true;
            var en = LoadTable("en");
            _keys = en != null ? new HashSet<string>(en.Keys) : new HashSet<string>();
            if (en == null) Debug.LogWarning("[Loc] Resources/Loc/en.json is missing: nothing will be translated");
            var forced = Find(CommandLineCode());
            var saved = Find(SavedCode);
            if (forced != null) Apply(forced, "cmdline");
            else if (saved != null) Apply(saved, "saved");
            else
            {
                var lang = Detect(out string src);
                Apply(lang, src);
            }
        }

        /// <summary>`Refluff.exe -language ja` starts in that language for this run without saving it (smoke tests,
        /// support); the menu can still switch.</summary>
        static string CommandLineCode()
        {
            string[] args;
            try { args = Environment.GetCommandLineArgs(); }
            catch (Exception) { return null; }
            for (int i = 0; i + 1 < args.Length; i++)
                if (string.Equals(args[i], "-language", StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        static void Apply(LocLanguage lang, string source)
        {
            Dictionary<string, string> table = null;
            if (lang.code != "en")
            {
                table = LoadTable(lang.code);
                if (table == null)
                {
                    Debug.LogWarning("[Loc] Resources/Loc/" + lang.code + ".json is missing: falling back to English");
                    lang = Languages[0];
                }
            }
            _current = lang;
            _table = table;
            _source = source;
        }

        static Dictionary<string, string> LoadTable(string code)
        {
            var asset = Resources.Load<TextAsset>(Folder + code);
            if (asset == null) return null;
            Dictionary<string, string> table;
            try { table = LocJson.ParseFlat(asset.text); }
            catch (FormatException e)
            {
                Debug.LogError("[Loc] Resources/Loc/" + code + ".json: " + e.Message);
                table = null;
            }
            Resources.UnloadAsset(asset);
            return table;
        }
    }
}
