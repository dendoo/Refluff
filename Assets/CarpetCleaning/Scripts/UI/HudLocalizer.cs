using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;
using UnityEngine.UIElements;

namespace CarpetCleaning
{
    /// <summary>
    /// Puts the current language on the HUD (Docs/LOCALIZATION.md 3). On CarpetUI next to the other HUD components.
    ///  - Static texts: every UNNAMED Label in CarpetHud.uxml whose text is a key of Resources/Loc/en.json is registered
    ///    once in Start and re-texted on Loc.Changed. Named labels belong to C# (CleaningHUD, EconomyHud, ShopView,
    ///    PayoutView, PauseMenu), which translate their own texts. Order -50: the walk sees the tree as the UXML built it,
    ///    before any component adds rows of its own.
    ///  - Font: Base languages keep the theme's Inter; Arabic, Thai, Japanese, Korean and Chinese put their Noto Sans font
    ///    (UI/Fonts, LocFontBuilder) inline on the root, so every label inherits it, Latin letters and digits included.
    ///  - Direction: Arabic sets languageDirection RTL on the root; the classes lang-&lt;code&gt;, script-&lt;script&gt; and lang-rtl on
    ///    .hud let CarpetHud.uss adjust spacing and mirror the rows that pair a text with a chip.
    ///  - Key chips (.kbd with one letter): the letter the player's keyboard layout prints on that key (KeyNames).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(-50)]
    public sealed class HudLocalizer : MonoBehaviour
    {
        [Header("Fonts (UI/Fonts, CarpetCleaning/Localization/Build Font Assets). Base languages use Unity's built-in Inter.")]
        public FontAsset arabic;
        public FontAsset thai;
        public FontAsset japanese;
        public FontAsset korean;
        public FontAsset simplifiedChinese;
        public FontAsset traditionalChinese;

        static readonly CultureInfo C = CultureInfo.InvariantCulture;

        UIDocument _doc;
        VisualElement _root, _hud;
        readonly List<TextElement> _textEls = new List<TextElement>(64);
        readonly List<string> _textKeys = new List<string>(64);
        readonly List<TextElement> _chipEls = new List<TextElement>(8);
        readonly List<Key> _chipKeys = new List<Key>(8);
        readonly List<string> _classes = new List<string>(4);
        bool _built;
        int _applies;

        public bool Built { get { return _built; } }
        /// <summary>How many times the language was put on the HUD (debug).</summary>
        public int Applies { get { return _applies; } }

        void Awake() { _doc = GetComponent<UIDocument>(); }

