using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CarpetCleaning
{
    /// <summary>
    /// Mouse/keyboard (Input System) -> tool stamps. StrokeSegment is shared with CarpetDebugApi (section 7.2); the scripted
    /// pointer (ScriptedPointer, Editor/StoreGifApi clip takes) can stand in for the mouse.
    /// </summary>
    public sealed class ToolController : MonoBehaviour
    {
        public CarpetSimulation sim;
        public CarpetRenderer view;
        public Camera cam;
        public BoxCollider carpetCollider;
        public FirstPersonPlayer player;
        public bool InputEnabled = true;
        /// <summary>
        /// Set by SteamService while the Steam overlay is up (Docs/STEAM.md 3). Acts like InputEnabled = false, but is a
        /// separate flag because the hand-over owns InputEnabled and would overwrite it mid-flight.
        /// </summary>
        [System.NonSerialized] public bool Suspended;
        /// <summary>
        /// Set by the upgrade shop while it is open (Docs/ECONOMY.md 5); a flag of its own for the same reason Suspended is.
        /// Clearing it keeps the tools blocked until both mouse buttons are up, so the click that closed the shop never
        /// becomes a stroke on the carpet under it.
        /// </summary>
        public bool UiModal { get { return _uiModal; } set { if (_uiModal && !value) _modalRelease = true; _uiModal = value; } }
        [System.NonSerialized] bool _uiModal;
        [System.NonSerialized] bool _modalRelease;
        CarpetHandover _handover;     // same GameObject; R hands it the Reset (a brand-new carpet, Docs/DIRT_LEVEL.md)

        // ---- heading smoothing (Docs/TOOL_MODELS.md 6) ------------------------------------------
        // The pointer arrives quantised to whole screen pixels, and one pixel is ~1.7 mm of carpet, so the
        // raw per-frame segment of a perfectly straight drag alternates between (1,0) and (0,1) px: tens of
        // degrees of direction noise per frame. Fed straight into the stamp and the tool head that reads as
        // a permanent tremble. The filter has two stages, both tunable here.
        [Header("Heading smoothing")]
        [Tooltip("Pointer travel (mm) that must add up before it is trusted as a direction. Kills pixel-quantisation wobble; raise it if the head still shivers on slow drags.")]
        public float headingMinMoveMm = 2.5f;
        [Tooltip("Travel (mm) over which the heading settles onto a new direction. Distance, not time, so a slow drag turns the head exactly as far as a fast one over the same path.")]
        public float headingTurnMm = 22f;
        [Tooltip("Ease rate (1/s) of the shown angle towards the filtered heading. 20 measured best: 1.7 deg of residual tremble, 90% of a 90 deg corner followed in 0.30 s.")]
        public float headingEaseHz = 20f;
        [Tooltip("Hard cap on how fast the head may turn (deg/s), so no single frame can snap it round.")]
        public float headingMaxDegPerSec = 360f;
        [Tooltip("A pointer move longer than this in one frame is a teleport (window re-entry, UI release), not a stroke: it must not spin the head.")]
        public float headingTeleportMm = 300f;
        [Tooltip("Ease rate (1/s) of the cursor hover height, so crossing the carpet rim glides instead of popping.")]
        public float heightEaseHz = 18f;

        public ToolId Current { get; private set; } = ToolId.Scraper;
        public event Action<ToolId> ToolChanged;
        /// <summary>Once per StrokeSegment call (each lays at least one stamp), with (tool, uvTo): SteamAchievements counts the tools used with it.</summary>
        public event Action<ToolId, Vector2> Stamped;

        static readonly Vector2 CarpetMm = CarpetSimulation.CarpetSizeMm;

        /// <summary>ToolDefinition.hotkey slot -> key: 1..9 the digits, 10 the 0 key (index 0 = no hotkey).</summary>
        static readonly Key[] NumberRow =
        {
            Key.None, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
        };

        Vector2 _lastUv;
        bool _hasLast;
        float _carryMm;
        Vector2 _hoverUv = new Vector2(0.5f, 0.5f);

        float _headAngle;             // eased tool AXIS, deg in mm space, mod 180: what the head is drawn at
        float _headTarget;            // distance-filtered measurement the eased axis chases
        Vector2 _headAccum;           // travel too short to carry a direction yet, mm
        bool _headSeeded;             // false = snap to the first trusted direction instead of easing to it
        float _travelSign = 1f;       // which way along the axis the tool is actually moving, +1 or -1
        Vector2 _prevPointerUv;
        bool _hasPointer;
        float _cursorHeightM = 0.03f;
        bool _hasCursorHeight;
        readonly RaycastHit[] _aimOccluders = new RaycastHit[32];

        public bool FirstPersonActive => player != null && player.Active;
        /// <summary>Validated contact, shared by the reticle and the held tool. False beyond reach or behind workshop furniture.</summary>
        public bool HasSurfaceAim { get; private set; }
        public Vector2 SurfaceAimUV => _hoverUv;
        public Vector3 SurfaceAimWorld
        {
            get
            {
                Transform root = carpetCollider != null ? carpetCollider.transform : transform;
                Vector2 size = CarpetSimulation.CarpetSizeM;
                return root.TransformPoint(new Vector3((_hoverUv.x - 0.5f) * size.x, _cursorHeightM, (_hoverUv.y - 0.5f) * size.y));
            }
        }
        public Vector3 SurfaceAimDirection
        {
            get
            {
                Transform root = carpetCollider != null ? carpetCollider.transform : transform;
                Vector2 direction = ViewDir;
                return root.TransformDirection(new Vector3(direction.x, 0f, direction.y));
            }
        }

        // squeegee plow (Docs/SQUEEGEE.md): where the previous sweep ended, so the next one starts from the same blade
        Vector2 _plowPrevAxis, _plowPrevP1;
        bool _plowHasPrev;

        /// <summary>Unit tool axis: the direction the cursor quad and the 3D head are drawn along.</summary>
        Vector2 ViewDir { get { float r = _headAngle * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(r), Mathf.Sin(r)); } }

        /// <summary>
        /// What goes into the stamp: the same axis, signed by the way the tool is really travelling. The
        /// footprint reads it through abs() (SimCommon.hlsl FootprintWeight) so it gets the drawn axis, while
        /// the fibre bend, the debris throw and vStroke read it signed and get the real stroke direction.
        /// </summary>
        Vector2 StrokeDir { get { return ViewDir * _travelSign; } }

        void Awake()
        {
            if (sim == null) sim = GetComponent<CarpetSimulation>();
            if (view == null) view = GetComponent<CarpetRenderer>();
            if (carpetCollider == null) carpetCollider = GetComponent<BoxCollider>();
            if (cam == null) cam = Camera.main;
            if (player == null) player = UnityEngine.Object.FindAnyObjectByType<FirstPersonPlayer>();
            _handover = GetComponent<CarpetHandover>();
            // scene reload is disabled (Enter Play Mode options): a capture that died mid-take must not keep the mouse away
            ScriptedPointer = false; ScriptedDown = false; ScriptedRight = false;
            HasSurfaceAim = false;
        }

        public void SetTool(ToolId t)
        {
            int i = Mathf.Clamp((int)t, 0, ToolTable.All.Length - 1);
            t = (ToolId)i;
            // every tool is always selectable: while the carpet is away the carpet tools act on the bare tile
            // under it exactly like they act on the rest of the floor (Docs/HANDOVER.md 6)
            if (t == Current) return;
            Current = t;
            _hasLast = false; _carryMm = 0f; _plowHasPrev = false;
            if (FirstPersonActive && !ScriptedPointer)
            {
                SnapHeadingOnNextMove();
                _hasPointer = false;
            }
            ToolChanged?.Invoke(t);
        }

        /// <summary>True when the last TryGetCarpetUV hit lies on the carpet itself (uv inside [0,1]^2); false on the floor or when nothing was hit.</summary>
        public bool PointerOnCarpet { get; private set; }

        /// <summary>
        /// True while a mouse button is held down on the carpet/floor and the tool is actually laying stamps.
        /// CleaningHUD reads it to fade the whole HUD out except the two bars, so the strips and buttons stop
        /// covering the debris the player is working on.
        /// </summary>
        public bool IsStroking { get; private set; }

        /// <summary>
        /// Pointer speed along the surface for the stroke segment in flight, mm/s; 0 while nothing is being
        /// stamped. The same number the stamps carry, so what ToolAudio (Docs/AUDIO.md) hears and what the
        /// simulation feels can never drift apart. Scripted strokes set it too.
        /// </summary>
        public float StrokeSpeedMmPerSec { get; private set; }

        /// <summary>Pressure of the stroke in flight: 1 left button, 0.5 right, 0 when not stroking.</summary>
        public float StrokePressure { get; private set; }

        /// <summary>
        /// Stamps laid since the scene loaded, whoever laid them. IsStroking only knows about the mouse, so
        /// scripted work (CarpetDebugApi, DebugLiveRunner) would be silent; ToolAudio watches this counter for
        /// change instead of a frame number, which keeps it right both in real frames and in scripted steps,
        /// where many simulation frames run inside one Unity frame.
        /// </summary>
        public int StampCount { get; private set; }

        int _lastStampFrame = -1;

        /// <summary>
        /// Drops the stroke readouts, unless something already stamped this frame. DebugLiveRunner stamps from
        /// its own Update with InputEnabled off, and component order inside one frame is undefined, so an
        /// unconditional reset here could wipe a live stroke's speed before ToolAudio ever reads it.
        /// </summary>
        void ClearStrokeIfIdle()
        {
            if (_lastStampFrame == Time.frameCount) return;
            StrokeSpeedMmPerSec = 0f;
            StrokePressure = 0f;
        }

        /// <summary>
        /// "Is the pointer over a HUD widget that must swallow the click?" Set by CleaningHUD, because a
        /// UI Toolkit panel is invisible to EventSystem.IsPointerOverGameObject(): the HUD supplies its own
        /// hit test over the elements it marked as blocking (Docs/HUD.md 3). Null = nothing blocks.
        /// An instance field, not a static one, so a play session with domain reload disabled cannot keep a
        /// delegate pointing at a destroyed HUD.
        /// </summary>
        public System.Func<Vector2, bool> PointerOverUI;

        // ---- scripted pointer (Editor/StoreGifApi, Docs/STORE_ART.md description clips) --------------------------
        /// <summary>
        /// Capture hook: while true the mouse is ignored and the pointer is ScriptedUv in carpet UV (the floor domain
        /// u -1..2, v -0.7..1.7 counts as hovering, anything else as "off the play area": no head), pressed while
        /// ScriptedDown (ScriptedRight = the right button, half pressure). It replaces only the mouse inputs (position ray,
        /// buttons, wheel); everything after them is the real path: StrokeSegment, the heading filter, the hover-height ease,
        /// IsStroking (HUD fade, brush spin, audio) and SetCursor. Gates: InputEnabled (hand-over flight), Suspended (Steam
        /// overlay) and UiModal (shop) block it exactly like the mouse, and after the shop closes the release wait reads
        /// ScriptedDown instead of the mouse buttons. PointerOverUI is not consulted: a UV has no screen position, so the
        /// script decides what the pointer is over. Hotkeys still work. Real frames stepped in an unfocused Editor
        /// otherwise read wherever the OS mouse happens to be (the 01_hero_reveal re-shoot pitfall). Cleared in Awake.
        /// </summary>
        [System.NonSerialized] public bool ScriptedPointer;
        [System.NonSerialized] public Vector2 ScriptedUv = new Vector2(0.5f, 0.5f);
        [System.NonSerialized] public bool ScriptedDown;
        [System.NonSerialized] public bool ScriptedRight;

        /// <summary>
        /// Screen point -> carpet UV. A carpet-collider hit returns the uv clamped to [0,1]. When the collider is missed the ray is
        /// intersected with the carpet root's y = 0 plane and the uv is accepted UNCLAMPED inside the floor domain
        /// u in [-1, 2], v in [-0.7, 1.7] (FLOOR_SYSTEM.md 6), so the tools also work on the floor around the carpet
        /// (the floor sim consumes the same stamp buffer; off-carpet stamps have zero footprint on the carpet cells).
        /// </summary>
        public bool TryGetCarpetUV(Vector2 screenPos, out Vector2 uv)
        {
            uv = _hoverUv;
            PointerOnCarpet = false;
            if (cam == null) cam = Camera.main;
            if (cam == null) return false;
            Ray ray = cam.ScreenPointToRay(screenPos);
            return TryGetSurfaceUV(ray, 100f, false, out uv);
        }

        /// <summary>FPS aim uses the viewport centre, short tool reach (unlimited for the hose) and solid-object occlusion.</summary>
        public bool TryGetFirstPersonUV(out Vector2 uv)
        {
            uv = _hoverUv;
            PointerOnCarpet = false;
            HasSurfaceAim = false;
            if (!FirstPersonActive) return false;
            float reach = Current == ToolId.Hose ? Mathf.Infinity : Mathf.Max(player.toolReach, 0.1f);
            bool valid = TryGetSurfaceUV(player.InteractionRay, reach, true, out uv);
            if (valid)
            {
                _hoverUv = uv;
                // Before the first stroke the blade faces away from the player, with its working
                // edge across the view. An unseeded world-X axis made it point down the screen.
                // Thereafter the existing travel filter owns direction just as in scripted strokes.
                if (!_headSeeded)
                {
                    Transform root = carpetCollider != null ? carpetCollider.transform : transform;
                    Vector3 forward = root.InverseTransformDirection(player.viewCamera.transform.forward);
                    if (forward.x * forward.x + forward.z * forward.z < 0.0001f)
                        forward = root.InverseTransformDirection(player.transform.forward);
                    _headAngle = _headTarget = Mathf.Atan2(forward.z, forward.x) * Mathf.Rad2Deg;
                    _headAccum = Vector2.zero;
                    _travelSign = 1f;
                    _headSeeded = true;
                }
                if (!_hasCursorHeight)
                    _cursorHeightM = PointerOnCarpet ? 0.03f : FloorConsts.FloorYMm * 0.001f + 0.012f;
            }
            HasSurfaceAim = valid;
            return valid;
        }

        bool TryGetSurfaceUV(Ray ray, float maxDistance, bool checkOcclusion, out Vector2 uv)
        {
            uv = _hoverUv;
            PointerOnCarpet = false;
            Transform root = carpetCollider != null ? carpetCollider.transform : transform;
            var size = CarpetSimulation.CarpetSizeM;

            bool colliderHit = false;
            Vector3 hitPoint = Vector3.zero;
            float distance;
            if (carpetCollider != null && carpetCollider.enabled && carpetCollider.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                colliderHit = true;
                hitPoint = hit.point;
            }

            Vector3 local;
            if (colliderHit)
            {
                local = root.InverseTransformPoint(hitPoint);
                distance = Vector3.Distance(ray.origin, hitPoint);
            }
            else
            {
                // Use the actual tile height. Even a 3mm height error visibly offsets a shallow,
                // long-range hose shot from the tile under the reticle.
                var plane = new Plane(root.up, root.TransformPoint(new Vector3(0f, FloorConsts.FloorYMm * 0.001f, 0f)));
                if (!plane.Raycast(ray, out float enter) || enter > maxDistance) return false;
                distance = enter;
                local = root.InverseTransformPoint(ray.GetPoint(enter));
            }
            // The carpet/floor intersection alone would let the player clean through a table or wall.
            // Ignore our own capsule and the carpet hit, but no other nearer solid collider.
            if (checkOcclusion)
            {
                int count = Physics.RaycastNonAlloc(ray, _aimOccluders, Mathf.Max(0f, distance - 0.015f),
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    Collider obstacle = _aimOccluders[i].collider;
                    if (obstacle == null || obstacle == carpetCollider) continue;
                    if (player != null && obstacle.transform.IsChildOf(player.transform)) continue;
                    return false;
                }
            }
            uv = new Vector2((local.x + size.x * 0.5f) / size.x, (local.z + size.y * 0.5f) / size.y);
            bool carpetHere = carpetCollider != null && carpetCollider.enabled;

            if (colliderHit)
            {
                uv.x = Mathf.Clamp01(uv.x); uv.y = Mathf.Clamp01(uv.y);
                PointerOnCarpet = true;
                return true;
            }
            // The active floor domain is centred on the rug; no clamping so the stamp stays under the pointer.
            if (!InsideFloorUV(uv)) return false;
            // with the collider off (carpet handed over) the rect is bare tile, so the cursor must stay at tile height
            PointerOnCarpet = carpetHere && uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
            return true;
        }

        /// <summary>Whether a carpet-UV position lies inside the physical floor simulation domain.</summary>
        static bool InsideFloorUV(Vector2 uv)
        {
            Vector2 cmm = Vector2.Scale(uv, CarpetMm);
            Vector2 lo = FloorConsts.FloorMinMm;
            Vector2 hi = lo + Vector2.one * FloorConsts.FloorSizeMm;
            return cmm.x >= lo.x && cmm.x <= hi.x && cmm.y >= lo.y && cmm.y <= hi.y;
        }

        /// <summary>
        /// The scripted pointer's TryGetCarpetUV: same floor domain and PointerOnCarpet rule, no ray. A point on the carpet
        /// rect counts as on the carpet only while its collider is enabled (with the carpet handed over it is bare tile).
        /// </summary>
        bool ScriptedCarpetUV(Vector2 p, out Vector2 uv)
        {
            uv = _hoverUv;
            PointerOnCarpet = false;
            if (!InsideFloorUV(p)) return false;
            uv = p;
            bool carpetHere = carpetCollider != null && carpetCollider.enabled;
            PointerOnCarpet = carpetHere && p.x >= 0f && p.x <= 1f && p.y >= 0f && p.y <= 1f;
            return true;
        }

        /// <summary>
        /// Makes the next trusted pointer direction snap the head instead of easing it round. Used when a
        /// stroke has no continuity with what came before it (scripted strokes, Docs/TOOL_MODELS.md 6).
        /// </summary>
        public void SnapHeadingOnNextMove() { _headSeeded = false; _headAccum = Vector2.zero; _travelSign = 1f; _plowHasPrev = false; }

        /// <summary>Camera teleports / debug poses must never lay a stroke across the old and new contact points.</summary>
        public void ResetPointerContinuity()
        {
            _hasLast = false; _carryMm = 0f; _hasPointer = false; _hasCursorHeight = false;
            _plowHasPrev = false; IsStroking = false; HasSurfaceAim = false;
            StrokeSpeedMmPerSec = 0f; StrokePressure = 0f;
            SnapHeadingOnNextMove();
        }

        /// <summary>
        /// One frame of pointer travel (mm) -> the filtered heading. Stage 1 accumulates travel until it is
        /// long enough to mean something and blends the target towards it by 1-exp(-len/headingTurnMm);
        /// stage 2 eases the shown angle towards that target under a deg/s cap. Called exactly once per
        /// frame, from the stroke path or the hover path, never both.
        /// </summary>
        void UpdateHeading(Vector2 segMm, float dt)
        {
            if (segMm.magnitude > Mathf.Max(headingTeleportMm, 1f)) _headAccum = Vector2.zero;   // teleport, not a stroke
            else _headAccum += segMm;

            float l = _headAccum.magnitude;
            if (l >= Mathf.Max(headingMinMoveMm, 0.01f))
            {
                float meas = Mathf.Atan2(_headAccum.y, _headAccum.x) * Mathf.Rad2Deg;
                _headAccum = Vector2.zero;
                if (!_headSeeded) { _headTarget = _headAngle = meas; _travelSign = 1f; _headSeeded = true; }
                else
                {
                    // A tool head is an axis, not an arrow: dragged back over its own path it covers exactly
                    // the same footprint. So fold the measurement onto whichever of its two representatives
                    // the axis already points at and remember which way along it the tool is travelling. A
                    // reversal then moves the axis by nothing at all and only flips the sign.
                    // Ease the signed angle round instead and it sweeps through the perpendicular: for 0.3 s
                    // the 220 mm squeegee lies ALONG the stroke and paints a thin stripe the head never
                    // covered, which is exactly the bug this replaced.
                    // The fold has to be applied here, to the measurement. Applied to the eased angle it
                    // never fires, because that angle follows a reversal round degree by degree and never
                    // sees a jump bigger than 90.
                    bool flipped = Mathf.Abs(Mathf.DeltaAngle(_headTarget, meas)) > 90f;
                    float vm = flipped ? meas + 180f : meas;
                    _travelSign = flipped ? -1f : 1f;
                    _headTarget = WrapDeg(_headTarget + Mathf.DeltaAngle(_headTarget, vm) * (1f - Mathf.Exp(-l / Mathf.Max(headingTurnMm, 0.1f))));
                }
            }

            if (dt <= 0f) return;
            _headAngle = EaseAngle(_headAngle, _headTarget, dt);
        }

        float EaseAngle(float cur, float target, float dt)
        {
            float step = Mathf.DeltaAngle(cur, target) * (1f - Mathf.Exp(-Mathf.Max(headingEaseHz, 0.01f) * dt));
            float cap = Mathf.Max(headingMaxDegPerSec, 1f) * dt;
            return WrapDeg(cur + Mathf.Clamp(step, -cap, cap));
        }

        static float WrapDeg(float a) { return Mathf.Repeat(a + 180f, 360f) - 180f; }

        /// <summary>Emits stamps every spacingMm along the segment (remainder carried); a HOLD stamp when none. Returns the count.</summary>
        public int StrokeSegment(ToolId t, Vector2 uvFrom, Vector2 uvTo, float dt, float pressure)
        {
            if (sim == null) return 0;
            var def = ToolTuning.Effective(t);   // AREA upgrade: the stamp, the plow and the cursor all get the upgraded size
            var mm = CarpetMm;
            Vector2 pFrom = Vector2.Scale(uvFrom, mm), pTo = Vector2.Scale(uvTo, mm);
            Vector2 seg = pTo - pFrom;
            float len = seg.magnitude;
            // Two directions on purpose: the stamps are laid along the path the pointer really took, while the
            // stamp's own dir (footprint axis, fibre bend, vStroke) is the filtered heading, free of the
            // per-pixel noise and always on the axis the head is drawn at.
            Vector2 stepDir = len > 1e-4f ? seg / len : Vector2.zero;
            UpdateHeading(seg, dt);
            Vector2 dir = StrokeDir;
            if (t == ToolId.Squeegee) EnqueuePlow(pFrom, pTo, def);
            float speed = dt > 1e-6f ? len / dt : 0f;
            float spacing = Mathf.Max(def.spacingMm, 0.5f);
            StrokeSpeedMmPerSec = speed;
            StrokePressure = Mathf.Clamp01(pressure);
            StampCount++;
            _lastStampFrame = Time.frameCount;
            Stamped?.Invoke(t, uvTo);

            int count = 0;
            float first = spacing - _carryMm;                 // distance from pFrom to the first stamp
            float pos = first;
            while (pos <= len + 1e-4f) { count++; pos += spacing; }
            _carryMm = len - (pos - spacing);                 // travelled since the last stamp (== old carry + len if none)
            if (_carryMm < 0f) _carryMm = 0f;

            var st = new ToolStamp
            {
                dir = dir, radiusMm = def.radiusMm, lengthMm = def.isBlade ? def.lengthMm : 0f,
                pressure = Mathf.Clamp01(pressure), tool = (int)t
            };
            if (count == 0)
            {
                st.uv = uvTo; st.speed = 0f; st.vStroke = Vector2.zero; st.dtS = dt;
                sim.EnqueueStamp(st);
                return 1;
            }
            st.speed = speed; st.vStroke = dir * speed; st.dtS = dt / count;
            for (int i = 0; i < count; i++)
            {
                Vector2 pm = pFrom + stepDir * (first + i * spacing);
                st.uv = new Vector2(pm.x / mm.x, pm.y / mm.y);
                sim.EnqueueStamp(st);
            }
            return count;
        }

        /// <summary>
        /// The squeegee moves water and foam by the path its blade sweeps, not by stamps (Docs/SQUEEGEE.md): one plow sweep
        /// per segment, from the previous blade (centre + axis) to this one. The previous axis is only trusted when the sweep
        /// continues the last one; after a stroke start, a heading snap or a sharp turn the blade is taken as not rotating,
        /// otherwise the rotation term would sweep a bogus fan of up to 220 mm at the blade ends. Long segments are split
        /// into SQ_SUB_MM pieces so no single sweep has to cover more than the kernel's look-back.
        /// </summary>
        void EnqueuePlow(Vector2 p0, Vector2 p1, in ToolDefinition def)
        {
            Vector2 a1 = ViewDir;
            Vector2 a0 = a1;
            if (_plowHasPrev && (p0 - _plowPrevP1).sqrMagnitude < 1f && Vector2.Dot(_plowPrevAxis, a1) > 0.94f) a0 = _plowPrevAxis;   // < ~20 deg
            _plowPrevAxis = a1; _plowPrevP1 = p1; _plowHasPrev = true;

            float sub = sim.settings != null ? Mathf.Max(sim.settings.SQ_SUB_MM, 5f) : 60f;
            int n = Mathf.Clamp(Mathf.CeilToInt((p1 - p0).magnitude / sub), 1, 8);
            Vector2 pa = p0, aa = a0;
            for (int i = 1; i <= n; i++)
            {
                float f = i / (float)n;
                Vector2 pb = i == n ? p1 : Vector2.Lerp(p0, p1, f);
                Vector2 ab = i == n ? a1 : Vector2.Lerp(a0, a1, f).normalized;
                sim.EnqueuePlow(new PlowSweep
                {
                    p0 = pa, p1 = pb, a0 = aa, a1 = ab, sign = _travelSign,
                    radiusMm = def.radiusMm, halfLengthMm = 0.5f * def.lengthMm
                });
                pa = pb; aa = ab;
            }
        }

        bool ModalBlocked()
        {
            if (_uiModal) return true;
            if (!_modalRelease) return false;
            if (ScriptedPointer)
            {
                if (ScriptedDown) return true;
            }
            else
            {
                var m = Mouse.current;
                if (m != null && (m.leftButton.isPressed || m.rightButton.isPressed)) return true;
            }
            _modalRelease = false;
            return false;
        }

        void Update()
        {
            HasSurfaceAim = false;
            if (sim == null) return;
            var def = ToolTuning.Effective(Current);
            bool firstPersonInput = FirstPersonActive && !ScriptedPointer;
            if (!InputEnabled || Suspended || ModalBlocked() || (firstPersonInput && !player.CanControl))
            {
                _hasLast = false; IsStroking = false; _hasPointer = false; _hasCursorHeight = false;
                ClearStrokeIfIdle();
                if (view != null) view.SetCursor(_hoverUv, ViewDir, def.radiusMm, def.lengthMm, Current, false);
                return;
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                // number row, straight from ToolTable so the keys and the HUD chips cannot disagree:
                // 1..8 the carpet tools; the floor tools have no hotkey (mouse only, FLOOR_TOOLS.md 1)
                for (int i = 0; i < ToolTable.All.Length; i++)
                {
                    int slot = ToolTable.All[i].hotkey;
                    if (slot >= 1 && slot < NumberRow.Length && kb[NumberRow[slot]].wasPressedThisFrame) SetTool(ToolTable.All[i].id);
                }
                // The old pointer HUD deliberately left the floor strip mouse-only. In first person
                // all tools need direct keys because the cursor belongs to the camera.
                if (firstPersonInput && kb[Key.Digit9].wasPressedThisFrame) SetTool(ToolId.FloorCleaner);
                if (firstPersonInput && kb[Key.Digit0].wasPressedThisFrame) SetTool(ToolId.Mop);
                if (kb.rKey.wasPressedThisFrame)
                {
                    // Reset = a brand-new carpet with new dirt (picture and colour too), floor and debris reset
                    if (_handover != null) _handover.ResetToNewCarpet(); else sim.ResetCarpet(sim.Seed + 1);
                    _hasLast = false; _carryMm = 0f;
                }
                def = ToolTuning.Effective(Current);
            }

            bool hovering, overUI, lmb, rmb;
            Vector2 uv;
            if (ScriptedPointer)
            {
                // capture takes (ScriptedPointer above): a UV instead of the mouse ray, no wheel, no HUD hit test
                overUI = false;
                hovering = ScriptedCarpetUV(ScriptedUv, out uv);
                lmb = ScriptedDown && !ScriptedRight;
                rmb = ScriptedDown && ScriptedRight;
            }
            else
            {
                var mouse = Mouse.current;
                if (mouse == null)
                {
                    _hasLast = false; IsStroking = false; _hasPointer = false; _hasCursorHeight = false;
                    ClearStrokeIfIdle();
                    if (view != null) view.SetCursor(_hoverUv, ViewDir, def.radiusMm, def.lengthMm, Current, false);
                    return;
                }

                Vector2 screenPos = mouse.position.ReadValue();
                // In FPS the mouse is captured: HUD panels do not intercept the centre aim. Modal
                // panels already own UiModal above and release the player's cursor.
                overUI = !firstPersonInput && ((EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                         || (PointerOverUI != null && PointerOverUI(screenPos)));
                float scroll = mouse.scroll.ReadValue().y;
                if (!overUI && Mathf.Abs(scroll) > 0.01f)
                {
                    // FPS has no clickable tool strips: the wheel reaches all ten tools, including detergent and mop.
                    int next = ((int)Current + (scroll > 0f ? -1 : 1) + ToolTable.All.Length) % ToolTable.All.Length;
                    SetTool(firstPersonInput ? (ToolId)next : ToolTable.NextOnStrip(Current, scroll > 0f ? -1 : 1));
                    def = ToolTuning.Effective(Current);
                }

                hovering = (firstPersonInput ? TryGetFirstPersonUV(out uv) : TryGetCarpetUV(screenPos, out uv)) && !overUI;
                lmb = mouse.leftButton.isPressed; rmb = mouse.rightButton.isPressed;
            }
            if (hovering) _hoverUv = uv;
            HasSurfaceAim = hovering;

            bool held = (lmb || rmb) && !overUI;
            // the HUD hides for as long as the button is down once the stroke has started, even if the pointer
            // later leaves the floor domain: a HUD that flickers back mid-stroke is worse than one that stays away
            IsStroking = (lmb || rmb) && (_hasLast || (held && hovering));
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f) * sim.TimeScale;
            if (dt <= 0f) dt = 1f / 60f;
            if (held && hovering)
            {
                // A fast camera turn can jump the ray across the room. It starts a new contact;
                // interpolating such a jump would clean a long stripe the tool never traversed.
                if (firstPersonInput && _hasLast && Vector2.Scale(uv - _lastUv, CarpetMm).magnitude > headingTeleportMm)
                {
                    _hasLast = false; _carryMm = 0f; _plowHasPrev = false;
                    SnapHeadingOnNextMove();
                }
                if (!_hasLast) { _lastUv = uv; _hasLast = true; _carryMm = 0f; _plowHasPrev = false; }
                StrokeSegment(Current, _lastUv, uv, dt, lmb ? 1f : 0.5f);   // feeds the heading filter
                _lastUv = uv;
            }
            else
            {
                _hasLast = false;
                _carryMm = 0f;
                ClearStrokeIfIdle();
                // The head follows the pointer while it only hovers as well, so pressing the button never
                // snaps it round from a heading left over from the previous stroke.
                Vector2 hoverSeg = hovering && _hasPointer ? Vector2.Scale(uv - _prevPointerUv, CarpetMm) : Vector2.zero;
                UpdateHeading(hoverSeg, dt);
            }
            if (hovering) { _prevPointerUv = uv; _hasPointer = true; } else _hasPointer = false;

            // Carpet hover height and tile hover height are 21 mm apart, so crossing the rim popped the head
            // up and down and shivered wherever the collider edge is ambiguous. Ease it; snap only when the
            // cursor reappears somewhere new.
            float targetH = PointerOnCarpet ? 0.03f : FloorConsts.FloorYMm * 0.001f + 0.012f;
            if (!_hasCursorHeight || !hovering) { _cursorHeightM = targetH; _hasCursorHeight = hovering; }
            else _cursorHeightM += (targetH - _cursorHeightM) * (1f - Mathf.Exp(-Mathf.Max(heightEaseHz, 0.01f) * dt));

            if (view != null) view.SetCursor(_hoverUv, ViewDir, def.radiusMm, def.lengthMm, Current, hovering, _cursorHeightM);
            HasSurfaceAim = hovering;
        }
    }
}
