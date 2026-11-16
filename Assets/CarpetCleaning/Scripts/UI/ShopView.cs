using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace CarpetCleaning
{
    /// <summary>
    /// The upgrade shop (Docs/ECONOMY.md 5): one card per tool, built from ToolTable (the only data-driven list the HUD
    /// rules allow in C#), each with a POWER and an AREA row of seven level pips, the gain over the player's start and
    /// a price button. A purchase pops the new pip with a sparkle and the wallet counts down; a click without the money
    /// shakes the button. All motion is timeline-driven from EconomyHud's clock; colours and sizes are CarpetHud.uss.
    /// Below the floor tools, INSPECTION holds the one-time items (Docs/HIGHLIGHTS.md): two fixed cards in the UXML, bound
    /// here by name, bought once, then marked OWNED with their toggle key.
    /// Texts go through Loc (Docs/LOCALIZATION.md): Relocalize re-texts the cards after a language switch, and every POWER /
    /// AREA caption takes the width of the wider one so the pips stay in one column in every language.
    /// </summary>
    public sealed class ShopView
    {
        static readonly CultureInfo C = CultureInfo.InvariantCulture;

        sealed class Row
        {
            public ToolId tool;
            public UpgradeStat stat;
            public VisualElement[] pips;
            public Label gain;
            public Button buy;
            public Label price;
            public float popT = -10f, shakeT = -10f;
            public int popPip = -1;
        }

        readonly VisualElement _layer, _panel;
        readonly Economy _eco;
        readonly UiFx _fx;
        readonly UiAudio _audio;
        readonly List<Row> _rows = new List<Row>(20);
        readonly List<Label> _toolNames = new List<Label>(10);
        readonly List<string> _toolKeys = new List<string>(10);
        readonly List<Label> _captions = new List<Label>(20);
        readonly List<string> _captionKeys = new List<string>(20);

        sealed class ItemCard
        {
            public ShopItem item;
            public VisualElement card;
            public Button buy;
            public Label price, text;
            public string blurb;
            public float popT = -10f, shakeT = -10f;
        }

        readonly List<ItemCard> _items = new List<ItemCard>(ShopItems.Count);
        bool _open;
        float _openT = -10f;

        public bool IsOpen { get { return _open; } }
        public System.Action CloseRequested;

        public ShopView(VisualElement root, Economy eco, UiFx fx, UiAudio audio)
        {
            _layer = root.Q<VisualElement>("shop");
            _panel = root.Q<VisualElement>("shop-panel");
            _eco = eco; _fx = fx; _audio = audio;
            var carpet = root.Q<VisualElement>("shop-carpet-tools");
            var floor = root.Q<VisualElement>("shop-floor-tools");
            for (int i = 0; i < ToolTable.All.Length; i++)
            {
                var def = ToolTable.All[i];
                var list = def.floorTool ? floor : carpet;
                if (list != null) list.Add(BuildCard(def));
            }
            BindItem(root, ShopItem.MoistureHighlight, "item-moisture");
            BindItem(root, ShopItem.DirtHighlight, "item-dirt");
            var close = root.Q<Button>("shop-close");
            if (close != null) { close.focusable = false; close.clicked += () => CloseRequested?.Invoke(); }
            var scrim = root.Q<VisualElement>("shop-scrim");
            if (scrim != null) scrim.RegisterCallback<ClickEvent>(e => { if (e.target == scrim) CloseRequested?.Invoke(); });
            if (_layer != null) _layer.style.display = DisplayStyle.None;
        }

        public bool Valid { get { return _layer != null && _panel != null; } }

        /// <summary>Loc.Changed: tool names, POWER / AREA, prices and item texts in the new language.</summary>
        public void Relocalize()
        {
            for (int i = 0; i < _toolNames.Count; i++) _toolNames[i].text = Loc.T(_toolKeys[i]);
            for (int i = 0; i < _captions.Count; i++) _captions[i].text = Loc.T(_captionKeys[i]);
            Refresh();
            if (_panel != null) _panel.schedule.Execute(EqualizeCaptions);
        }

        /// <summary>Every POWER / AREA caption as wide as the widest, measured in the current font (a panel update after the
        /// text changed, when the new font is resolved).</summary>
        void EqualizeCaptions()
        {
            float w = 50f;
            foreach (var c in _captions)
                w = Mathf.Max(w, Mathf.Ceil(c.MeasureTextSize(c.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x));
            foreach (var c in _captions) c.style.width = w;
        }

        VisualElement BuildCard(ToolDefinition def)
        {
            var card = Ve("ucard");
            var head = Ve("ucard__head");
            // the tool's real key (9 / 0 for the floor pair); a tool without one gets no chip, never a placeholder glyph
            if (def.HasHotkey)
            {
                var key = new Label(def.HotkeyLabel) { pickingMode = PickingMode.Ignore };
                key.AddToClassList("kbd");
                head.Add(key);
            }
            else head.AddToClassList("ucard__head--plain");
            var name = new Label(Loc.T(def.name)) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("ucard__name");
            head.Add(name);
            _toolNames.Add(name);
            _toolKeys.Add(def.name);
            card.Add(head);
            card.Add(BuildRow(def.id, UpgradeStat.Power));
            card.Add(BuildRow(def.id, UpgradeStat.Area));
            return card;
        }

        VisualElement BuildRow(ToolId tool, UpgradeStat stat)
        {
            var row = new Row { tool = tool, stat = stat };
            var r = Ve("urow");
            r.AddToClassList(stat == UpgradeStat.Power ? "urow--power" : "urow--area");
            var icon = Ve("icon");
            icon.AddToClassList(stat == UpgradeStat.Power ? "icon--bolt" : "icon--expand");
            icon.AddToClassList("urow__icon");
            r.Add(icon);
            string capKey = stat == UpgradeStat.Power ? Loc.Key("POWER") : Loc.Key("AREA");
            var cap = new Label(Loc.T(capKey)) { pickingMode = PickingMode.Ignore };
            cap.AddToClassList("caption");
            cap.AddToClassList("urow__caption");
            r.Add(cap);
            _captions.Add(cap);
            _captionKeys.Add(capKey);
            var pips = Ve("pips");
            int n = (_eco != null ? _eco.MaxLevel : 6) + 1;
            row.pips = new VisualElement[n];
            for (int k = 0; k < n; k++)
            {
                var p = Ve("pip");
                p.usageHints = UsageHints.DynamicTransform;
                pips.Add(p);
                row.pips[k] = p;
            }
            r.Add(pips);
            row.gain = new Label("") { pickingMode = PickingMode.Ignore };
            row.gain.AddToClassList("urow__gain");
            row.gain.languageDirection = LanguageDirection.LTR;   // "x1.44 -> x1.73" must not be reordered in an RTL language
            r.Add(row.gain);
            row.buy = new Button { focusable = false };
            row.buy.AddToClassList("ubuy");
            row.buy.AddToClassList("ui-blocking");
            row.buy.usageHints = UsageHints.DynamicTransform;
            row.price = new Label("") { pickingMode = PickingMode.Ignore };
            row.price.AddToClassList("ubuy__label");
            row.buy.Add(row.price);
            row.buy.clicked += () => OnBuy(row);
            r.Add(row.buy);
            _rows.Add(row);
            return r;
        }

        void BindItem(VisualElement root, ShopItem item, string name)
        {
            var c = new ItemCard { item = item, card = root.Q<VisualElement>(name) };
            c.buy = root.Q<Button>(name + "-buy");
            c.price = root.Q<Label>(name + "-price");
            c.text = root.Q<Label>(name + "-text");
            if (c.card == null || c.buy == null || c.price == null) { Debug.LogWarning("[ShopView] item card '" + name + "' missing in CarpetHud.uxml"); return; }
            c.blurb = c.text != null ? c.text.text : "";   // the English UXML text = its Loc key (named labels are never re-texted by HudLocalizer)
            c.card.usageHints = UsageHints.DynamicTransform;
            c.buy.focusable = false;
            c.buy.usageHints = UsageHints.DynamicTransform;
            c.buy.clicked += () => OnBuyItem(c);
            _items.Add(c);
        }

        static VisualElement Ve(string cls)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(cls);
            return e;
        }

        // ------------------------------------------------------------------ state
        public void SetOpen(bool open, float now)
        {
            if (_open == open || !Valid) return;
            _open = open;
            _openT = now;
            _layer.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open) { Refresh(); _panel.schedule.Execute(EqualizeCaptions); }
            _audio?.Play(open ? UiSfx.Open : UiSfx.Close, 1f);
        }

        public void Refresh()
        {
            if (_eco == null) return;
            foreach (var row in _rows)
            {
                int l = _eco.Level(row.tool, row.stat);
                // pip k = level k; the first one (the tool as bought) is always lit, so level 3 reads "4 of 7"
                for (int k = 0; k < row.pips.Length; k++) row.pips[k].EnableInClassList("is-on", k <= l);
                float g = _eco.GainOverStart(row.stat, l);
                bool max = _eco.IsMax(row.tool, row.stat);
                row.gain.text = max ? Times(g) + "  " + Loc.T("MAX") : Times(g) + "  →  " + Times(_eco.GainOverStart(row.stat, l + 1));
                int price = _eco.Price(row.tool, row.stat);
                row.price.text = max ? Loc.T("MAX") : PayoutResult.Money(price);
                row.buy.EnableInClassList("is-max", max);
                row.buy.EnableInClassList("is-poor", !max && _eco.Money < price);
            }
            foreach (var c in _items)
            {
                bool owned = _eco.Owns(c.item);
                int price = _eco.ItemPrice(c.item);
                c.price.text = owned ? Loc.T("OWNED") : PayoutResult.Money(price);
                c.buy.EnableInClassList("is-owned", owned);
                c.buy.EnableInClassList("is-poor", !owned && _eco.Money < price);
                c.card.EnableInClassList("is-owned", owned);
                if (c.text != null) c.text.text = owned ? Loc.F("Owned: press {0} to switch it on and off", ShopItems.Key(c.item)) : Loc.T(c.blurb);
            }
        }

        // as a multiple of the player's start: "x1.44 -> x1.73", AREA ends at "x3.00"
        static string Times(float g) { return "×" + (1f + g).ToString("0.00", C); }

        void OnBuy(Row row)
        {
            if (_eco == null || !_open) return;
            float now = _fx != null ? _fx.Now : 0f;
            if (_eco.IsMax(row.tool, row.stat)) { row.shakeT = now; _audio?.Play(UiSfx.Deny, 0.7f); return; }
            if (!_eco.TryBuy(row.tool, row.stat)) { row.shakeT = now; _audio?.Play(UiSfx.Deny, 1f); return; }
            int l = _eco.Level(row.tool, row.stat);
            row.popT = now;
            row.popPip = Mathf.Clamp(l, 0, row.pips.Length - 1);
            _audio?.Play(UiSfx.Buy, 1f, 1f + 0.04f * l);
            if (_fx != null)
            {
                var at = _fx.CenterOf(row.pips[row.popPip]);
                string tint = row.stat == UpgradeStat.Power ? "fx--gold" : "fx--mint";
                _fx.Burst(FxKind.Sparkle, tint, at, 12, now, 0xB0u + (uint)row.tool * 31u + (uint)l, 120f, 360f, 0.7f, 0.45f, 0.95f, 240f, 3f);
                _fx.Pulse(FxKind.Ring, tint, at, now, 0.45f, 0.2f, 1.6f);
                _fx.Pulse(FxKind.Glow, tint, at, now, 0.4f, 0.4f, 1.6f);
            }
            Refresh();
        }

        void OnBuyItem(ItemCard c)
        {
            if (_eco == null || !_open) return;
            float now = _fx != null ? _fx.Now : 0f;
            if (_eco.Owns(c.item)) { c.shakeT = now; _audio?.Play(UiSfx.Deny, 0.7f); return; }
            if (!_eco.TryBuyItem(c.item)) { c.shakeT = now; _audio?.Play(UiSfx.Deny, 1f); return; }
            c.popT = now;
            _audio?.Play(UiSfx.Buy, 1f, c.item == ShopItem.DirtHighlight ? 1.12f : 1.04f);
            if (_fx != null)
            {
                var at = _fx.CenterOf(c.buy);
                string tint = c.item == ShopItem.DirtHighlight ? "fx--amber" : "fx--sky";
                _fx.Burst(FxKind.Sparkle, tint, at, 16, now, 0xE1u + (uint)c.item * 97u, 140f, 420f, 0.75f, 0.45f, 1.0f, 240f, 3f);
                _fx.Pulse(FxKind.Ring, tint, at, now, 0.5f, 0.3f, 2.2f);
                _fx.Pulse(FxKind.Glow, tint, at, now, 0.45f, 0.5f, 2.0f);
            }
            Refresh();
        }

        /// <summary>Per frame while open: panel entrance, pip pops, deny shakes.</summary>
        public void Tick(float now)
        {
            if (!_open || !Valid) return;
            float a = UiEase.Window(now, _openT, 0.22f);
            float s = Mathf.LerpUnclamped(0.94f, 1f, UiEase.OutBack(a, 2f));
            _panel.style.scale = new Scale(new Vector2(s, s));
            _panel.style.opacity = UiEase.Clamp01(a * 1.8f);
            foreach (var row in _rows)
            {
                float pt = (now - row.popT) / 0.4f;
                if (row.popPip >= 0 && pt >= 0f && pt <= 1.05f)
                {
                    float p = UiEase.Punch(pt, 0.9f);
                    row.pips[row.popPip].style.scale = new Scale(new Vector2(p, p));
                    if (pt > 1f) row.popPip = -1;
                }
                float st = (now - row.shakeT) / 0.35f;
                float dx = st >= 0f && st < 1f ? Mathf.Sin(st * 40f) * 7f * (1f - st) : 0f;
                row.buy.style.translate = new Translate(new Length(dx, LengthUnit.Pixel), new Length(0f, LengthUnit.Pixel));
            }
            foreach (var c in _items)
            {
                // a bought card swells once (a softer punch than a pip: it is a whole card)
                float p = 1f + (UiEase.Punch((now - c.popT) / 0.45f, 0.9f) - 1f) * 0.06f;
                c.card.style.scale = new Scale(new Vector2(p, p));
                float st = (now - c.shakeT) / 0.35f;
                float dx = st >= 0f && st < 1f ? Mathf.Sin(st * 40f) * 7f * (1f - st) : 0f;
                c.buy.style.translate = new Translate(new Length(dx, LengthUnit.Pixel), new Length(0f, LengthUnit.Pixel));
            }
        }

        public string Json()
        {
            var sb = new System.Text.StringBuilder(256);
            sb.Append("{\"open\":").Append(_open ? "true" : "false").Append(",\"rows\":").Append(_rows.Count.ToString(C));
            sb.Append(",\"items\":[");
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"tool\":").Append(((int)r.tool).ToString(C)).Append(",\"stat\":\"").Append(r.stat).Append("\",\"price\":\"").Append(r.price.text)
                  .Append("\",\"gain\":\"").Append(r.gain.text).Append("\",\"poor\":").Append(r.buy.ClassListContains("is-poor") ? "true" : "false").Append('}');
            }
            sb.Append("],\"oneTime\":[");
            for (int i = 0; i < _items.Count; i++)
            {
                var c = _items[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"item\":\"").Append(c.item).Append("\",\"price\":\"").Append(c.price.text)
                  .Append("\",\"owned\":").Append(c.buy.ClassListContains("is-owned") ? "true" : "false")
                  .Append(",\"poor\":").Append(c.buy.ClassListContains("is-poor") ? "true" : "false").Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }
    }
}
