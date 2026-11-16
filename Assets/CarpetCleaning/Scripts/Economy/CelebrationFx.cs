using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// "Sparkling clean" glints on the pile of a carpet being handed over (Docs/ECONOMY.md 4.3). The particle system hangs
    /// under CarpetStage in LOCAL space, so the glints ride on the rug while it flies to the client; Clear() when the stage
    /// snaps home at Away, or a leftover glint would pop up where the carpet used to lie. Additive HDR (FxSparkle.shader)
    /// so the bloom catches them. Amount by result: a few for any carpet, gold for Spotless, cool white for Bone dry, more
    /// for a COMBO. Built and configured by CarpetSceneBuilder through Configure().
    /// </summary>
    [DefaultExecutionOrder(216)]
    public sealed class CelebrationFx : MonoBehaviour
    {
        public ParticleSystem sparkles;
        public Economy economy;
        public CarpetHandover handover;

        [Header("Burst")]
        public int anyCarpet = 24;
        public int spotless = 110;
        public int boneDry = 60;
        public float comboMultiplier = 1.5f;
        public Color warm = new Color(1f, 0.86f, 0.52f, 1f);
        public Color cool = new Color(0.78f, 0.9f, 1f, 1f);
        public Vector2 lifetimeS = new Vector2(0.35f, 0.8f);
        public Vector2 sizeM = new Vector2(0.03f, 0.08f);
        [Tooltip("Height of the glints above the carpet backing (m): just over the 20 mm pile, which writes depth.")]
        public Vector2 heightM = new Vector2(0.024f, 0.036f);

        bool _subscribed;
        Economy _subEco;
        CarpetHandover _subHo;
        uint _seed = 1;

        void Awake()
        {
            if (sparkles == null) sparkles = GetComponentInChildren<ParticleSystem>(true);
            if (economy == null) economy = UnityEngine.Object.FindAnyObjectByType<Economy>();
            if (handover == null) handover = GetComponent<CarpetHandover>();
            if (sparkles != null) { sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        }

        void OnEnable()
        {
            if (_subscribed) return;
            if (economy == null) economy = UnityEngine.Object.FindAnyObjectByType<Economy>();
            if (economy != null) economy.PayoutReady += OnPayout;
            if (handover != null) handover.StateChanged += OnState;
            _subEco = economy; _subHo = handover; _subscribed = true;
        }

        void OnDisable()
        {
            if (!_subscribed) return;
            if (_subEco != null) _subEco.PayoutReady -= OnPayout;
            if (_subHo != null) _subHo.StateChanged -= OnState;
            _subEco = null; _subHo = null; _subscribed = false;
        }

        void OnPayout(PayoutResult r)
        {
            if (handover != null && handover.State != HandoverState.FlyOut) return;   // a debug replay: no carpet is flying
            Emit(r);
        }

        void OnState(HandoverState s)
        {
            if (s == HandoverState.Away && sparkles != null) sparkles.Clear(true);
        }

        public int CountFor(PayoutResult r)
        {
            float n = anyCarpet + (r.perfectClean ? spotless : 0) + (r.perfectDry ? boneDry : 0);
            if (r.combo) n *= comboMultiplier;
            return Mathf.RoundToInt(n);
        }

        void Emit(PayoutResult r)
        {
            if (sparkles == null) return;
            if (!sparkles.isPlaying) sparkles.Play(true);
            int n = CountFor(r);
            float warmW = anyCarpet * 0.5f + (r.perfectClean ? spotless : 0);
            float coolW = anyCarpet * 0.5f + (r.perfectDry ? boneDry : 0);
            int nWarm = Mathf.RoundToInt(n * warmW / Mathf.Max(warmW + coolW, 1f));
            var size = CarpetSimulation.CarpetSizeM;
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                float x = (Rand() - 0.5f) * size.x * 0.96f;
                float z = (Rand() - 0.5f) * size.y * 0.96f;
                float y = Mathf.Lerp(heightM.x, heightM.y, Rand());
                ep.position = new Vector3(x, y, z);
                ep.velocity = new Vector3(0f, 0.02f + 0.04f * Rand(), 0f);
                ep.startLifetime = Mathf.Lerp(lifetimeS.x, lifetimeS.y, Rand());
                ep.startSize = Mathf.Lerp(sizeM.x, sizeM.y, Rand() * Rand());
                ep.rotation = Rand() * 90f;
                Color c = i < nWarm ? warm : cool;
                ep.startColor = c;
                sparkles.Emit(ep, 1);
            }
        }

        float Rand()
        {
            // xorshift: deterministic for DebugSeek, no allocation, independent of UnityEngine.Random's global state
            _seed ^= _seed << 13; _seed ^= _seed >> 17; _seed ^= _seed << 5;
            return (_seed & 0xFFFFFF) / 16777216f;
        }

        /// <summary>Test hook: the burst of `r` simulated to t seconds, then held (EconomyDebugApi.SparkleSeek).</summary>
        public string DebugSeek(PayoutResult r, float t)
        {
            if (sparkles == null) return "{\"error\":\"no particle system\"}";
            _seed = 12345u;
            sparkles.Clear(true);
            sparkles.Play(true);
            Emit(r);
            sparkles.Simulate(Mathf.Max(t, 0f), true, false, true);
            // Simulate is applied lazily (measured: the time only lands with the next call), so flush it now
            sparkles.Simulate(1e-4f, true, false, true);
            return "{\"emitted\":" + CountFor(r) + ",\"alive\":" + sparkles.particleCount + "}";
        }

        /// <summary>
        /// The whole particle system setup, applied by CarpetSceneBuilder (edit mode) so it lives next to the code that
        /// emits. Nothing emits by itself: no rate, no bursts, not on awake; Emit() places every glint.
        /// </summary>
        public static void Configure(ParticleSystem ps, Material mat)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.useUnscaledTime = true;       // presentation, like the flight itself
            main.maxParticles = 1024;
            main.startLifetime = 0.6f;
            main.startSpeed = 0f;
            main.startSize = 0.03f;
            main.gravityModifier = 0f;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f; em.rateOverDistance = 0f; em.SetBursts(new ParticleSystem.Burst[0]);
            var shape = ps.shape; shape.enabled = false;

            // twinkle: grow fast, hold, shrink; slow spin; fade at the very end
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 8f), new Keyframe(0.18f, 1f), new Keyframe(0.45f, 0.55f), new Keyframe(0.62f, 0.9f), new Keyframe(1f, 0f)));
            var rol = ps.rotationOverLifetime; rol.enabled = true; rol.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = ps.GetComponent<ParticleSystemRenderer>();
            if (r != null)
            {
                r.renderMode = ParticleSystemRenderMode.Billboard;
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.sortingFudge = -10f;
                r.minParticleSize = 0f;
                r.maxParticleSize = 0.2f;
            }
        }
    }
}
