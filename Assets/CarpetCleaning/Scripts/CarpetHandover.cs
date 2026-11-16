using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CarpetCleaning
{
    public enum HandoverState { Idle = 0, FlyOut = 1, Away = 2, FlyIn = 3 }

    /// <summary>
    /// "Hand over to client" / "Take new carpet" (Docs/HANDOVER.md). Idle -> FlyOut -> Away -> FlyIn -> Idle.
    /// Animates CarpetRenderer.Stage ONLY, never CarpetRoot: the collider, the tool cursor, the stamp path, the pointer
    /// fallback plane and the debris frame (_CarpetToWorld) all live on the root and must stay at the origin.
    /// The floor keeps its state across a hand-over: the swap calls ResetCarpet(seed, notify: false).
    /// Order 210 = after CarpetRenderer (200), so Stage and the child renderers exist when Awake runs.
    /// </summary>
    [DefaultExecutionOrder(210)]
    public sealed class CarpetHandover : MonoBehaviour
    {
        public CarpetSimulation sim;
        public CarpetRenderer view;
        public ToolController tools;
        public DebrisSystem debris;
        public Texture2D[] pictures;                       // full catalogue: builder's serialized list plus Resources/CarpetPictures
        public const string PictureOverrideResource = "CarpetPicturesOverride";
        public Texture2D[] ActivePictures { get; private set; }

        [Header("Flight (metres / degrees, Docs/HANDOVER.md)")]
        public Vector3 outPos = new Vector3(0.25f, 0.65f, 8.5f);
        public Vector3 outEuler = new Vector3(-18f, 4f, 6f);
        public Vector3 inPos = new Vector3(0.10f, 0.45f, 8.3f);
        public Vector3 inEuler = new Vector3(14f, -3f, -4f);
        public float flyOutSeconds = 0.85f;
        public float flyInSeconds = 0.95f;
        public int seedStep = 1;
        public bool debugForceWin;                         // CLI: offer the button without cleaning a carpet first

        public HandoverState State { get; private set; }
        public bool IsAnimating { get { return State == HandoverState.FlyOut || State == HandoverState.FlyIn; } }
        public bool CanHandOver { get { return State == HandoverState.Idle && sim != null && (debugForceWin || sim.IsWon); } }
        public bool CanTakeNew { get { return State == HandoverState.Away; } }
        public float Phase01 { get; private set; }
        public int PictureIndex { get; private set; }
        public string PictureName { get; private set; }
        public event Action<HandoverState> StateChanged;

        Material _fiber;                                   // runtime instance: the .mat asset is never written
        readonly List<int> _picBag = new List<int>();
        readonly List<int> _colBag = new List<int>();
        int _lastPicture = -1;
        bool _subscribed;
        CarpetSimulation _subscribedTo;

        // mid-dark rug bases; the 3x sun blows out anything lighter (SESSION_LOG 2026-09-19). The rug photos are opaque and
        // cover the whole carpet, so the base only shows through a picture with transparent texels.
        static readonly Color[] BaseColors =
        {
            new Color(0.20f, 0.36f, 0.62f),   // deep blue (the authored one)
            new Color(0.45f, 0.16f, 0.20f),   // burgundy
            new Color(0.16f, 0.34f, 0.24f),   // forest
            new Color(0.60f, 0.50f, 0.34f),   // sand
            new Color(0.18f, 0.36f, 0.38f),   // teal slate
            new Color(0.32f, 0.20f, 0.40f),   // plum
        };

        void Awake()
        {
            PictureIndex = -1;
            PictureName = null;
            _lastPicture = -1;
            if (sim == null) sim = GetComponent<CarpetSimulation>();
            if (view == null) view = GetComponent<CarpetRenderer>();
            if (tools == null) tools = GetComponent<ToolController>();
            if (debris == null) debris = UnityEngine.Object.FindFirstObjectByType<DebrisSystem>();
            MergeResourcePictures();
            var overrides = LoadPictureOverrides();
            ActivePictures = overrides.Length > 0 ? overrides : pictures;
            _picBag.Clear(); _colBag.Clear();               // domain reload is disabled: never let a bag survive a play session
            EnsureMaterial();
            if (PictureIndex < 0) ApplyNextLook();           // an excluded first picture must not survive from the scene/material
            SnapToIdle();
        }

        void OnEnable() { Subscribe(); }
        void OnDisable() { Unsubscribe(); }

        void OnDestroy()
        {
            Unsubscribe();
            if (_fiber != null) { Destroy(_fiber); _fiber = null; }
        }

        void Subscribe()
        {
            if (_subscribed || sim == null) return;
            sim.CarpetReset += OnCarpetReset;
            _subscribedTo = sim;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_subscribedTo != null) _subscribedTo.CarpetReset -= OnCarpetReset;
            _subscribedTo = null;
            _subscribed = false;
        }

        // The hand-over swap passes notify: false, so this only fires for a real full reset (Reset button / R via
        // ResetToNewCarpet, debug API).
        void OnCarpetReset() { SnapToIdle(); }

        void MergeResourcePictures()
        {
            // Existing scenes keep their authored order and first picture. Resources makes newly shipped designs
            // available without rebuilding the scene; a later Build Scene may already serialize the same assets.
            var extra = Resources.LoadAll<Texture2D>("CarpetPictures");
            Array.Sort(extra, (a, b) => StringComparer.Ordinal.Compare(a.name, b.name));
            var merged = pictures != null ? new List<Texture2D>(pictures) : new List<Texture2D>();
            foreach (var picture in extra)
                if (picture != null && !merged.Contains(picture)) merged.Add(picture);
            pictures = merged.ToArray();
        }

        /// <summary>A nonempty Resources/CarpetPicturesOverride temporarily replaces the active pool, keeping the catalogue intact.</summary>
        public static Texture2D[] LoadPictureOverrides()
        {
            var active = Resources.LoadAll<Texture2D>(PictureOverrideResource);
            Array.Sort(active, (a, b) => StringComparer.Ordinal.Compare(a.name, b.name));
            return active;
        }

        void EnsureMaterial()
        {
            if (_fiber == null)
            {
                if (view == null) return;
                var mr = view.ShellsRenderer;
                if (mr == null || mr.sharedMaterial == null) return;
                _fiber = new Material(mr.sharedMaterial)
                {
                    name = mr.sharedMaterial.name + " (runtime)",
                    hideFlags = HideFlags.DontUnloadUnusedAsset
                };
                _fiber.SetFloat("_ShellCount", view.shellCount);
                mr.sharedMaterial = _fiber;
            }
            // Re-identify the current texture even when scene/domain reload leaves the runtime material alive.
            var cur = _fiber.GetTexture("_PictureTex") as Texture2D;
            if (cur != null && ActivePictures != null)
            {
                for (int i = 0; i < ActivePictures.Length; i++)
                {
                    if (ActivePictures[i] != cur) continue;
                    _lastPicture = i; PictureIndex = i; PictureName = cur.name;
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ public API
        /// <summary>Idle -> FlyOut. Returns false when the carpet is not ready to be handed over.</summary>
        public bool Begin(bool force = false)
        {
            if (State != HandoverState.Idle) return false;
            if (!force && !CanHandOver) return false;
            if (tools != null)
            {
                tools.InputEnabled = false;
                if (tools.carpetCollider != null) tools.carpetCollider.enabled = false;
            }
            // the foam surface reads the FLOOR foam field in its 24 mm rim band and snaps its rim vertices to the floor
            // grid pitch; both are meaningless once the mesh is a metre away, so it stays hidden for the whole trip
            SetRenderers(true, true, false);
            if (debris != null) debris.KillCarpetRect();
            Phase01 = 0f;
            SetState(HandoverState.FlyOut);
            ApplyPose();
            return true;
        }

        /// <summary>
        /// Away -> FlyIn: new picture, new base colour, fresh dirt at the next seed's own random level and soil mask
        /// (Docs/DIRT_LEVEL.md); the floor and the debris are left alone.
        /// </summary>
        public bool TakeNewCarpet()
        {
            if (State != HandoverState.Away) return false;
            ApplyNextLook();
            if (sim != null) sim.ResetCarpet(sim.Seed + Mathf.Max(seedStep, 1), false);
            if (debris != null) debris.KillCarpetRect();
            SetRenderers(true, true, false);
            Phase01 = 0f;
            SetState(HandoverState.FlyIn);
            ApplyPose();
            return true;
        }

        /// <summary>
        /// Reset button / R (Docs/DIRT_LEVEL.md): a brand-new carpet in place, from any state - next picture and base colour,
        /// next seed, i.e. a new dirt level, mix and mask. It is a full reset: the floor and the debris start over too, and
        /// CarpetReset snaps the stage home.
        /// </summary>
        public void ResetToNewCarpet()
        {
            if (sim == null) return;
            ApplyNextLook();
            sim.ResetCarpet(sim.Seed + Mathf.Max(seedStep, 1));
        }

        /// <summary>Everything back to the home pose: used by Awake and by a full CarpetReset from any state.</summary>
        public void SnapToIdle()
        {
            Phase01 = 0f;
            var st = Stage;
            if (st != null)
            {
                st.localPosition = Vector3.zero;
                st.localRotation = Quaternion.identity;
                st.localScale = Vector3.one;
            }
            SetRenderers(true, true, true);
            if (tools != null)
            {
                if (tools.carpetCollider != null) tools.carpetCollider.enabled = true;
                tools.InputEnabled = true;
            }
            SetState(HandoverState.Idle);
        }

        /// <summary>Drives the flight. Raw delta time on purpose: this is presentation, not simulation (TimeScale must not blur it).</summary>
        public void Tick(float dt)
        {
            if (!IsAnimating) return;
            float dur = Mathf.Max(State == HandoverState.FlyOut ? flyOutSeconds : flyInSeconds, 1e-3f);
            Phase01 = Mathf.Min(Phase01 + Mathf.Max(dt, 0f) / dur, 1f);
            ApplyPose();
            if (Phase01 >= 1f) Advance();
        }

        /// <summary>Test hook: place the flight at t01 without accumulating time (frames only advance while a CLI command ticks the Editor).
        /// Goes through ApplyPose, so the corner lift applies here too.</summary>
        public void Seek(float t01)
        {
            if (!IsAnimating) return;
            Phase01 = Mathf.Clamp01(t01);
            ApplyPose();
        }

        // ------------------------------------------------------------------ internals
        Transform Stage { get { return view != null ? view.Stage : null; } }

        void SetState(HandoverState s)
        {
            State = s;
            // The rect is bare tile from the moment the carpet has flown out until the new one has LANDED: the floor sim,
            // the debris system and the floor shaders drop their carpet wall there, and the carpet sim ignores every stamp.
            // Holding it through FlyIn means the puddles left inside the rect are wiped (K_FInject zeroes the wall cells)
            // exactly when the new carpet covers them, never in the open. Docs/HANDOVER.md 6.
            if (sim != null) sim.CarpetAway = (s == HandoverState.Away || s == HandoverState.FlyIn);
            var h = StateChanged;
            if (h != null) h(s);
        }

        void SetRenderers(bool shells, bool water, bool foam)
        {
            if (view == null) return;
            if (view.ShellsRenderer != null) view.ShellsRenderer.enabled = shells;
            if (view.WaterRenderer != null) view.WaterRenderer.enabled = water;
            if (view.FoamRenderer != null && view.FoamRenderer.sharedMaterial != null) view.FoamRenderer.enabled = foam;
        }

        void Advance()
        {
            if (State == HandoverState.FlyOut)
            {
                SetRenderers(false, false, false);
                var st = Stage;
                if (st != null) { st.localPosition = Vector3.zero; st.localRotation = Quaternion.identity; }
                // the floor is still there to work on, with every tool, including over the rect the carpet left bare
                if (tools != null)
                {
                    tools.InputEnabled = true;
                    tools.SetTool(ToolTable.FirstOnStrip(true));     // the mop/detergent pair is what bare tile asks for
                }
                SetState(HandoverState.Away);
            }
            else if (State == HandoverState.FlyIn)
            {
                var st = Stage;
                if (st != null) { st.localPosition = Vector3.zero; st.localRotation = Quaternion.identity; }
                SetRenderers(true, true, true);
                if (tools != null)
                {
                    if (tools.carpetCollider != null) tools.carpetCollider.enabled = true;
                    tools.InputEnabled = true;
                    if (sim != null) tools.SetTool(sim.SuggestedTool);
                }
                SetState(HandoverState.Idle);
            }
        }

        static float Smooth(float t) { return t * t * (3f - 2f * t); }
        static float InQuad(float t) { return t * t; }
        static float OutCubic(float t) { float u = 1f - t; return 1f - u * u * u; }

        void ApplyPose()
        {
            var st = Stage;
            if (st == null) return;
            float t = Mathf.Clamp01(Phase01);
            Vector3 pos;
            Quaternion rot;
            if (State == HandoverState.FlyOut)
            {
                // zero velocity at the start (no snap while it is still fully visible), accelerating out of frame
                pos = Vector3.LerpUnclamped(Vector3.zero, outPos, InQuad(t));
                rot = Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.Euler(outEuler), Smooth(Mathf.Clamp01(t / 0.70f)));
            }
            else if (State == HandoverState.FlyIn)
            {
                // slides in already moving and lands soft; the tilt flattens after the slide stops, like a rug being laid down
                pos = Vector3.LerpUnclamped(inPos, Vector3.zero, OutCubic(Mathf.Clamp01(t / 0.82f)));
                rot = Quaternion.SlerpUnclamped(Quaternion.Euler(inEuler), Quaternion.identity, OutCubic(t));
            }
            else return;
            // The tilt pivots on the rug centre and runs ahead of the lift (FlyOut: InQuad position vs Smooth(t/0.70) tilt;
            // FlyIn: the slide stops at t 0.82, the tilt flattens until t 1), so a corner went under the tiles: -78 mm on the
            // way out, -5.6 mm on landing (Docs/HANDOVER.md 10). Keep every corner at or above its resting height. Flat
            // (identity) the lift is 0 and pos.y >= 0 on both legs, so a rug lying flat is never moved.
            pos.y = Mathf.Max(pos.y, CornerLift(rot));
            st.localPosition = pos;
            st.localRotation = rot;
            st.localScale = Vector3.one;   // never scale: the shells displace by millimetres, a scale would grow the pile
        }

        /// <summary>
        /// How far (metres) the stage must rise so that none of the rug's four corners, turned by rot around the rug centre,
        /// sits below y 0, its resting height (backing top). 0 when no corner dips.
        /// </summary>
        public static float CornerLift(Quaternion rot)
        {
            var half = CarpetSimulation.CarpetSizeM * 0.5f;
            float minY = 0f;
            for (int i = 0; i < 4; i++)
                minY = Mathf.Min(minY, (rot * new Vector3((i & 1) == 0 ? -half.x : half.x, 0f, (i & 2) == 0 ? -half.y : half.y)).y);
            return -minY;
        }

        // ------------------------------------------------------------------ look of the next carpet
        void ApplyNextLook()
        {
            EnsureMaterial();
            if (_fiber == null) return;
            int pi = NextPicture();
            if (pi >= 0 && ActivePictures[pi] != null)
            {
                _fiber.SetTexture("_PictureTex", ActivePictures[pi]);
                FitPicture(ActivePictures[pi]);
                PictureIndex = pi;
                PictureName = ActivePictures[pi].name;
            }
            _fiber.SetColor("_CarpetColor", NextColor());
        }

        int NextPicture()
        {
            if (ActivePictures == null || ActivePictures.Length == 0) return -1;
            if (_picBag.Count == 0)
            {
                for (int i = 0; i < ActivePictures.Length; i++) if (ActivePictures[i] != null) _picBag.Add(i);
                Shuffle(_picBag);
                // no back-to-back repeat across a bag boundary (impossible with a single picture, which is fine)
                if (_picBag.Count > 1 && _picBag[0] == _lastPicture)
                {
                    int last = _picBag.Count - 1;
                    int tmp = _picBag[0]; _picBag[0] = _picBag[last]; _picBag[last] = tmp;
                }
            }
            if (_picBag.Count == 0) return -1;
            int idx = _picBag[0];
            _picBag.RemoveAt(0);
            _lastPicture = idx;
            return idx;
        }

        Color NextColor()
        {
            if (_colBag.Count == 0)
            {
                for (int i = 0; i < BaseColors.Length; i++) _colBag.Add(i);
                Shuffle(_colBag);
            }
            int idx = _colBag[0];
            _colBag.RemoveAt(0);
            return BaseColors[idx];
        }

        static void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }

        void FitPicture(Texture2D tex)
        {
            if (_fiber == null || tex == null) return;
            PictureFit(tex, out var offset, out var scale);
            _fiber.SetVector("_PicOffset", offset);
            _fiber.SetVector("_PicScale", scale);
        }

        /// <summary>
        /// A rug photo covers the whole carpet, so its own border becomes the carpet's edge: scaled to cover
        /// without stretching, the excess cropped evenly. The shipped rugs are exactly 4:5 and map 1:1 (Docs/HANDOVER.md 5).
        /// Shared with CarpetSceneBuilder, which writes the same values for the first carpet into the material.
        /// </summary>
        public static void PictureFit(Texture tex, out Vector4 offset, out Vector4 scale)
        {
            const float carpetW = CarpetSimulation.CarpetWidthMm, carpetH = CarpetSimulation.CarpetHeightMm;
            float a = (tex != null && tex.height > 0) ? tex.width / (float)tex.height : carpetW / carpetH;
            float uW = 1f, vH = 1f;                      // picture size in carpet uv, >= 1 on both axes
            if (a >= carpetW / carpetH) uW = a * carpetH / carpetW;
            else vH = carpetW / (Mathf.Max(a, 1e-4f) * carpetH);
            scale = new Vector4(1f / uW, 1f / vH, 0f, 0f);
            offset = new Vector4((1f - uW) * 0.5f, (1f - vH) * 0.5f, 0f, 0f);
        }

        void LateUpdate()
        {
            Tick(Time.deltaTime);
            var kb = Keyboard.current;
            if (kb == null) return;
            // Steam overlay up: its text fields must not hand the carpet over (Docs/STEAM.md 3); the upgrade shop likewise (ECONOMY.md 5)
            if (tools != null && (tools.Suspended || tools.UiModal)) return;
            if (kb.hKey.wasPressedThisFrame) Begin();
            if (kb.nKey.wasPressedThisFrame) TakeNewCarpet();
        }
    }
}