        void OnEnable()
        {
            Loc.Changed += OnLanguageChanged;
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        void OnDisable()
        {
            Loc.Changed -= OnLanguageChanged;
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        void Start() { Build(); }

        void Update() { if (!_built) Build(); }

        void Build()
        {
            if (_built) return;
            if (_doc == null) _doc = GetComponent<UIDocument>();
            _root = _doc != null ? _doc.rootVisualElement : null;
            if (_root == null) return;
            _hud = _root.Q<VisualElement>(className: "hud");
            if (_hud == null) return;   // UIDocument not ready yet: Update retries
            _root.Query<TextElement>().ForEach(Register);
            // numbers read left to right in every language: under an RTL root bidi would turn "+$120" into "$120+"
            foreach (var n in LtrNumbers)
            {
                var e = _root.Q<VisualElement>(n);
                if (e != null) e.languageDirection = LanguageDirection.LTR;
            }
            _built = true;
            Apply();
        }

        static readonly string[] LtrNumbers =
            { "clean-value", "dry-value", "clock-value", "wallet-value", "shop-wallet-value", "menu-volume-value", "fps-label" };

        void Register(TextElement te)
        {
            string t = te.text;
            if (string.IsNullOrEmpty(t)) return;
            if (te.ClassListContains("kbd"))
            {
                var k = KeyNames.FromLetter(t);
                if (k != Key.None) { _chipEls.Add(te); _chipKeys.Add(k); }
                return;
            }
            if (!string.IsNullOrEmpty(te.name)) return;   // named = driven from C#
            if (!Loc.IsKey(t)) return;                     // "Refluff", "Esc", digits: not translated
            _textEls.Add(te);
            _textKeys.Add(t);
        }

        void OnLanguageChanged() { Apply(); }

        void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            // a keyboard layout switch reconfigures the device: the chips follow it
            if (_built && device is Keyboard && (change == InputDeviceChange.ConfigurationChanged || change == InputDeviceChange.Added))
                ApplyChips();
        }

        /// <summary>Puts Loc.Current on the HUD: font, direction, classes, the registered texts and chips.</summary>
        public void Apply()
        {
            if (!_built) return;
            _applies++;
            var lang = Loc.Current;
            _root.style.unityFontDefinition = RootFont(lang.script);
            _root.languageDirection = lang.Rtl ? LanguageDirection.RTL : LanguageDirection.Inherit;
            foreach (var c in _classes) _hud.RemoveFromClassList(c);
            _classes.Clear();
            _classes.Add("lang-" + lang.code);
            _classes.Add("script-" + ScriptClass(lang.script));
            if (lang.Rtl) _classes.Add("lang-rtl");
            foreach (var c in _classes) _hud.AddToClassList(c);
            for (int i = 0; i < _textEls.Count; i++)
            {
                string s = Loc.T(_textKeys[i]);
                if (_textEls[i].text != s) _textEls[i].text = s;
            }
            ApplyChips();
        }

        void ApplyChips()
        {
            for (int i = 0; i < _chipEls.Count; i++)
            {
                string s = KeyNames.Label(_chipKeys[i]);
                if (_chipEls[i].text != s) _chipEls[i].text = s;
            }
        }

        /// <summary>The bundled font of a script; null for Base (the theme's Inter).</summary>
        public FontAsset FontFor(LocScript script)
        {
            switch (script)
            {
                case LocScript.Arabic: return arabic;
                case LocScript.Thai: return thai;
                case LocScript.Japanese: return japanese;
                case LocScript.Korean: return korean;
                case LocScript.SimplifiedChinese: return simplifiedChinese;
                case LocScript.TraditionalChinese: return traditionalChinese;
                default: return null;
            }
        }

        /// <summary>Inline font for the HUD root: the script's font, or no inline value (the theme's Inter).</summary>
        StyleFontDefinition RootFont(LocScript script)
        {
            var f = FontFor(script);
            return f != null ? new StyleFontDefinition(FontDefinition.FromSDFFont(f)) : new StyleFontDefinition(StyleKeyword.Null);
        }

        /// <summary>Font for a single element that must show another language than the HUD's (the language list: every name
        /// in its own script). Base = the initial value, i.e. the theme's Inter even under a CJK root.</summary>
        public StyleFontDefinition ElementFont(LocScript script)
        {
            var f = FontFor(script);
            return f != null ? new StyleFontDefinition(FontDefinition.FromSDFFont(f)) : new StyleFontDefinition(StyleKeyword.Initial);
        }

        public static string ScriptClass(LocScript s)
        {
            switch (s)
            {
                case LocScript.Arabic: return "arabic";
                case LocScript.Thai: return "thai";
                case LocScript.Japanese: return "cjk";
                case LocScript.Korean: return "cjk";
                case LocScript.SimplifiedChinese: return "cjk";
                case LocScript.TraditionalChinese: return "cjk";
                default: return "base";
            }
        }

        // ------------------------------------------------------------------ debug (CarpetDebugApi.LocJson / LocOverflow)
        public string DebugJson()
        {
            var lang = Loc.Current;
            var sb = new StringBuilder(256);
            sb.Append("{\"built\":").Append(_built ? "true" : "false");
            sb.Append(",\"code\":\"").Append(lang.code).Append('"');
            sb.Append(",\"source\":\"").Append(Loc.Source).Append('"');
            sb.Append(",\"saved\":\"").Append(Loc.SavedCode).Append('"');
            sb.Append(",\"steam\":\"").Append(SteamService.GameLanguage).Append('"');
            sb.Append(",\"system\":\"").Append(Application.systemLanguage).Append('"');
            sb.Append(",\"keys\":").Append(Loc.KeyCount.ToString(C));
            sb.Append(",\"missing\":").Append(Loc.MissingCount.ToString(C));
            sb.Append(",\"texts\":").Append(_textEls.Count.ToString(C));
            sb.Append(",\"chips\":").Append(_chipEls.Count.ToString(C));
            sb.Append(",\"applies\":").Append(_applies.ToString(C));
            sb.Append(",\"font\":\"").Append(FontFor(lang.script) != null ? FontFor(lang.script).name : "Inter (theme)").Append('"');
            sb.Append(",\"rtl\":").Append(_root != null && _root.languageDirection == LanguageDirection.RTL ? "true" : "false");
            sb.Append(",\"fontsWired\":").Append((arabic != null && thai != null && japanese != null && korean != null && simplifiedChinese != null && traditionalChinese != null) ? "true" : "false");
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Every shown text element whose text does not fit its box: a single-line label wider than its content box, or a
        /// wrapping one taller than it. Ellipsis-free overflow is what a longer translation does to a fixed-width HUD.
        /// </summary>
        public string OverflowJson()
        {
            if (!_built) return "{\"error\":\"not built\"}";
            var sb = new StringBuilder(512);
            int n = 0, checkedCount = 0;
            sb.Append("{\"code\":\"").Append(Loc.Code).Append("\",\"items\":[");
            _root.Query<TextElement>().ForEach(te =>
            {
                if (string.IsNullOrEmpty(te.text) || !Shown(te)) return;
                var box = te.contentRect;
                if (box.width <= 0f) return;
                checkedCount++;
                bool wraps = te.resolvedStyle.whiteSpace == WhiteSpace.Normal;
                Vector2 need = wraps
                    ? te.MeasureTextSize(te.text, box.width, VisualElement.MeasureMode.Exactly, 0f, VisualElement.MeasureMode.Undefined)
                    : te.MeasureTextSize(te.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
                float over = wraps ? need.y - box.height : need.x - box.width;
                if (over <= 1.5f) return;
                if (n++ > 0) sb.Append(',');
                sb.Append("{\"text\":\"").Append(Esc(te.text)).Append("\",\"path\":\"").Append(Path(te)).Append("\",\"over\":")
                  .Append(over.ToString("0.#", C)).Append(",\"box\":").Append(box.width.ToString("0.#", C)).Append(",\"wraps\":")
                  .Append(wraps ? "true" : "false").Append('}');
            });
            sb.Append("],\"overflowing\":").Append(n.ToString(C)).Append(",\"checked\":").Append(checkedCount.ToString(C)).Append('}');
            return sb.ToString();
        }

        static bool Shown(VisualElement e)
        {
            for (var p = e; p != null; p = p.parent)
            {
                if (p.resolvedStyle.display == DisplayStyle.None) return false;
                if (p.resolvedStyle.visibility == Visibility.Hidden) return false;
            }
            return e.panel != null;
        }

        static string Path(VisualElement e)
        {
            var parts = new List<string>(6);
            for (var p = e; p != null && parts.Count < 4; p = p.parent)
            {
                string id = !string.IsNullOrEmpty(p.name) ? "#" + p.name : null;
                if (id == null) foreach (var c in p.GetClasses()) { id = "." + c; break; }
                if (id != null) parts.Add(id);
            }
            parts.Reverse();
            return string.Join(" ", parts);
        }

        static string Esc(string s) { return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n"); }
    }
}
