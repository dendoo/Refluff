using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    /// <summary>
    /// Shows the 3D head of the active tool where the flat cursor outline used to be (Docs/TOOL_MODELS.md).
    /// Driven from CarpetRenderer.SetCursor, so it inherits the same uv -> world mapping and the same
    /// 0.03 m / 0.009 m hover heights ToolController already picks for the carpet and the tile.
    /// Lives under CarpetRoot, never under CarpetStage: CarpetHandover animates the stage and the tool
    /// must stay put with the collider and the stamp path (Docs/HANDOVER.md).
    /// A head whose prefab has a "Spin" child turns that part while the tool works (section 7).
    /// </summary>
    [DefaultExecutionOrder(205)]
    public sealed class ToolModelView : MonoBehaviour
    {
        /// <summary>Name of the pivot ToolModelPostprocess puts over a head's spinning part.</summary>
        public const string SpinNode = "Spin";
        /// <summary>Authored mounting point on a normalized contact-head prefab.</summary>
        public const string SocketNode = "HandleSocket";

        // The two cursor hover heights ToolController picks: surface + 12 mm over the carpet backing and over the tile.
        const float CarpetHoverM = 0.03f;
        const float TileHoverM = FloorConsts.FloorYMm * 0.001f + 0.012f;

        public ToolModelSet set;
        [Tooltip("Says when the tool is working, which is when a spinning head turns. Wired by CarpetSceneBuilder.")]
        public ToolController tools;

        /// <summary>The pivot, and the spinning part followed by its runtime copies drawn along one frame's turn.</summary>
        sealed class Spinner
        {
            public Transform pivot;
            public Transform[] parts;
            public Vector3 basePos;
            public Quaternion baseRot;
            public Vector3 baseScale;
        }

        /// <summary>
        /// Radial shrink per smear copy. A continuous bristle skirt (the v2 brush) turned a few degrees lies on itself, and
        /// the copies z-fought it; each earlier copy is nested a hair inside the later one instead (4 copies: 1.6 %,
        /// under 1 mm at the rim), so it only shows through the gaps between strands.
        /// </summary>
        const float SmearShrink = 0.004f;

        readonly Dictionary<ToolId, Transform> _instances = new Dictionary<ToolId, Transform>();
        readonly Dictionary<ToolId, Spinner> _spinners = new Dictionary<ToolId, Spinner>();
        Transform _shown;
        bool _built;

        // one machine at a time, like ToolAudio's motor: the state belongs to the current tool
        ToolId _spinTool;
        bool _hasSpinTool;
        float _spin01, _spinDeg, _stepDeg, _sinceWork = 999f, _headingDeg;
        int _stampSeen, _spinFrame = -1, _smearShown;

        bool _pinned, _pinWorking;
        Vector2 _pinUv;

        /// <summary>0..1 share of spinRpm the current tool's spinning part runs at.</summary>
        public float Spin01 { get { return _spin01; } }
        public float SpinDeg { get { return _spinDeg; } }
        /// <summary>Degrees the spinning part turned this frame.</summary>
        public float StepDeg { get { return _stepDeg; } }
        public int SmearCopies { get { return _smearShown; } }
        public bool Shown { get { return _shown != null; } }
        /// <summary>The actual contact head, used to attach the first-person tool's shaft without drifting from its footprint.</summary>
        public Transform ShownTransform { get { return _shown; } }

        void Awake()
        {
            // Enter Play Mode keeps domain and scene, so state from a previous session can still be around.
            _instances.Clear();
            _spinners.Clear();
            _shown = null;
            _built = false;
            _hasSpinTool = false;
            _spin01 = 0f; _spinDeg = 0f; _stepDeg = 0f; _sinceWork = 999f;
            _spinFrame = -1; _smearShown = 0;
            _pinned = false;
            ClearChildren();
        }

        /// <summary>
        /// Heads are instantiated on first use rather than in Awake: AddComponent runs Awake in edit mode
        /// too, and instantiating there would leave real GameObjects behind in the scene on every
        /// CarpetCleaning/Build Scene run.
        /// </summary>
        void EnsureBuilt()
        {
            if (_built || !Application.isPlaying) return;
            _built = true;
            BuildInstances();
        }

        void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
            }
        }

        void BuildInstances()
        {
            if (set == null)
            {
                Debug.LogWarning("[ToolModelView] no ToolModelSet assigned (run CarpetCleaning/Build Scene); tool heads hidden.", this);
                return;
            }
            for (int i = 0; i < ToolTable.All.Length; i++)
            {
                var id = ToolTable.All[i].id;
                if (!set.TryGet(id, out var e)) continue;
                var go = Instantiate(e.prefab, transform);
                go.name = "Tool_" + id;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                var pivot = go.transform.Find(SpinNode);
                if (pivot != null && pivot.childCount > 0) _spinners[id] = BuildSpinner(pivot);
                StripRenderers(go);
                go.SetActive(false);
                _instances[id] = go.transform;
            }
        }

        Spinner BuildSpinner(Transform pivot)
        {
            var part = pivot.GetChild(0);
            int copies = Mathf.Clamp(set.smearMaxCopies, 0, 8);
            var sp = new Spinner { pivot = pivot, parts = new Transform[copies + 1], basePos = part.localPosition, baseRot = part.localRotation, baseScale = part.localScale };
            sp.parts[0] = part;
            for (int i = 1; i <= copies; i++)
            {
                var c = Instantiate(part.gameObject, pivot);
                c.name = part.name + "_Smear" + i;
                c.SetActive(false);
                sp.parts[i] = c.transform;
            }
            return sp;
        }

        /// <summary>The scene has one shadowless sun and flat ambient; match what CarpetRenderer.Setup does.</summary>
        static void StripRenderers(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                rs[i].shadowCastingMode = ShadowCastingMode.Off;
                rs[i].receiveShadows = false;
                rs[i].lightProbeUsage = LightProbeUsage.Off;
                rs[i].reflectionProbeUsage = ReflectionProbeUsage.Off;
                rs[i].motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }
            var cols = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;   // must not catch the tool raycast
        }

        /// <summary>True when this tool has a generated head; CarpetRenderer keeps the outline when it has not.</summary>
        public bool Has(ToolId tool) { EnsureBuilt(); return _instances.ContainsKey(tool); }

        /// <summary>
        /// Debug: shows the head at `uv` over the carpet whatever the pointer does, and with `working` runs its
        /// spinning part as if the tool were stamping. The pointer normally has to be over the Game view for a
        /// head to show, which a CLI-driven Editor never has.
        /// </summary>
        public void DebugPin(bool on, Vector2 uv, bool working)
        {
            _pinned = on; _pinUv = uv; _pinWorking = on && working;
            // placed right away: an unfocused Editor runs no Update that would do it
            if (on && tools != null) SetTool(uv, new Vector2(1f, 0f), tools.Current, true, 0.03f);
        }

        /// <summary>
        /// One spin step with an explicit dt, then the shown head re-posed. Called by CarpetDebugApi.StepAll so
        /// scripted frames spin the head like real ones (the same reason ToolAudio.StepFrame exists).
        /// </summary>
        public void StepFrame(float dt)
        {
            if (set == null || tools == null) return;
            EnsureBuilt();
            var tool = tools.Current;
            AdvanceSpin(tool, dt);
            if (_shown != null && _instances.TryGetValue(tool, out var t) && t == _shown && _spinners.TryGetValue(tool, out var sp))
                ApplySpin(sp, _headingDeg);
        }

        /// <summary>
        /// Same placement as CarpetRenderer.SetCursor: local +X follows the stroke, +Z lies across it.
        /// `heightM` is the cursor hover height; the head sits on top of it by set.hoverMarginM.
        /// </summary>
        public void SetTool(Vector2 uv, Vector2 dirMm, ToolId tool, bool active, float heightM)
        {
            if (set == null) return;
            EnsureBuilt();
            if (_pinned) { uv = _pinUv; active = true; heightM = 0.03f; }
            // A held nozzle is drawn in the player's grip, not a second time sitting on the floor. Scripted captures
            // and DebugPin retain the established surface-head view.
            if (!_pinned && tools != null && tools.FirstPersonActive && !tools.ScriptedPointer && FirstPersonToolView.IsHandheld(tool))
                active = false;
            // before any early return: a pad that is let go keeps coasting down while the head is hidden
            StepSpin(tool);

            Transform t;
            if (!_instances.TryGetValue(tool, out t)) t = null;

            if (_shown != null && _shown != t) { _shown.gameObject.SetActive(false); _shown = null; }
            if (t == null) return;
            if (!active) { if (_shown != null) { _shown.gameObject.SetActive(false); _shown = null; } return; }

            set.TryGet(tool, out var e);
            if (dirMm.sqrMagnitude < 1e-8f) dirMm = new Vector2(1f, 0f);
            dirMm.Normalize();

            var size = CarpetSimulation.CarpetSizeM;
            float x = (uv.x - 0.5f) * size.x, z = (uv.y - 0.5f) * size.y;
            float angleDeg = Mathf.Atan2(dirMm.y, dirMm.x) * Mathf.Rad2Deg;
            var yaw = Quaternion.Euler(0f, -angleDeg, 0f);

            t.localRotation = yaw * Quaternion.Euler(e.eulerOffset);
            // pileSinkM only over the pile: the tile is hard. heightM eases between the two hover heights across the
            // rim, and the sink follows it, so the head never pops.
            float onPile = Mathf.InverseLerp(TileHoverM, CarpetHoverM, heightM);
            t.localPosition = new Vector3(x, heightM + set.hoverMarginM - e.pileSinkM * onPile, z) + yaw * e.posOffsetM;
            float s = e.extraScale > 0f ? e.extraScale : 1f;
            // AREA upgrade (Docs/ECONOMY.md 2): every head grows uniformly to match its effective working width.
            // Preserve the model's proportions; the first-person shaft follows the scaled HandleSocket.
            t.localScale = s * ToolTuning.HeadScale(tool);
            // the head yaws with the stroke; adding the yaw back keeps the spin rate the same while the stroke turns
            // (exact for a Y-only eulerOffset, which is all a round head needs)
            _headingDeg = angleDeg - e.eulerOffset.y;
            if (_spinners.TryGetValue(tool, out var sp)) ApplySpin(sp, _headingDeg);

            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            _shown = t;
        }

        /// <summary>
        /// Advances the current tool's spin once per frame. Working = the button is down or a stamp landed since the
        /// last frame (scripted and live strokes stamp without a button), held for spinReleaseGraceS; the speed eases
        /// up and coasts down like a motor with a heavy pad on it.
        /// </summary>
        void StepSpin(ToolId tool)
        {
            int frame = Time.frameCount;
            if (frame == _spinFrame) return;
            float dt = _spinFrame < 0 ? 0f : Mathf.Min(Time.deltaTime, 0.1f);
            _spinFrame = frame;
            AdvanceSpin(tool, dt);
        }

        void AdvanceSpin(ToolId tool, float dt)
        {
            if (!_hasSpinTool || tool != _spinTool)
            {
                // a new tool starts still; the stamp counter is taken as seen so the switch itself is not a stroke
                _spinTool = tool; _hasSpinTool = true;
                _spin01 = 0f; _sinceWork = 999f;
                _stampSeen = tools != null ? tools.StampCount : 0;
            }

            bool stamped = false;
            if (tools != null && tools.StampCount != _stampSeen) { _stampSeen = tools.StampCount; stamped = true; }
            bool working = stamped || (tools != null && tools.IsStroking) || _pinWorking;
            _sinceWork = working ? 0f : _sinceWork + dt;
            bool on = _sinceWork <= set.spinReleaseGraceS;

            float rate = Mathf.Max(0.01f, on ? set.spinUpHz : set.spinDownHz);
            _spin01 += ((on ? 1f : 0f) - _spin01) * (1f - Mathf.Exp(-rate * dt));
            if (!on && _spin01 < 1e-3f) _spin01 = 0f;

            float rpm = set.TryGet(tool, out var e) ? e.spinRpm : 0f;
            _stepDeg = rpm * 6f * _spin01 * dt;   // rev/min -> deg/s
            _spinDeg = Mathf.Repeat(_spinDeg + _stepDeg, 360f);
        }

        /// <summary>
        /// Turns the pivot, then fills the arc it swept this frame with copies of the part, never more than
        /// smearMaxStepDeg apart. A frame at full speed turns the brush by about two tuft spacings, and sampled once
        /// per frame that reads as a still or backwards brush (the wagon-wheel effect); the copies draw the swept
        /// volume instead, and appear one by one as the pad spins up.
        /// </summary>
        void ApplySpin(Spinner sp, float headingDeg)
        {
            sp.pivot.localRotation = Quaternion.Euler(0f, _spinDeg + headingDeg, 0f);
            int n = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(_stepDeg) / Mathf.Max(0.1f, set.smearMaxStepDeg)) - 1, 0, sp.parts.Length - 1);
            for (int i = 1; i < sp.parts.Length; i++)
            {
                bool show = i <= n;
                var c = sp.parts[i];
                if (show)
                {
                    var q = Quaternion.Euler(0f, -_stepDeg * i / (n + 1), 0f);   // where the part was earlier in this frame
                    float s = 1f - SmearShrink * i;                               // radial only (the part is never tilted about X/Z but for a flip)
                    c.localPosition = q * new Vector3(sp.basePos.x * s, sp.basePos.y, sp.basePos.z * s);
                    c.localRotation = q * sp.baseRot;
                    c.localScale = new Vector3(sp.baseScale.x * s, sp.baseScale.y, sp.baseScale.z * s);
                }
                if (c.gameObject.activeSelf != show) c.gameObject.SetActive(show);
            }
            _smearShown = n;
        }
    }
}
