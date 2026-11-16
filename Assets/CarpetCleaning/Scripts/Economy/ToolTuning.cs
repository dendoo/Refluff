using UnityEngine;

namespace CarpetCleaning
{
    public enum UpgradeStat { Power = 0, Area = 1 }

    /// <summary>
    /// Runtime multipliers of the tool upgrades (Docs/ECONOMY.md 2), relative to the factory tuning: 1 = the tool exactly as
    /// it shipped before the economy existed. Economy fills them from the save; everything that acts on a tool reads them:
    ///   AREA  - Effective(t) scales the stamp geometry (blade length, or disc radius + stamp spacing by sqrt(area)), so the
    ///           carpet, the floor, the debris capture, the squeegee plow, the cursor and the tool head all follow the stamp;
    ///   POWER - FillPower packs (P - 1) into the _ToolPower uniform of CarpetSim / FloorSim, where only the effect RATES of
    ///           each tool branch are multiplied (never the pile styling). An unset uniform reads 0 = factory power.
    /// Static on purpose: the simulations, the controller and the head view need it without a scene reference. Domain reload
    /// is disabled, so the arrays are reset in SubsystemRegistration; a scene without an Economy plays at factory tuning.
    /// </summary>
    public static class ToolTuning
    {
        public const int MaxTools = 16;   // 4 x float4 in HLSL, like _ToolSpacing

        static readonly float[] s_power = new float[MaxTools];
        static readonly float[] s_area = new float[MaxTools];

        /// <summary>
        /// Every multiplier reads 1 (factory tuning). CarpetDebugApi.Reset turns it on, so scripted regression recipes and the
        /// numbers in SESSION_LOG never depend on the player's save; EconomyDebugApi.LiveTuning(true) opts a script out.
        /// </summary>
        public static bool Neutral { get; private set; }

        /// <summary>Opt-out for scripted runs that want the saved levels (EconomyDebugApi.LiveTuning).</summary>
        public static bool ScriptsUseLiveTuning;

        /// <summary>Bumped on every change, so consumers can cache.</summary>
        public static int Version { get; private set; }

        static ToolTuning() { Fill(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Fill();
            Neutral = false;
            ScriptsUseLiveTuning = false;
            Version++;
        }

        static void Fill()
        {
            for (int i = 0; i < MaxTools; i++) { s_power[i] = 1f; s_area[i] = 1f; }
        }

        static int Index(ToolId t) { int i = (int)t; return i >= 0 && i < MaxTools ? i : -1; }

        public static float Power(ToolId t) { int i = Index(t); return Neutral || i < 0 ? 1f : s_power[i]; }
        public static float Area(ToolId t) { int i = Index(t); return Neutral || i < 0 ? 1f : s_area[i]; }

        public static void Set(ToolId t, float power, float area)
        {
            int i = Index(t);
            if (i < 0) return;
            s_power[i] = Mathf.Clamp(power, 0.05f, 20f);
            s_area[i] = Mathf.Clamp(area, 0.05f, 20f);
            Version++;
        }

        public static void SetNeutral(bool on)
        {
            if (Neutral == on) return;
            Neutral = on;
            Version++;
        }

        /// <summary>Called by CarpetDebugApi.Reset: scripted runs play at factory tuning unless they opted out.</summary>
        public static void EnterScriptedRun() { SetNeutral(!ScriptsUseLiveTuning); }

        /// <summary>
        /// The tool as it acts now. A blade (isBlade) grows along its length only: the swept area is length x travel.
        /// A disc grows its radius by sqrt(area), and its stamp spacing with it, so the stamps per frame and the dose per
        /// stamp stay as they were; a wider disc still stays over each cell longer (2R / v), which is an honest bonus.
        /// </summary>
        public static ToolDefinition Effective(ToolId t)
        {
            var d = ToolTable.Get(t);
            float a = Area(t);
            if (Mathf.Approximately(a, 1f)) return d;
            if (d.isBlade) d.lengthMm *= a;
            else
            {
                float k = Mathf.Sqrt(a);
                d.radiusMm *= k;
                d.spacingMm *= k;
            }
            return d;
        }

        /// <summary>
        /// Uniform scale of the 3D head: preserve the authored proportions, including collars and round details.
        /// Match the effective working width: blades grow by area (length x travel), discs by sqrt(area).
        /// The normalized prefab's bottom pivot keeps surface contact fixed as its height and depth grow.
        /// </summary>
        public static Vector3 HeadScale(ToolId t)
        {
            float a = Area(t);
            float k = ToolTable.Get(t).isBlade ? a : Mathf.Sqrt(a);
            return Vector3.one * k;
        }

        /// <summary>(P - 1) per ToolId, 4 per element, for the _ToolPower uniform (so an unset uniform = factory power).</summary>
        public static void FillPower(Vector4[] dst)
        {
            for (int i = 0; i < dst.Length; i++) dst[i] = Vector4.zero;
            for (int i = 0; i < ToolTable.All.Length && i < MaxTools; i++)
            {
                float p = Power((ToolId)i) - 1f;
                var v = dst[i >> 2]; v[i & 3] = p; dst[i >> 2] = v;
            }
        }

        /// <summary>Effective stamp spacing (mm) per ToolId, 4 per element: K_Fiber's swept pile set must see the spacing the stamps really have.</summary>
        public static void FillSpacing(Vector4[] dst)
        {
            for (int i = 0; i < dst.Length; i++) dst[i] = Vector4.zero;
            for (int i = 0; i < ToolTable.All.Length && i < MaxTools; i++)
            {
                var v = dst[i >> 2]; v[i & 3] = Effective((ToolId)i).spacingMm; dst[i >> 2] = v;
            }
        }
    }
}
