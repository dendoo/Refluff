using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace CarpetCleaning
{
    /// <summary>
    /// The pay slip of a handed-over carpet, played line by line (Docs/ECONOMY.md 4.1). A TIMELINE in seconds, rebuilt
    /// from the slip: every visual (card drop, line slide-ins, count-ups, pops, the COMBO stamp and shake, the total, the
    /// coins flying into the wallet) is a pure function of t, and every sound and particle burst is an event at a fixed
    /// t. Playing = advancing t by unscaled dt and firing the events crossed; a debug seek clears the particles and re-fires
    /// every event before t without sound, which rebuilds the exact frame (UiFx is analytic). Only transform / opacity /
    /// text are written; colours and sizes live in CarpetHud.uss.
    /// Hidden with display:none (never opacity or picking): CleaningHUD re-enables picking on every .ui-blocking element
    /// after each stroke, so a transparent card would still swallow clicks in the middle of the screen.
    /// Title and details go through Loc (Docs/LOCALIZATION.md); a language switch while the card is up re-texts them.
    /// </summary>
    public sealed class PayoutView
    {
        const int LineCount = 5;
        static readonly CultureInfo C = CultureInfo.InvariantCulture;

        sealed class Line
        {
            public VisualElement root, shine, icon;
            public Label amount, detail;
            public float start, countStart, pop;
            public int value;
            public bool achieved;
            public string tint;
            public int lastTickStep;
        }

        struct Ev { public float t; public int kind; public int arg; }
        const int EvCardIn = 0, EvLineIn = 1, EvLinePop = 2, EvComboLand = 3, EvTotalPop = 4, EvCoinLand = 5, EvDivider = 6;

        readonly VisualElement _layer, _card, _rays, _flash, _stamp, _divider;
        readonly Label _title, _number, _total;
        readonly Line[] _lines = new Line[LineCount];
        readonly UiFx _fx;
        readonly UiAudio _audio;
        readonly System.Func<VisualElement> _walletTarget;
        readonly System.Action _walletBump;

        EconomySettings _s;
        PayoutResult _r;
        bool _active;
        float _t, _end;
        float _cardT0, _dividerT, _totalStart, _totalPop, _coinsT, _lastLand, _exitT, _comboLand = -1f;
        readonly List<float> _coinLand = new List<float>(32);
        readonly List<Ev> _events = new List<Ev>(64);
        int _totalTickStep;

        public bool Active { get { return _active; } }
        public float Time01 { get { return _end > 0f ? Mathf.Clamp01(_t / _end) : 0f; } }
        public float T { get { return _t; } }
        public float End { get { return _end; } }
        public PayoutResult Slip { get { return _r; } }
        public bool Frozen;

        public PayoutView(VisualElement root, UiFx fx, UiAudio audio, System.Func<VisualElement> walletTarget, System.Action walletBump)
        {
            _layer = root.Q<VisualElement>("payout");
            _card = root.Q<VisualElement>("payout-card");
            _rays = root.Q<VisualElement>("payout-rays");
            _flash = root.Q<VisualElement>("payout-flash");
            _stamp = root.Q<VisualElement>("payout-stamp");
            _divider = root.Q<VisualElement>("payout-divider");
            _title = root.Q<Label>("payout-title");
            _number = root.Q<Label>("payout-number");
            _total = root.Q<Label>("payout-total");
            string[] ids = { "base", "speed", "clean", "dry", "combo" };
            string[] tints = { "fx--gold", "fx--gold", "fx--mint", "fx--amber", "fx--violet" };
            for (int i = 0; i < LineCount; i++)
            {
                var l = new Line { root = root.Q<VisualElement>("line-" + ids[i]), tint = tints[i] };
                if (l.root != null)
                {
                    l.shine = l.root.Q<VisualElement>(className: "pline__shine");
                    l.icon = l.root.Q<VisualElement>(className: "pline__icon");
                    l.amount = l.root.Q<Label>(className: "pline__amount");
                    l.detail = l.root.Q<Label>(className: "pline__detail");
                }
                _lines[i] = l;
            }
            _fx = fx;
            _audio = audio;
            _walletTarget = walletTarget;
            _walletBump = walletBump;
            // money and times read left to right in every language (bidi would reorder "+$120" in Arabic otherwise)
            if (_total != null) _total.languageDirection = LanguageDirection.LTR;
            foreach (var l in _lines) if (l.amount != null) l.amount.languageDirection = LanguageDirection.LTR;
            Hide();
        }

        public bool Valid { get { return _layer != null && _card != null && _total != null; } }

        // ------------------------------------------------------------------ control
        public void Play(PayoutResult r, EconomySettings s)
        {
            if (!Valid || s == null) return;
            _s = s;
            _r = r;
            BuildTimeline();
            Fill();
            _t = 0f;
            _active = true;
            _layer.style.display = DisplayStyle.Flex;
            Apply(_t);
        }

        /// <summary>First skip: jump to the total with the coins taking off now. Second skip (or a forced one): close.</summary>
        public void Skip(bool close = false)
        {
            if (!_active) return;
            if (!close && _t < _totalPop)
            {
                // everything before the total shows at once, quietly; the coins still fly so the wallet visibly fills
                Fire(_t, _totalPop, false);
                _t = _totalPop;
                if (_r.total > 0) _audio?.Play(UiSfx.KaChing, 1f);
                return;
            }
            if (!close && _t < _exitT) { _t = _exitT; return; }
            Finish();
        }

        /// <summary>Immediately done: card gone, wallet shows the final amount.</summary>
        public void Finish()
        {
            if (!_active) return;
            _active = false;
            Hide();
        }

        void Hide()
        {
            if (_layer != null) _layer.style.display = DisplayStyle.None;
        }

        /// <summary>Wallet value the HUD should show while the coins are landing (moneyBefore + landed share).</summary>
        public int WalletValue(int fallback)
        {
            if (!_active) return fallback;
            int n = _coinLand.Count;
            if (n == 0) return _t >= _totalPop ? _r.moneyAfter : _r.moneyBefore;
            int landed = 0;
            for (int i = 0; i < n; i++) if (_coinLand[i] <= _t) landed++;
            if (landed >= n) return _r.moneyAfter;
            return _r.moneyBefore + Mathf.FloorToInt((_r.moneyAfter - _r.moneyBefore) * landed / (float)n);
        }

        public void Tick(float dt)
        {
            if (!_active || Frozen) return;
            float t1 = _t + Mathf.Max(dt, 0f);
            Fire(_t, t1, true);
            _t = t1;
            Apply(_t);
            if (_t >= _end) Finish();
        }

        /// <summary>Debug: the exact frame at t, without sound (particles re-spawned analytically). Freezes the card there.</summary>
        public void Seek(float t)
        {
            if (!_active) return;
            Frozen = true;
            _fx.Clear();
            t = Mathf.Clamp(t, 0f, Mathf.Max(_end - 1e-3f, 0f));
            Fire(-1f, t, false);
            _t = t;
            Apply(_t);
        }

        // ------------------------------------------------------------------ timeline
        void BuildTimeline()
        {
            var s = _s;
            _events.Clear();
            _coinLand.Clear();
            _comboLand = -1f;
            _cardT0 = s.cardDelay;
            _events.Add(new Ev { t = _cardT0, kind = EvCardIn });
            float t = _cardT0 + s.firstLine;
            int[] values = { _r.basePay, _r.speedBonus, _r.cleanBonus, _r.dryBonus, _r.comboBonus };
            bool[] ach = { _r.basePay > 0, _r.speedBonus > 0, _r.perfectClean, _r.perfectDry, _r.combo };
            for (int i = 0; i < LineCount; i++)
            {
                var l = _lines[i];
                l.value = values[i];
                l.achieved = ach[i] && values[i] > 0;
                l.start = t;
                l.lastTickStep = 0;
                _events.Add(new Ev { t = t, kind = EvLineIn, arg = i });
                if (!l.achieved)
                {
                    l.countStart = l.pop = t;
                    t += s.missedLineGap;
                    continue;
                }
                if (i == 4)
                {
                    // COMBO: the stamp slams down, the card shakes, confetti; the amount is simply there when it lands
                    l.countStart = t;
                    l.pop = t + 0.26f;
                    _comboLand = l.pop;
                    _events.Add(new Ev { t = l.pop, kind = EvComboLand, arg = i });
                    t = l.pop + s.comboBeat;
                    continue;
                }
                l.countStart = t + 0.14f;
                l.pop = l.countStart + (i <= 1 ? s.countUp : s.countUp * 0.6f);
                _events.Add(new Ev { t = l.pop, kind = EvLinePop, arg = i });
                t += s.lineGap;
                if (l.pop + 0.1f > t) t = l.pop + 0.1f;
            }
            _dividerT = t;
            _events.Add(new Ev { t = _dividerT, kind = EvDivider });
            _totalStart = _dividerT + 0.18f;
            _totalPop = _totalStart + (_r.total > 0 ? s.totalCount : 0.05f);
            _events.Add(new Ev { t = _totalPop, kind = EvTotalPop });
            _coinsT = _totalPop + 0.04f;
            int coins = _r.total > 0 ? Mathf.Clamp(s.coins, 1, 40) : 0;
            uint seed = 0xC0135u;
            _lastLand = _totalPop;
            for (int i = 0; i < coins; i++)
            {
                float launch = _coinsT + i * 0.05f;
                float flight = s.coinFlight * Mathf.Lerp(0.88f, 1.15f, Hash(ref seed));
                float land = launch + flight;
                _coinLand.Add(land);
                _events.Add(new Ev { t = land, kind = EvCoinLand, arg = i });
                if (land > _lastLand) _lastLand = land;
            }
            _exitT = _lastLand + s.hold;
            _end = _exitT + s.cardOut;
            _events.Sort((a, b) => a.t.CompareTo(b.t));
            _totalTickStep = 0;
        }

        static float Hash(ref uint s) { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / 16777216f; }

        /// <summary>Loc.Changed: the card's words in the new language (amounts are numbers and stay).</summary>
        public void Relocalize()
        {
            if (_active && _r.valid) FillText();
        }

        void FillText()
        {
            var r = _r;
            if (_number != null) _number.text = Loc.F("#{0}", Mathf.Max(r.carpetNumber, 1).ToString(C));
            if (_title != null)
            {
                string soil = Loc.T(PayoutResult.SoilName(r.jobValue));
                _title.text = r.jobValue <= 0.001f ? soil : soil + "  ·  " + PayoutResult.Clock(r.seconds);
            }
            SetDetail(0, r.jobValue <= 0.001f ? Loc.T("nothing to clean") : Loc.F("{0}% job", Mathf.RoundToInt(r.jobValue * 100f).ToString(C)));
            SetDetail(1, Loc.F("{0} / par {1}", PayoutResult.Clock(r.seconds), PayoutResult.Clock(r.parSeconds)));
            SetDetail(2, Loc.F("{0}% clean", Mathf.RoundToInt(r.clean * 100f).ToString(C)));
            SetDetail(3, Loc.F("{0}% dry", Mathf.RoundToInt(r.dry * 100f).ToString(C)));
            SetDetail(4, r.combo ? Loc.T("spotless + bone dry") : Loc.T("100% clean + 100% dry"));
        }

        void Fill()
        {
            var r = _r;
            FillText();
            for (int i = 0; i < LineCount; i++)
            {
                var l = _lines[i];
                if (l.root == null) continue;
                l.root.EnableInClassList("pline--missed", !l.achieved);
                l.root.EnableInClassList("pline--won", l.achieved);
                if (l.amount != null) l.amount.text = l.achieved ? "+" + PayoutResult.Money(0) : "—";
            }
            if (_total != null) _total.text = PayoutResult.Money(0);
            if (_stamp != null) _stamp.style.display = r.combo ? DisplayStyle.Flex : DisplayStyle.None;
            if (_rays != null) _rays.style.display = r.combo ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void SetDetail(int i, string text) { var l = _lines[i]; if (l.detail != null) l.detail.text = text; }

        // ------------------------------------------------------------------ events
        void Fire(float from, float to, bool live)
        {
            for (int k = 0; k < _events.Count; k++)
            {
                var e = _events[k];
                if (e.t <= from || e.t > to) continue;
                On(e, to, live);
            }
            if (live) Ticks(from, to);
        }

        void On(Ev e, float now, bool live)
        {
            float t0 = _fx.Now - (now - e.t);    // backdate so a late or re-played event looks exactly like a live one
            switch (e.kind)
            {
                case EvCardIn:
                    if (live) _audio?.Play(UiSfx.Whoosh, 0.9f);
                    break;
                case EvLineIn:
                {
                    var l = _lines[e.arg];
                    if (live) { if (l.achieved) _audio?.Play(UiSfx.Whoosh, 0.45f, 1.15f); else _audio?.Play(UiSfx.Thud, 0.6f); }
                    break;
                }
                case EvLinePop:
                {
                    var l = _lines[e.arg];
                    Vector2 at = _fx.CenterOf(l.amount);
                    uint seed = 0xA11u + (uint)e.arg * 977u;
                    _fx.Pulse(FxKind.Glow, l.tint, at, t0, 0.35f, 0.5f, 1.5f);
                    _fx.Burst(FxKind.Sparkle, l.tint, at, e.arg >= 2 ? 20 : 14, t0, seed, 220f, 560f, 0.8f, 0.8f, 1.6f, 260f, 3f);
                    if (e.arg >= 2) _fx.Pulse(FxKind.Ring, l.tint, at, t0, 0.5f, 0.3f, 2.2f);
                    if (live)
                    {
                        _audio?.Play(UiSfx.Clink, 0.9f, 1f + 0.06f * e.arg);
                        if (e.arg >= 2) _audio?.Play(UiSfx.Sparkle, 0.8f);
                    }
                    break;
                }
                case EvComboLand:
                {
                    var l = _lines[e.arg];
                    Vector2 at = _fx.CenterOf(_stamp != null ? _stamp : l.amount);
                    string[] tints = { "fx--gold", "fx--mint", "fx--amber", "fx--violet", "fx--rose" };
                    for (int k = 0; k < tints.Length; k++)
                    {
                        _fx.Burst(FxKind.Confetti, tints[k], at + new Vector2(-60f, 0f), 9, t0, 0xC0F1u + (uint)k * 31u, 300f, 820f, 1.6f, 0.8f, 1.2f, 900f, 1.4f, -120f, 70f);
                        _fx.Burst(FxKind.Confetti, tints[k], at + new Vector2(60f, 0f), 9, t0, 0xC0F2u + (uint)k * 57u, 300f, 820f, 1.6f, 0.8f, 1.2f, 900f, 1.4f, -60f, 70f);
                    }
                    _fx.Pulse(FxKind.Ring, "fx--violet", at, t0, 0.6f, 0.4f, 4f);
                    _fx.Pulse(FxKind.Glow, "fx--gold", at, t0, 0.6f, 1f, 4.5f);
                    _fx.Burst(FxKind.Sparkle, "fx--gold", at, 18, t0, 0x5EEDu, 220f, 600f, 0.9f, 0.6f, 1.3f, 300f, 2.5f);
                    if (live) { _audio?.Play(UiSfx.Stamp, 1f); _audio?.Play(UiSfx.Sparkle, 0.9f, 1.1f); }
                    break;
                }
                case EvDivider:
                    break;
                case EvTotalPop:
                {
                    Vector2 at = _fx.CenterOf(_total);
                    if (_r.total > 0)
                    {
                        _fx.Pulse(FxKind.Ring, "fx--gold", at, t0, 0.55f, 0.4f, 3.6f);
                        _fx.Pulse(FxKind.Glow, "fx--gold", at, t0, 0.5f, 1f, 3.2f);
                        _fx.Burst(FxKind.Sparkle, "fx--gold", at, 20, t0, 0x70A1u, 200f, 560f, 0.85f, 0.6f, 1.2f, 280f, 2.6f);
                        var wallet = _walletTarget != null ? _walletTarget() : null;
                        Vector2 to = wallet != null ? _fx.CenterOf(wallet) : at + new Vector2(0f, -400f);
                        uint seed = 0xC0135u;
                        for (int i = 0; i < _coinLand.Count; i++)
                        {
                            float launch = _coinsT + i * 0.05f;
                            float flight = _coinLand[i] - launch;
                            _fx.CoinFly(at, to, _fx.Now - (now - launch), flight, seed + (uint)i * 7919u);
                        }
                        if (live) _audio?.Play(UiSfx.KaChing, 1f);
                    }
                    break;
                }
                case EvCoinLand:
                {
                    var wallet = _walletTarget != null ? _walletTarget() : null;
                    if (wallet != null && (e.arg % 3 == 0 || e.arg == _coinLand.Count - 1))
                        _fx.Burst(FxKind.Sparkle, "fx--gold", _fx.CenterOf(wallet), 3, t0, 0x3A1Bu + (uint)e.arg, 60f, 180f, 0.45f, 0.4f, 0.8f, 120f, 3f);
                    if (live)
                    {
                        _walletBump?.Invoke();
                        _audio?.Play(UiSfx.Clink, 0.35f, 1.1f + 0.03f * (e.arg % 5), 0.035f);
                    }
                    break;
                }
            }
        }

        /// <summary>Count-up ticks: one per step of the shown value, pitch climbing as the count runs.</summary>
        void Ticks(float from, float to)
        {
            for (int i = 0; i < LineCount; i++)
            {
                var l = _lines[i];
                if (!l.achieved || i == 4 || to < l.countStart || from > l.pop) continue;
                float u = UiEase.Window(to, l.countStart, l.pop - l.countStart);
                int steps = Mathf.Clamp(l.value / Mathf.Max(_s.roundTo, 1), 1, 10);
                int step = Mathf.FloorToInt(UiEase.OutCubic(u) * steps);
                if (step > l.lastTickStep && u < 1f)
                {
                    l.lastTickStep = step;
                    _audio?.Play(UiSfx.Tick, 0.8f, 1f + 0.45f * u + 0.05f * i, 0.03f);
                }
            }
            if (_r.total > 0 && to >= _totalStart && from <= _totalPop)
            {
                float u = UiEase.Window(to, _totalStart, _totalPop - _totalStart);
                int step = Mathf.FloorToInt(UiEase.InQuad(u) * 14f);
                if (step > _totalTickStep && u < 1f)
                {
                    _totalTickStep = step;
                    _audio?.Play(UiSfx.Tick, 0.9f, 1.1f + 0.8f * u, 0.025f);
                }
            }
        }

        // ------------------------------------------------------------------ visuals at t
        void Apply(float t)
        {
            // card: drops in with an overshoot, shakes when the COMBO lands, lifts away at the end
            float a = UiEase.Window(t, _cardT0, _s.cardIn);
            float y = Mathf.LerpUnclamped(-46f, 0f, UiEase.OutBack(a, 2.2f));
            float sc = Mathf.LerpUnclamped(0.9f, 1f, UiEase.OutBack(a, 2.2f));
            float op = UiEase.Clamp01(a * 2.2f);
            float x = 0f, rot = 0f;
            if (_comboLand > 0f && t >= _comboLand && t < _comboLand + 0.4f)
            {
                float q = 1f - (t - _comboLand) / 0.4f;
                x = Mathf.Sin(t * 80f) * 9f * q * q;
                y += Mathf.Cos(t * 63f) * 6f * q * q;
                rot = Mathf.Sin(t * 47f) * 1.2f * q * q;
            }
            float b = UiEase.Window(t, _exitT, _s.cardOut);
            if (b > 0f) { op *= 1f - b; y -= 20f * UiEase.InQuad(b); sc *= 1f - 0.05f * b; }
            _card.style.translate = new Translate(new Length(x, LengthUnit.Pixel), new Length(y, LengthUnit.Pixel));
            _card.style.scale = new Scale(new Vector2(sc, sc));
            _card.style.rotate = new Rotate(new Angle(rot, AngleUnit.Degree));
            _card.style.opacity = op;

            for (int i = 0; i < LineCount; i++) ApplyLine(_lines[i], i, t);

            // COMBO stamp: slams down from 2.6x, lands with a squash; rays turn behind the card
            if (_stamp != null && _r.combo)
            {
                float s0 = _lines[4].start;
                float q = UiEase.Window(t, s0, _comboLand - s0);
                float land = t - _comboLand;
                float scale = q < 1f ? Mathf.Lerp(2.6f, 1f, q * q) : UiEase.Punch(land / 0.3f, -0.12f);
                _stamp.style.opacity = t < s0 ? 0f : UiEase.Clamp01(q * 3f);
                _stamp.style.scale = new Scale(new Vector2(scale, scale));
                _stamp.style.rotate = new Rotate(new Angle(-8f, AngleUnit.Degree));
            }
            if (_rays != null && _r.combo)
            {
                float q = _comboLand > 0f ? UiEase.Window(t, _comboLand, 0.4f) : 0f;
                _rays.style.opacity = 0.9f * q;
                _rays.style.rotate = new Rotate(new Angle(t * 22f, AngleUnit.Degree));
                float pulse = 1f + 0.06f * Mathf.Sin(t * 4f);
                _rays.style.scale = new Scale(new Vector2(pulse, pulse) * Mathf.Lerp(0.6f, 1f, UiEase.OutBack(q)));
            }
            if (_flash != null)
            {
                float f = _comboLand > 0f && t >= _comboLand ? 1f - UiEase.Window(t, _comboLand, 0.22f) : 0f;
                _flash.style.opacity = 0.4f * f;
            }

            // divider draws itself, then the total counts up (accelerating) and punches
            if (_divider != null) _divider.style.scale = new Scale(new Vector2(UiEase.OutCubic(UiEase.Window(t, _dividerT, 0.3f)), 1f));
            float tu = UiEase.Window(t, _totalStart, _totalPop - _totalStart);
            int shown = t >= _totalPop ? _r.total : Mathf.RoundToInt(_r.total * UiEase.InQuad(tu));
            _total.text = PayoutResult.Money(shown);
            float tp = UiEase.Punch((t - _totalPop) / 0.45f, 0.4f);
            _total.style.scale = new Scale(new Vector2(tp, tp));
            _total.style.opacity = UiEase.Window(t, _dividerT, 0.2f);
        }

        void ApplyLine(Line l, int i, float t)
        {
            if (l.root == null) return;
            float a = UiEase.Window(t, l.start, 0.24f);
            l.root.style.opacity = l.achieved ? a : 0.55f * a;
            l.root.style.translate = new Translate(new Length(-28f * (1f - UiEase.OutCubic(a)), LengthUnit.Pixel), new Length(0f, LengthUnit.Pixel));
            if (l.amount == null) return;
            if (!l.achieved) { l.amount.style.scale = new Scale(Vector2.one); return; }

            if (i == 4) l.amount.text = "+" + PayoutResult.Money(t >= l.pop ? l.value : 0);
            else
            {
                float u = UiEase.Window(t, l.countStart, l.pop - l.countStart);
                int v = t >= l.pop ? l.value : Mathf.RoundToInt(l.value * UiEase.OutCubic(u));
                l.amount.text = "+" + PayoutResult.Money(v);
            }
            float p = UiEase.Punch((t - l.pop) / 0.35f, 0.34f);
            l.amount.style.scale = new Scale(new Vector2(p, p));
            if (l.icon != null)
            {
                float ip = UiEase.Punch((t - l.pop) / 0.4f, 0.28f);
                l.icon.style.scale = new Scale(new Vector2(ip, ip));
            }
            if (l.shine != null)
            {
                float s = UiEase.Window(t, l.pop, 0.5f);
                bool on = t >= l.pop && s < 1f;
                l.shine.style.opacity = on ? UiEase.Bell(s) : 0f;
                l.shine.style.translate = new Translate(Length.Percent(Mathf.Lerp(-60f, 460f, UiEase.InOutSine(s))), new Length(0f, LengthUnit.Pixel));
            }
        }

        // ------------------------------------------------------------------ debug
        public string Json()
        {
            var sb = new System.Text.StringBuilder(256);
            sb.Append("{\"active\":").Append(_active ? "true" : "false");
            sb.Append(",\"t\":").Append(_t.ToString("0.###", C)).Append(",\"end\":").Append(_end.ToString("0.###", C));
            sb.Append(",\"frozen\":").Append(Frozen ? "true" : "false");
            sb.Append(",\"totalPop\":").Append(_totalPop.ToString("0.###", C)).Append(",\"combo\":").Append(_comboLand.ToString("0.###", C));
            sb.Append(",\"exit\":").Append(_exitT.ToString("0.###", C));
            sb.Append(",\"total\":\"").Append(_total != null ? _total.text : "").Append('"');
            sb.Append(",\"lines\":[");
            for (int i = 0; i < LineCount; i++)
            {
                var l = _lines[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"start\":").Append(l.start.ToString("0.###", C)).Append(",\"pop\":").Append(l.pop.ToString("0.###", C));
                sb.Append(",\"won\":").Append(l.achieved ? "true" : "false").Append(",\"amount\":\"").Append(l.amount != null ? l.amount.text : "").Append("\"}");
            }
            sb.Append("],\"slip\":").Append(_r.valid ? _r.ToJson() : "null").Append('}');
            return sb.ToString();
        }
    }
}
