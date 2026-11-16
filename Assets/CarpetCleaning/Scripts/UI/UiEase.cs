using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>Easing curves for the C#-driven HUD timelines (payout card, shop). t in [0, 1] (clamped).</summary>
    public static class UiEase
    {
        public static float Clamp01(float t) { return t < 0f ? 0f : (t > 1f ? 1f : t); }
        public static float OutCubic(float t) { t = Clamp01(t); float u = 1f - t; return 1f - u * u * u; }
        public static float OutQuart(float t) { t = Clamp01(t); float u = 1f - t; return 1f - u * u * u * u; }
        public static float InQuad(float t) { t = Clamp01(t); return t * t; }
        public static float InCubic(float t) { t = Clamp01(t); return t * t * t; }
        public static float InOutSine(float t) { t = Clamp01(t); return 0.5f - 0.5f * Mathf.Cos(Mathf.PI * t); }

        /// <summary>Overshoots past 1 and settles: the "lands with a bounce" of the card and the pips.</summary>
        public static float OutBack(float t, float s = 1.70158f)
        {
            t = Clamp01(t) - 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }

        /// <summary>A punch: 1 at rest, jumps to 1 + amp and rings back down with a quick damped wobble over [0, 1].</summary>
        public static float Punch(float t, float amp)
        {
            if (t <= 0f || t >= 1f) return 1f;
            float u = 1f - t;
            return 1f + amp * u * u * Mathf.Cos(t * 9f);
        }

        /// <summary>0 -> 1 -> 0 over [0, 1] (sine bell).</summary>
        public static float Bell(float t) { t = Clamp01(t); return Mathf.Sin(Mathf.PI * t); }

        /// <summary>Progress of the window [start, start + dur] at time t, 0..1.</summary>
        public static float Window(float t, float start, float dur) { return Clamp01((t - start) / Mathf.Max(dur, 1e-4f)); }
    }
}
