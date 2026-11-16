using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CarpetCleaning
{
    /// <summary>
    /// The pause menu (Docs/PAUSE_MENU.md): Esc or the Menu button in the top-right row. Resume, master volume, language
    /// (LanguagePicker, Docs/LOCALIZATION.md), Credits (UI/Credits.txt: the CC BY sound credits the licences require
    /// inside the game, Docs/AUDIO_CREDITS.md) and Quit.
    /// Modal like the shop: the simulation pauses (CarpetSimulation.UiPaused) and the tools stop (ToolController.UiModal).
    ///
    /// Runs before EconomyHud (230), so the Esc that closes the shop never also opens the menu in the same frame, and
    /// EconomyHud ignores U while the menu is open. Structure is CarpetHud.uxml (#menu), style CarpetHud.uss (.menu*).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(228)]
    public sealed class PauseMenu : MonoBehaviour
    {
        public CarpetSimulation sim;
        public ToolController tools;
        public EconomyHud economyHud;
        public UiAudio uiAudio;
        public HudLocalizer localizer;
        [Tooltip("UI/Credits.txt: the Credits page, one Label per line ('# ' starts a heading). SteamBuild also copies it next to the exe as CREDITS.txt.")]
        public TextAsset credits;

        /// <summary>PlayerPrefs key of the master volume (AudioListener.volume, 0..1).</summary>
        public const string VolumeKey = "refluff.masterVolume";
        const float EnterSeconds = 0.22f;
        static readonly CultureInfo C = CultureInfo.InvariantCulture;

        UIDocument _doc;
        VisualElement _layer, _panel, _mainPage, _creditsPage, _creditsBody;
        ScrollView _creditsScroll;
        Slider _volume;
        Label _volumeValue;
        LanguagePicker _picker;
        bool _built, _open, _onCredits, _creditsFilled;
        float _now, _openT = -10f;

        public bool IsOpen { get { return _open; } }
        public bool OnCredits { get { return _open && _onCredits; } }
        public LanguagePicker Picker { get { return _picker; } }
        /// <summary>How many times Quit was pressed (the Editor ignores Application.Quit; a test reads this instead).</summary>
        public int QuitRequests { get; private set; }

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            if (sim == null) sim = Object.FindAnyObjectByType<CarpetSimulation>();
            if (tools == null) tools = Object.FindAnyObjectByType<ToolController>();
            if (economyHud == null) economyHud = GetComponent<EconomyHud>();
            if (uiAudio == null) uiAudio = GetComponent<UiAudio>();
            if (localizer == null) localizer = GetComponent<HudLocalizer>();
            AudioListener.volume = SavedVolume();
        }

        void Start() { Build(); }

        void OnEnable() { Loc.Changed += OnLanguageChanged; }

        void OnDisable()
        {
            Loc.Changed -= OnLanguageChanged;
            if (_open) ApplyModal(false);
            _open = false;
        }

        public static float SavedVolume() { return Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f)); }

        // ------------------------------------------------------------------ construction
        void Build()
        {
            if (_built) return;
            if (_doc == null) _doc = GetComponent<UIDocument>();
            var root = _doc != null ? _doc.rootVisualElement : null;
            if (root == null || root.Q<VisualElement>("menu") == null) return;   // UIDocument not ready yet: Update retries

            _layer = root.Q<VisualElement>("menu");
            _panel = root.Q<VisualElement>("menu-panel");
            _mainPage = root.Q<VisualElement>("menu-main");
            _creditsPage = root.Q<VisualElement>("menu-credits-page");
            _creditsScroll = root.Q<ScrollView>("credits-scroll");
            _creditsBody = root.Q<VisualElement>("credits-body");
            _volume = root.Q<Slider>("menu-volume");
            _volumeValue = root.Q<Label>("menu-volume-value");

            Bind(root, "menu-button", () => SetOpen(!_open));
            Bind(root, "menu-resume", () => SetOpen(false));
            Bind(root, "menu-credits", () => ShowCredits(true));
            Bind(root, "menu-back", () => ShowCredits(false));
            Bind(root, "menu-quit", Quit);
            var scrim = root.Q<VisualElement>("menu-scrim");
            if (scrim != null) scrim.RegisterCallback<ClickEvent>(e => { if (e.target == scrim) SetOpen(false); });
            if (_volume != null)
            {
                _volume.focusable = false;
                _volume.lowValue = 0f; _volume.highValue = 1f;
                _volume.SetValueWithoutNotify(AudioListener.volume);
                _volume.RegisterValueChangedCallback(e => SetVolume(e.newValue, false));
            }
            SetVolumeLabel(AudioListener.volume);
            _picker = new LanguagePicker(root, localizer, uiAudio);
            if (_layer != null) _layer.style.display = DisplayStyle.None;
            _built = true;
        }

        /// <summary>Loc.Changed: the select, the credits (rebuilt in the new language) and the row captions. The static labels
        /// are HudLocalizer's.</summary>
        void OnLanguageChanged()
        {
            if (!_built) return;
            _picker?.Refresh();
            SetVolumeLabel(AudioListener.volume);
            _creditsFilled = false;
            _creditsBody?.Clear();
            if (_onCredits) FillCredits();
            if (_mainPage != null) _mainPage.schedule.Execute(EqualizeRowCaptions);
        }

        /// <summary>VOLUME and LANGUAGE get the width of the wider caption, so the slider and the select line up in every
        /// language. Runs a panel update after a switch, when the captions have their new text and font.</summary>
        void EqualizeRowCaptions()
        {
            if (_mainPage == null) return;
            var caps = _mainPage.Query<Label>(className: "menu__row-caption").ToList();
            float w = 72f;
            foreach (var c in caps)
                w = Mathf.Max(w, Mathf.Ceil(c.MeasureTextSize(c.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x));
            foreach (var c in caps) c.style.width = w;
        }

        static void Bind(VisualElement root, string name, System.Action action)
        {
            var b = root.Q<Button>(name);
            if (b == null) return;
            b.focusable = false;   // Enter is the UI Submit key and would re-click a focused button
            b.clicked += action;
        }

        /// <summary>One Label per line of UI/Credits.txt; '# ' lines are headings. Built on the first visit and again after a
        /// language switch. A heading is looked up in its uppercase form, a line as written (Docs/LOCALIZATION.md 2);
        /// attribution lines have no translation and stay English, laid out left to right even in an RTL language.</summary>
        void FillCredits()
        {
            if (_creditsFilled || _creditsBody == null) return;
            _creditsFilled = true;
            bool rtl = Loc.Current.Rtl;
            bool baseScript = Loc.Current.script == LocScript.Base;
            string text = credits != null ? credits.text : "Credits file missing (UI/Credits.txt).";
            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;   // the spacing comes from the heading margins
                bool head = line.StartsWith("# ");
                string key = head ? line.Substring(2).ToUpperInvariant() : line;
                bool translated = Loc.IsKey(key);
                var l = new Label(Loc.T(key)) { pickingMode = PickingMode.Ignore };
                l.AddToClassList(head ? "credits__head" : "credits__line");
                if (head) l.AddToClassList("caption");
                if (head && _creditsBody.childCount == 0) l.AddToClassList("credits__head--first");
                if (rtl && !translated)
                {
                    l.languageDirection = LanguageDirection.LTR;
                    l.AddToClassList("credits__line--ltr");
                }
                // an English attribution line under a non-Latin HUD font: Inter, as in English (Noto Sans Arabic's taller line
                // box made those lines look double-spaced)
                if (!baseScript && !translated && localizer != null) l.style.unityFontDefinition = localizer.ElementFont(LocScript.Base);
                _creditsBody.Add(l);
            }
        }

        // ------------------------------------------------------------------ state
        public void SetOpen(bool open)
        {
            if (!_built) Build();
            if (!_built || open == _open) return;
            if (open && economyHud != null && economyHud.ShopOpen) return;   // the shop owns the modal; Esc closes it first
            _open = open;
            _openT = _now;
            _picker?.Close();
            ShowCredits(false, false);
            _layer.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open && _volume != null) { _volume.SetValueWithoutNotify(AudioListener.volume); SetVolumeLabel(AudioListener.volume); }
            if (open && _mainPage != null) _mainPage.schedule.Execute(EqualizeRowCaptions);
            if (!open) PlayerPrefs.Save();
            ApplyModal(open);
            uiAudio?.Play(open ? UiSfx.Open : UiSfx.Close, 1f);
        }

        public void ShowCredits(bool on) { ShowCredits(on, true); }

        void ShowCredits(bool on, bool sound)
        {
            if (!_built) return;
            if (on) _picker?.Close();
            if (on) FillCredits();
            _onCredits = on;
            if (_mainPage != null) _mainPage.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
            if (_creditsPage != null) _creditsPage.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (_panel != null) _panel.EnableInClassList("menu--wide", on);
            if (on && _creditsScroll != null) _creditsScroll.scrollOffset = Vector2.zero;
            if (sound) uiAudio?.Play(UiSfx.Tick, 0.8f);
        }

        void ApplyModal(bool on)
        {
            if (sim != null) sim.UiPaused = on;
            if (tools != null) tools.UiModal = on;
        }

        public void SetVolume(float v, bool moveSlider)
        {
            v = Mathf.Clamp01(v);
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(VolumeKey, v);
            if (moveSlider && _volume != null) _volume.SetValueWithoutNotify(v);
            SetVolumeLabel(v);
        }

        void SetVolumeLabel(float v)
        {
            if (_volumeValue != null) _volumeValue.text = Loc.F("{0}%", Mathf.RoundToInt(v * 100f));
        }

        void Quit()
        {
            QuitRequests++;
            PlayerPrefs.Save();
            uiAudio?.Play(UiSfx.Close, 1f);
            Debug.Log("[PauseMenu] quit to desktop");
            Application.Quit();   // ignored in the Editor
        }

        // ------------------------------------------------------------------ per frame
        void Update()
        {
            if (!_built) { Build(); if (!_built) return; }
            _now += Time.unscaledDeltaTime;

            var kb = Keyboard.current;
            // Steam overlay up: its text fields must not open the menu
            if (kb != null && !(tools != null && tools.Suspended) && kb.escapeKey.wasPressedThisFrame)
            {
                if (_open)
                {
                    if (_picker != null && _picker.IsOpen) _picker.Close();   // Esc closes the language list first
                    else if (_onCredits) ShowCredits(false);
                    else SetOpen(false);
                }
                else if (economyHud == null || !economyHud.ShopOpen) SetOpen(true);
            }

            if (_open && _panel != null)
            {
                // the shop's entrance: a small overshoot in, opacity leading
                float a = UiEase.Window(_now, _openT, EnterSeconds);
                float s = Mathf.LerpUnclamped(0.94f, 1f, UiEase.OutBack(a, 2f));
                _panel.style.scale = new Scale(new Vector2(s, s));
                _panel.style.opacity = UiEase.Clamp01(a * 1.8f);
            }
            _picker?.Tick();
        }

        // ------------------------------------------------------------------ debug (CarpetDebugApi.MenuJson)
        public string DebugJson()
        {
            int lines = _creditsBody != null ? _creditsBody.childCount : 0;
            return "{\"built\":" + B(_built) + ",\"open\":" + B(_open) + ",\"credits\":" + B(OnCredits)
                + ",\"creditLines\":" + lines.ToString(C)
                + ",\"volume\":" + AudioListener.volume.ToString("0.###", C)
                + ",\"saved\":" + SavedVolume().ToString("0.###", C)
                + ",\"simPaused\":" + B(sim != null && sim.UiPaused)
                + ",\"toolsModal\":" + B(tools != null && tools.UiModal)
                + ",\"quits\":" + QuitRequests.ToString(C)
                + ",\"language\":\"" + Loc.Code + "\""
                + ",\"langListOpen\":" + B(_picker != null && _picker.IsOpen)
                + ",\"langItems\":" + (_picker != null ? _picker.ItemCount : 0).ToString(C) + "}";
        }

        static string B(bool v) { return v ? "true" : "false"; }
    }
}
