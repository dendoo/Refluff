using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CarpetCleaning
{
    /// <summary>
    /// The economy's side of the HUD (Docs/ECONOMY.md 4-5, Docs/HUD.md 7): the wallet and the carpet clock in the meters
    /// card, the workbench's upgrade shop (ShopView), the pay-slip card (PayoutView) and the particle layer
    /// (UiFx). Lives next to CleaningHUD on CarpetUI and shares its UIDocument; CleaningHUD is not touched.
    ///
    /// Everything it animates runs on one unscaled clock (_now) that a debug seek can freeze, because frames do not tick
    /// while the Editor is unfocused. Hidden states are display:none, never opacity or picking-mode: CleaningHUD turns
    /// picking back on for every .ui-blocking element after each stroke.
    /// While the shop is open the simulation pauses (CarpetSimulation.UiPaused) and the tools stop (ToolController.UiModal);
    /// both are flags of their own, so the Steam overlay's pause never fights them.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(230)]
    public sealed class EconomyHud : MonoBehaviour
    {
        public Economy economy;
        public CarpetSimulation sim;
        public ToolController tools;
        public CarpetHandover handover;
        public UiAudio uiAudio;

        static readonly CultureInfo C = CultureInfo.InvariantCulture;

        UIDocument _doc;
        PauseMenu _menu;
        VisualElement _root, _wallet, _walletCoin, _badge, _shopWallet;
        Label _walletValue, _shopWalletValue, _clockValue, _clockPar;
        Button _shopBtn;
        UiFx _fx;
        PayoutView _payout;
        ShopView _shop;
        bool _built, _subscribed;
        Economy _subEco;
        CarpetHandover _subHo;

        float _now;
        bool _frozen;
        int _walletShown = -1;
        int _walletFrom, _walletTo;
        float _walletT0 = -10f;
        float _walletBumpT = -10f;
        string _clockText, _parText;
        bool _late;

        public bool ShopOpen { get { return _shop != null && _shop.IsOpen; } }
        public PayoutView Payout { get { return _payout; } }

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            if (economy == null) economy = UnityEngine.Object.FindAnyObjectByType<Economy>();
            if (sim == null) sim = UnityEngine.Object.FindAnyObjectByType<CarpetSimulation>();
            if (tools == null) tools = UnityEngine.Object.FindAnyObjectByType<ToolController>();
            if (handover == null) handover = UnityEngine.Object.FindAnyObjectByType<CarpetHandover>();
            if (uiAudio == null) uiAudio = GetComponent<UiAudio>();
            _menu = GetComponent<PauseMenu>();
        }

        void OnEnable() { Subscribe(); Loc.Changed += Relocalize; }

        void OnDisable()
        {
            Unsubscribe();
            Loc.Changed -= Relocalize;
            if (ShopOpen) ApplyModal(false);
        }

        /// <summary>Loc.Changed: the shop's tool cards and the pay slip in the new language (the clock re-texts every frame).</summary>
        void Relocalize()
        {
            if (!_built) return;
            _shop?.Relocalize();
            _payout?.Relocalize();
        }

        void Start() { Build(); }

        void Subscribe()
        {
            if (_subscribed) return;
            if (economy != null)
            {
                economy.PayoutReady += OnPayout;
                economy.MoneyChanged += OnMoney;
                economy.Upgraded += OnUpgraded;
                economy.ItemChanged += OnItemChanged;
                economy.JobRestarted += OnJobRestarted;
            }
            if (handover != null) handover.StateChanged += OnHandoverState;
            _subEco = economy; _subHo = handover; _subscribed = true;
        }

        void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_subEco != null)
            {
                _subEco.PayoutReady -= OnPayout;
                _subEco.MoneyChanged -= OnMoney;
                _subEco.Upgraded -= OnUpgraded;
                _subEco.ItemChanged -= OnItemChanged;
                _subEco.JobRestarted -= OnJobRestarted;
            }
            if (_subHo != null) _subHo.StateChanged -= OnHandoverState;
            _subEco = null; _subHo = null; _subscribed = false;
        }

        // ------------------------------------------------------------------ construction
        void Build()
        {
            if (_built) return;
            if (_doc == null) _doc = GetComponent<UIDocument>();
            _root = _doc != null ? _doc.rootVisualElement : null;
            if (_root == null || _root.Q<VisualElement>("fx") == null) return;   // UIDocument not ready yet: Update retries

            _wallet = _root.Q<VisualElement>("wallet");
            _walletCoin = _root.Q<VisualElement>("wallet-coin");
            _walletValue = _root.Q<Label>("wallet-value");
            _clockValue = _root.Q<Label>("clock-value");
            _clockPar = _root.Q<Label>("clock-par");
            _shopBtn = _root.Q<Button>("shop-button");
            _badge = _root.Q<VisualElement>("shop-badge");
            _shopWallet = _root.Q<VisualElement>("shop-wallet");
            _shopWalletValue = _root.Q<Label>("shop-wallet-value");

            _fx = new UiFx(_root.Q<VisualElement>("fx"));
            _payout = new PayoutView(_root, _fx, uiAudio, () => _wallet != null && _wallet.panel != null ? (_walletCoin ?? _wallet) : null, () => _walletBumpT = _now);
            _shop = new ShopView(_root, economy, _fx, uiAudio);
            _shop.CloseRequested = () => SetShopOpen(false);

            if (_shopBtn != null) { _shopBtn.focusable = false; _shopBtn.clicked += () => SetShopOpen(!ShopOpen); }
            var body = _root.Q<VisualElement>("payout-body");
            if (body != null) body.RegisterCallback<ClickEvent>(_ => { if (_payout != null) _payout.Skip(); });
            // Enter is the UI Submit key and re-clicks the focused button: nothing in the HUD may keep focus
            _root.Query<Button>().ForEach(b => b.focusable = false);

            _walletShown = economy != null ? economy.Money : 0;
            SetWalletText(_walletShown);
            _built = true;
        }

        // ------------------------------------------------------------------ events
        void OnPayout(PayoutResult r)
        {
            if (!_built || economy == null) return;
            if (ShopOpen) SetShopOpen(false);
            _walletShown = r.moneyBefore;
            SetWalletText(_walletShown);
            _payout.Frozen = false;
            _payout.Play(r, economy.settings);
        }

        void OnMoney(int balance, int delta) { if (ShopOpen) _shop.Refresh(); }
        void OnUpgraded(ToolId t, UpgradeStat s, int level) { if (ShopOpen) _shop.Refresh(); }
        void OnItemChanged(ShopItem item, bool owned) { if (ShopOpen) _shop.Refresh(); }
        void OnJobRestarted() { if (_payout != null) _payout.Finish(); }

        void OnHandoverState(HandoverState s)
        {
            // a new carpet lands where the card is: never let them overlap
            if (s == HandoverState.FlyIn && _payout != null) _payout.Finish();
        }

        public void SetShopOpen(bool open)
        {
            if (!_built || _shop == null) return;
            if (open == ShopOpen) return;
            if (open && ((_menu != null && _menu.IsOpen) || (tools != null && tools.Suspended))) return;
            if (open && _payout != null) { _payout.Finish(); _fx.Clear(); }   // no leftover confetti over the cards
            _shop.SetOpen(open, _now);
            ApplyModal(open);
        }

        /// <summary>Gameplay entrance. The unrestricted setter remains available to the existing debug recipes.</summary>
        public bool OpenFromWorkbench(UpgradeWorkbench workbench)
        {
            if (workbench == null || !workbench.CanInteract) return false;
            SetShopOpen(true);
            return ShopOpen;
        }

        void ApplyModal(bool on)
        {
            if (sim != null) sim.UiPaused = on;
            if (tools != null) tools.UiModal = on;
        }

        // ------------------------------------------------------------------ per frame
        void Update()
        {
            if (!_built) { Build(); if (!_built) return; }
            float dt = Time.unscaledDeltaTime;
            if (!_frozen) _now += dt;
            _fx.Now = _now;

            HandleKeys();
            if (!_frozen) _payout.Tick(dt);
            _shop.Tick(_now);
            UpdateWallet();
            UpdateClock();
            if (_badge != null)
            {
                bool any = economy != null && economy.AnyAffordable();
                var want = any ? DisplayStyle.Flex : DisplayStyle.None;
                if (_badge.style.display != want) _badge.style.display = want;
            }
            _fx.Update();
        }

        void HandleKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (tools != null && tools.Suspended) return;   // Steam overlay up: its text fields must not open the shop
            if (_menu != null && _menu.IsOpen) return;       // the pause menu owns Esc while it is up (it runs first, 228)
            if (kb.uKey.wasPressedThisFrame && (tools == null || !tools.FirstPersonActive)) SetShopOpen(!ShopOpen);
            else if (kb.escapeKey.wasPressedThisFrame && ShopOpen) SetShopOpen(false);
            if (kb.spaceKey.wasPressedThisFrame && _payout.Active) _payout.Skip();
        }

        void UpdateWallet()
        {
            if (economy == null) return;
            int target = _payout.Active ? _payout.WalletValue(economy.Money) : economy.Money;
            if (_walletShown < 0) { _walletShown = target; SetWalletText(_walletShown); }
            if (_payout.Active)
            {
                // the coins drive it: every landing adds its share and bumps the wallet
                if (target != _walletShown) { _walletShown = target; SetWalletText(_walletShown); }
                _walletTo = _walletFrom = _walletShown;
            }
            else if (target != _walletTo)
            {
                // a purchase or a debug grant: roll the digits over 0.4 s instead of snapping
                _walletFrom = _walletShown; _walletTo = target; _walletT0 = _now;
            }
            if (!_payout.Active && _walletShown != _walletTo)
            {
                float u = UiEase.Window(_now, _walletT0, 0.4f);
                // in double: a float holds whole dollars only up to 2^24, and near int.MaxValue (test money) it rounds up to
                // 2^31, which RoundToInt turns into int.MinValue ("$-2,147,483,648" on the wallet)
                _walletShown = u >= 1f ? _walletTo : (int)System.Math.Round(_walletFrom + ((double)_walletTo - _walletFrom) * UiEase.OutCubic(u));
                SetWalletText(_walletShown);
            }
            float b = UiEase.Punch((_now - _walletBumpT) / 0.3f, 0.22f);
            if (_wallet != null) _wallet.style.scale = new Scale(new Vector2(b, b));
        }

        void SetWalletText(int v)
        {
            string s = PayoutResult.Money(v);
            if (_walletValue != null && _walletValue.text != s) _walletValue.text = s;
            if (_shopWalletValue != null && _shopWalletValue.text != s) _shopWalletValue.text = s;
        }

        void UpdateClock()
        {
            if (economy == null || _clockValue == null) return;
            bool away = handover != null && handover.State != HandoverState.Idle && !economy.ClockRunning;
            float sec = economy.CarpetSeconds, par = economy.ParSeconds;
            string c = PayoutResult.Clock(sec);
            string p = economy.JobValue <= 0.001f ? Loc.T("spotless") : Loc.F("par {0}", PayoutResult.Clock(par));
            if (away && handover.State != HandoverState.FlyOut) { c = "—"; p = ""; }
            if (c != _clockText) { _clockText = c; _clockValue.text = c; }
            if (_clockPar != null && p != _parText) { _parText = p; _clockPar.text = p; }
            bool late = economy.ClockRunning && economy.JobValue > 0.001f && sec > par;
            if (late != _late) { _late = late; _clockValue.EnableInClassList("is-late", late); }
        }

        // ------------------------------------------------------------------ debug (EconomyDebugApi)
        public void DebugSeekPayout(float t)
        {
            if (!_built) Build();
            if (_payout == null) return;
            _frozen = true;
            _fx.Now = _now;
            _payout.Seek(t);
            UpdateWallet();
            _fx.Update();
        }

        public void DebugResumePayout() { _frozen = false; if (_payout != null) _payout.Frozen = false; }

        public string DebugPayoutJson()
        {
            if (_payout == null) return "{\"error\":\"not built\"}";
            return _payout.Json().TrimEnd('}') + ",\"wallet\":\"" + (_walletValue != null ? _walletValue.text : "") + "\",\"fxAlive\":" + _fx.Alive.ToString(C) + "}";
        }

        public string DebugShopJson() { return _shop != null ? _shop.Json() : "{\"error\":\"not built\"}"; }

        /// <summary>
        /// PNG of the HUD over the scene: the panel captured with VisualElementCaptureExtensions (works while the Editor is
        /// unfocused, when the Game view never repaints) composited over a Camera.Render of the same size.
        /// </summary>
        public string DebugCapture(string pngPath, int width, int height)
        {
            if (!_built || _root == null || _root.panel == null) return "{\"error\":\"HUD not built\"}";
            if (width <= 0 || height <= 0) { width = Screen.width; height = Screen.height; }
            var ui = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            // URP derives its intermediate format from the camera target. Render HDR first so ACES and
            // bloom see emissive values above 1, then reuse the store capture's sRGB encoding pass.
            var hdr = StoreShotApi.NewCaptureTarget(width, height, true);
            var scene = StoreShotApi.NewCaptureTarget(width, height, false);
            ui.Create();
            string result;
            try
            {
                var target = _root.panel.visualTree;
                bool ok = VisualElementCaptureExtensions.TryCaptureIntoRenderTexture(target, ui);
                var cam = Camera.main;
                if (cam != null)
                {
                    StoreShotApi.RenderHdr(cam, hdr, scene);
                }
                var a = Read(ui, width, height);
                var b = Read(scene, width, height);
                var outPx = new Color32[a.Length];
                for (int i = 0; i < a.Length; i++)
                {
                    // the panel renders premultiplied: over = ui + (1 - ui.a) * scene
                    float ia = 1f - a[i].a / 255f;
                    outPx[i] = new Color32((byte)Mathf.Min(255, a[i].r + b[i].r * ia), (byte)Mathf.Min(255, a[i].g + b[i].g * ia), (byte)Mathf.Min(255, a[i].b + b[i].b * ia), 255);
                }
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.SetPixels32(outPx);
                tex.Apply();
                string full = Path.IsPathRooted(pngPath) ? pngPath : Path.Combine(Application.dataPath, pngPath);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, tex.EncodeToPNG());
                var only = new Texture2D(width, height, TextureFormat.RGBA32, false);
                only.SetPixels32(a); only.Apply();
                File.WriteAllBytes(Path.ChangeExtension(full, null) + "_ui.png", only.EncodeToPNG());
                Destroy(tex); Destroy(only);
                int covered = 0; for (int i = 0; i < a.Length; i++) if (a[i].a > 8) covered++;
                result = "{\"ok\":" + (ok ? "true" : "false") + ",\"uiCoverage\":" + (covered / (float)a.Length).ToString("0.####", C) + ",\"path\":\"" + full.Replace("\\", "/") + "\"}";
            }
            finally
            {
                ui.Release(); Destroy(ui);
                StoreShotApi.FreeCaptureTarget(hdr); StoreShotApi.FreeCaptureTarget(scene);
            }
            return result;
        }

        static Color32[] Read(RenderTexture rt, int w, int h)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            t.Apply();
            RenderTexture.active = prev;
            var px = t.GetPixels32();
            Destroy(t);
            return px;
        }
    }
}
