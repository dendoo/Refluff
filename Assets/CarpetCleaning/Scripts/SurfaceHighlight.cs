using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CarpetCleaning
{
    /// <summary>
    /// The inspection lamp (Docs/HIGHLIGHTS.md): two one-time shop items, toggled with Z (moisture) and X (dirt). While one is
    /// on, the carpet, the floor, the water, the foam and the crumbs dim and desaturate, and what still costs the pay slip
    /// glows: soil amber (the DirtScore weights), water blue (w + h / W_CAP, the Wetness measure). Both may be on at once.
    ///
    /// This component owns only the state, the keys and two shader globals; the look is Shaders/HighlightCommon.hlsl.
    ///   _Highlight     = (dirt amount 0..1, moisture amount 0..1, dirt sweep 0..1, moisture sweep 0..1)
    ///   _HighlightTune = (dirtRef, wetRef, dim, glow), pushed every frame so the look can be tuned live in the inspector.
    /// Switching on fades in over fadeIn and runs a scan ring out from the carpet centre (the sweep); switching off fades.
    /// Nothing here touches the simulation or the pay: the lamp only shows the state the sims already hold.
    /// </summary>
    [DefaultExecutionOrder(225)]
    public sealed class SurfaceHighlight : MonoBehaviour
    {
        public Economy economy;
        public ToolController tools;
        public UiAudio uiAudio;
        public CleaningHUD hud;

        [Header("Look (pushed as _HighlightTune every frame)")]
        [Tooltip("Carpet DirtScore of a cell (1 crust + 0.8 mud + 0.6 stain + 0.9 grit + 0.7 slurry + 0.5 foam dirt) that glows at about a third of full strength; the response is logarithmic, so traces far below it still show.")]
        public float dirtRef = 0.02f;
        [Tooltip("Cell wetness (w + h / W_CAP) that glows at about a third of full strength (logarithmic, like dirtRef).")]
        public float wetRef = 0.02f;
        [Tooltip("How far the lit surface is darkened while a highlight is on (0 = not at all, 1 = black).")]
        [Range(0f, 1f)] public float dim = 0.62f;
        [Tooltip("Glow brightness multiplier (the ramps peak a little above the 1.2 bloom threshold at 1).")]
        public float glow = 1f;

        [Header("Timing (unscaled seconds)")]
        public float fadeIn = 0.25f;
        public float fadeOut = 0.2f;
        [Tooltip("The scan ring runs from the carpet centre to HighlightCommon.hlsl HL_SWEEP_MM (2700 mm) in this time.")]
        public float sweepSeconds = 0.55f;

        public static readonly int HighlightId = Shader.PropertyToID("_Highlight");
        public static readonly int TuneId = Shader.PropertyToID("_HighlightTune");

        /// <summary>Raised on every switch (on, off, forced off because the item is no longer owned).</summary>
        public event Action Changed;

        bool _dirtOn, _wetOn;
        float _dirtAmt, _wetAmt, _dirtSweep = 1f, _wetSweep = 1f;
        Economy _subEco;

        public bool DirtOn { get { return _dirtOn; } }
        public bool MoistureOn { get { return _wetOn; } }
        public bool IsOn(ShopItem item) { return item == ShopItem.DirtHighlight ? _dirtOn : _wetOn; }
        public float Amount(ShopItem item) { return item == ShopItem.DirtHighlight ? _dirtAmt : _wetAmt; }
        public float Sweep(ShopItem item) { return item == ShopItem.DirtHighlight ? _dirtSweep : _wetSweep; }

        /// <summary>Owned in the save. A scene without an Economy (a bare test scene) lends both.</summary>
        public bool Owned(ShopItem item) { return economy == null || economy.Owns(item); }

        void Awake()
        {
            if (economy == null) economy = UnityEngine.Object.FindAnyObjectByType<Economy>();
            if (tools == null) tools = UnityEngine.Object.FindAnyObjectByType<ToolController>();
            if (uiAudio == null) uiAudio = GetComponent<UiAudio>();
            if (hud == null) hud = GetComponent<CleaningHUD>();
            // scene reload is off (CLAUDE.md): every Play starts dark, whatever the last session left
            _dirtOn = _wetOn = false;
            _dirtAmt = _wetAmt = 0f;
            _dirtSweep = _wetSweep = 1f;
        }

        void OnEnable()
        {
            if (economy != null) { economy.ItemChanged += OnItemChanged; _subEco = economy; }
        }

        void OnDisable()
        {
            if (_subEco != null) { _subEco.ItemChanged -= OnItemChanged; _subEco = null; }
            // shader globals outlive Play mode: without this the Scene view keeps the lamp after Stop
            Shader.SetGlobalVector(HighlightId, Vector4.zero);
        }

        void OnItemChanged(ShopItem item, bool owned)
        {
            if (!owned && IsOn(item)) Set(item, false, true);
            else Changed?.Invoke();   // the HUD chips show once an item is owned
        }

        // ------------------------------------------------------------------ input
        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            // the shop is modal and the Steam overlay owns the keyboard (same gate as H / N in CarpetHandover); the carpet flight is not
            if (tools != null && (tools.Suspended || tools.UiModal)) return;
            if (kb.zKey.wasPressedThisFrame) Toggle(ShopItem.MoistureHighlight);
            if (kb.xKey.wasPressedThisFrame) Toggle(ShopItem.DirtHighlight);
        }

        /// <summary>What the key does: switch an owned item, or refuse with a pointer to the shop. Returns the new state.</summary>
        public bool Toggle(ShopItem item)
        {
            if (!Owned(item))
            {
                if (uiAudio != null) uiAudio.Play(UiSfx.Deny, 0.8f, 1f, 0.15f);
                if (hud != null) hud.Toast(tools != null && tools.FirstPersonActive
                    ? Loc.F("{0}: buy it at the upgrade table", Loc.T(ShopItems.Name(item)))
                    : Loc.F("{0}: buy it in Upgrades ({1})", Loc.T(ShopItems.Name(item)), KeyNames.Label(UnityEngine.InputSystem.Key.U)), 2.2f);
                return false;
            }
            bool on = !IsOn(item);
            Set(item, on, false);
            // on = the shop's rising sweep, off = its falling one; the two lamps differ in pitch so they are told apart blind
            float pitch = item == ShopItem.DirtHighlight ? 1.12f : 0.94f;
            if (uiAudio != null) uiAudio.Play(on ? UiSfx.Open : UiSfx.Close, 0.8f, pitch);
            return on;
        }

        /// <summary>Switches a lamp. instant = no fade and no sweep (captures, tests); it also pushes the globals at once.</summary>
        public void Set(ShopItem item, bool on, bool instant)
        {
            bool was = IsOn(item);
            if (item == ShopItem.DirtHighlight) _dirtOn = on; else _wetOn = on;
            if (instant)
            {
                if (item == ShopItem.DirtHighlight) { _dirtAmt = on ? 1f : 0f; _dirtSweep = 1f; }
                else { _wetAmt = on ? 1f : 0f; _wetSweep = 1f; }
                Push();
            }
            else if (on && !was)
            {
                // a new scan from the centre only when the lamp was (nearly) dark; flicking it back on mid-fade keeps the view
                if (item == ShopItem.DirtHighlight) { if (_dirtAmt < 0.05f) _dirtSweep = 0f; }
                else if (_wetAmt < 0.05f) _wetSweep = 0f;
            }
            if (was != on || instant) Changed?.Invoke();
        }

        // ------------------------------------------------------------------ animation + globals
        void LateUpdate()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            Step(ref _dirtAmt, ref _dirtSweep, _dirtOn, dt);
            Step(ref _wetAmt, ref _wetSweep, _wetOn, dt);
            Push();
        }

        void Step(ref float amt, ref float sweep, bool on, float dt)
        {
            if (on) amt = Mathf.Min(1f, amt + dt / Mathf.Max(fadeIn, 1e-3f));
            else amt = Mathf.Max(0f, amt - dt / Mathf.Max(fadeOut, 1e-3f));
            if (on && sweep < 1f) sweep = Mathf.Min(1f, sweep + dt / Mathf.Max(sweepSeconds, 1e-3f));
        }

        void Push()
        {
            // eased amounts: the lamp settles instead of stopping dead
            float d = Ease(_dirtAmt), w = Ease(_wetAmt);
            Shader.SetGlobalVector(HighlightId, d > 0f || w > 0f ? new Vector4(d, w, _dirtSweep, _wetSweep) : Vector4.zero);
            Shader.SetGlobalVector(TuneId, new Vector4(Mathf.Max(dirtRef, 1e-4f), Mathf.Max(wetRef, 1e-4f), Mathf.Clamp01(dim), Mathf.Max(glow, 0f)));
        }

        static float Ease(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    }
}
