using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CarpetCleaning
{
    /// <summary>
    /// The language drop-down of the pause menu (Docs/LOCALIZATION.md 3). The select (#menu-lang) shows the current
    /// language in its own name; a click opens #lang-popup under it: one row per Loc.Languages entry, every name in its
    /// own script and font (HudLocalizer.ElementFont), the current one ticked. Picking a row switches the language at once
    /// (Loc.SetLanguage, saved) and the whole HUD re-texts itself. #lang-catcher covers the screen while the list is open,
    /// so a click beside it (or Esc, PauseMenu) closes the list, not the menu. Owned by PauseMenu like ShopView by EconomyHud.
    /// </summary>
    public sealed class LanguagePicker
    {
        const float EnterSeconds = 0.16f;

        readonly VisualElement _layer, _catcher, _popup;
        readonly ScrollView _list;
        readonly Button _select;
        readonly Label _value;
        readonly HudLocalizer _loc;
        readonly UiAudio _audio;
        readonly List<Button> _items = new List<Button>(32);
        bool _open;
        float _openT = -10f;

        public bool IsOpen { get { return _open; } }
        public bool Valid { get { return _select != null && _popup != null && _list != null && _catcher != null; } }
        public int ItemCount { get { return _items.Count; } }

        public LanguagePicker(VisualElement root, HudLocalizer loc, UiAudio audio)
        {
            _layer = root.Q<VisualElement>("menu");
            _select = root.Q<Button>("menu-lang");
            _value = root.Q<Label>("menu-lang-value");
            _catcher = root.Q<VisualElement>("lang-catcher");
            _popup = root.Q<VisualElement>("lang-popup");
            _list = root.Q<ScrollView>("lang-list");
            _loc = loc;
            _audio = audio;
            if (!Valid) { Debug.LogWarning("[LanguagePicker] #menu-lang / #lang-popup / #lang-list / #lang-catcher missing in CarpetHud.uxml"); return; }

            _select.focusable = false;
            _select.clicked += Toggle;
            _catcher.RegisterCallback<ClickEvent>(e => { if (e.target == _catcher) Close(); });
            _list.verticalScrollerVisibility = ScrollerVisibility.Auto;
            foreach (var lang in Loc.Languages)
            {
                var item = new Button { focusable = false };
                item.AddToClassList("lang-item");
                item.AddToClassList("ui-blocking");
                var name = new Label(lang.nativeName) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("lang-item__name");
                if (_loc != null) name.style.unityFontDefinition = _loc.ElementFont(lang.script);
                // a name in another script than the HUD's keeps its own direction: Arabic reads right to left in any HUD
                name.languageDirection = lang.Rtl ? LanguageDirection.RTL : LanguageDirection.LTR;
                item.Add(name);
                var tick = new VisualElement { pickingMode = PickingMode.Ignore };
                tick.AddToClassList("lang-item__tick");
                item.Add(tick);
                string code = lang.code;
                item.clicked += () => Pick(code);
                item.userData = code;
                _list.Add(item);
                _items.Add(item);
            }
            Hide();
            Refresh();
        }

        /// <summary>The select's text and the ticked row follow Loc.Current (after every switch).</summary>
        public void Refresh()
        {
            if (!Valid) return;
            var cur = Loc.Current;
            if (_value != null)
            {
                _value.text = cur.nativeName;
                if (_loc != null) _value.style.unityFontDefinition = _loc.ElementFont(cur.script);
            }
            foreach (var item in _items) item.EnableInClassList("is-active", (string)item.userData == cur.code);
        }

        public void Toggle() { if (_open) Close(); else Open(); }

        public void Open()
        {
            if (!Valid || _open) return;
            _open = true;
            _openT = Time.unscaledTime;
            Place();
            _catcher.style.display = DisplayStyle.Flex;
            _popup.style.display = DisplayStyle.Flex;
            _popup.style.opacity = 0f;
            _select.AddToClassList("is-open");
            Refresh();
            // the list has no layout until the next panel update: scroll to the ticked row once it has one
            _popup.schedule.Execute(ScrollToCurrent).ExecuteLater(1);
            _audio?.Play(UiSfx.Tick, 0.8f);
        }

        public void Close()
        {
            if (!Valid || !_open) return;
            _open = false;
            Hide();
            _audio?.Play(UiSfx.Tick, 0.6f, 0.9f);
        }

        void Hide()
        {
            _catcher.style.display = DisplayStyle.None;
            _popup.style.display = DisplayStyle.None;
            _select.RemoveFromClassList("is-open");
        }

        void Pick(string code)
        {
            if (!_open) return;
            _open = false;
            Hide();
            if (code != Loc.Code) Loc.SetLanguage(code, true);   // Loc.Changed -> HudLocalizer + every view, PauseMenu -> Refresh
            _audio?.Play(UiSfx.Tick, 0.9f, 1.1f);
        }

        /// <summary>Under the select, as wide as it (at least 220 px), in the menu layer's coordinates.</summary>
        void Place()
        {
            Rect r = _select.worldBound;
            Vector2 topLeft = _layer.WorldToLocal(new Vector2(r.xMin, r.yMax));
            Vector2 topRight = _layer.WorldToLocal(new Vector2(r.xMax, r.yMax));
            float w = Mathf.Max(topRight.x - topLeft.x, 220f);
            float x = Loc.Current.Rtl ? topRight.x - w : topLeft.x;
            _popup.style.left = x;
            _popup.style.top = topLeft.y + 6f;
            _popup.style.width = w;
        }

        void ScrollToCurrent()
        {
            if (!_open) return;
            foreach (var item in _items)
                if ((string)item.userData == Loc.Code) { _list.ScrollTo(item); break; }
        }

        /// <summary>Per frame while open: a short fade and drop in.</summary>
        public void Tick()
        {
            if (!_open || !Valid) return;
            float a = UiEase.Window(Time.unscaledTime, _openT, EnterSeconds);
            _popup.style.opacity = UiEase.Clamp01(a * 1.5f);
            _popup.style.translate = new Translate(new Length(0f, LengthUnit.Pixel), new Length(-6f * (1f - UiEase.OutCubic(a)), LengthUnit.Pixel));
        }

        /// <summary>Debug: picks a language as a click on its row would (the list does not have to be open).</summary>
        public bool DebugPick(string code)
        {
            if (!Valid || Loc.Find(code) == null) return false;
            _open = true;
            Pick(code);
            return true;
        }
    }
}
