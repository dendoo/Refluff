using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// Clip slots for the payout card and the shop (Docs/AUDIO.md 10), indexed by UiSfx. An empty slot is rendered from its
    /// UiSfxSynth recipe at startup; CarpetCleaning/Audio/Bake UI Sounds writes the recipes to Audio/Generated/ui_*.wav and
    /// fills the slots, and a real recording simply replaces a slot. Settings/UiSoundBank.asset is created by
    /// CarpetSceneBuilder only when missing, so assigned clips and tuned gains survive a Build Scene.
    /// </summary>
    [CreateAssetMenu(fileName = "UiSoundBank", menuName = "CarpetCleaning/UiSoundBank")]
    public sealed class UiSoundBank : ScriptableObject
    {
        [Range(0f, 2f)] public float masterGain = 0.8f;
        public AudioClip[] clips = new AudioClip[UiSfxSynth.Count];
        public float[] gains = DefaultGains();

        public static float[] DefaultGains()
        {
            // Tick, Clink, Whoosh, Thud, Stamp, KaChing, Sparkle, Buy, Deny, Open, Close
            return new[] { 0.35f, 0.7f, 0.35f, 0.6f, 0.9f, 0.85f, 0.45f, 0.75f, 0.6f, 0.35f, 0.3f };
        }

        public AudioClip Clip(UiSfx id)
        {
            int i = (int)id;
            return clips != null && i >= 0 && i < clips.Length ? clips[i] : null;
        }

        public float Gain(UiSfx id)
        {
            int i = (int)id;
            return gains != null && i >= 0 && i < gains.Length ? gains[i] : 1f;
        }

        /// <summary>Keeps the arrays at UiSfxSynth.Count after an enum grew.</summary>
        public void Normalize()
        {
            if (clips == null || clips.Length != UiSfxSynth.Count)
            {
                var c = new AudioClip[UiSfxSynth.Count];
                if (clips != null) for (int i = 0; i < c.Length && i < clips.Length; i++) c[i] = clips[i];
                clips = c;
            }
            if (gains == null || gains.Length != UiSfxSynth.Count)
            {
                var g = DefaultGains();
                if (gains != null) for (int i = 0; i < g.Length && i < gains.Length; i++) g[i] = gains[i];
                gains = g;
            }
        }
    }
}
