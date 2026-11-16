using System;

namespace CarpetCleaning
{
    public enum UiSfx { Tick = 0, Clink = 1, Whoosh = 2, Thud = 3, Stamp = 4, KaChing = 5, Sparkle = 6, Buy = 7, Deny = 8, Open = 9, Close = 10 }

    /// <summary>
    /// Procedural placeholders for the payout card and the shop (Docs/AUDIO.md 10, Docs/ECONOMY.md 4.4). Pure maths, no
    /// engine calls, so it runs on a worker thread (UiAudio) or in the Editor baker (UiSoundBaker -> Audio/Generated/ui_*.wav).
    /// Follows the project's synthesis rules: noise first; every tone is a NOISE-EXCITED modal resonator (a struck coin or
    /// bell), never a sine oscillator; no LFOs; band-pass gain is normalised by 1/Q (an SVF band output has gain Q at centre).
    /// A real recording dropped into the UiSoundBank slot replaces a recipe.
    /// </summary>
    public static class UiSfxSynth
    {
        public const int SampleRate = 44100;
        public const int Count = 11;

        public static string Name(UiSfx id) { return "ui_" + id.ToString().ToLowerInvariant(); }

        public static float[] Render(UiSfx id)
        {
            switch (id)
            {
                case UiSfx.Tick: return Tick();
                case UiSfx.Clink: return Clink(2350f, 0xC01Au, 1f);
                case UiSfx.Whoosh: return Whoosh(0.34f, 520f, 2300f, 0x1234u);
                case UiSfx.Thud: return Thud();
                case UiSfx.Stamp: return Stamp();
                case UiSfx.KaChing: return KaChing();
                case UiSfx.Sparkle: return Sparkle();
                case UiSfx.Buy: return Buy();
                case UiSfx.Deny: return Deny();
                case UiSfx.Open: return Whoosh(0.24f, 450f, 1800f, 0x0FE1u);
                case UiSfx.Close: return Whoosh(0.22f, 1700f, 420f, 0xC105u);
                default: return new float[1];
            }
        }

        // ------------------------------------------------------------------ recipes
        static float[] Tick()
        {
            // a counter wheel clicking over: a 0.3 ms-attack noise tick, band-limited, with a faint high ring
            var y = New(0.05f);
            var rng = new Rng(0x71C4u);
            var bp = new Svf(3200f, 1.5f); var ring = new Mode(4300f, 0.03f);
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)SampleRate;
                float e = Ar(t, 0.0003f, 0.003f) * rng.Noise();
                y[i] = bp.Band(e) + 0.25f * ring.Tick(e);
            }
            return Finish(y, 0.9f);
        }

        /// <summary>A coin struck on a hard surface: five inharmonic disc modes, plus a small bounce 45 ms later.</summary>
        static float[] Clink(float f0, uint seed, float bounce)
        {
            var y = New(0.62f);
            var rng = new Rng(seed);
            float[] ratio = { 1f, 1.47f, 2.09f, 2.56f, 3.01f };
            float[] amp = { 1f, 0.7f, 0.55f, 0.35f, 0.25f };
            // short rings: a coin on a counter "clinks", a long ring reads as a bell tone (the analyzer's tonal / st90)
            float[] t60 = { 0.32f, 0.25f, 0.19f, 0.15f, 0.11f };
            var modes = new Mode[ratio.Length];
            for (int k = 0; k < modes.Length; k++) modes[k] = new Mode(f0 * ratio[k] * (1f + (rng.Next() - 0.5f) * 0.01f), t60[k]);
            var click = new Svf(5200f, 1.2f);
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)SampleRate;
                float n = rng.Noise();
                float e = Ar(t, 0.0002f, 0.0007f) * n + bounce * 0.35f * Ar(t - 0.045f, 0.0002f, 0.0006f) * n;
                float s = 0f;
                for (int k = 0; k < modes.Length; k++) s += amp[k] * modes[k].Tick(e);
                y[i] = s + 0.9f * click.Band(e);
            }
            return Finish(y, 0.85f);
        }

        static float[] Whoosh(float len, float fFrom, float fTo, uint seed)
        {
            var y = New(len);
            var rng = new Rng(seed);
            var pink = new Pink();
            var bp = new Svf(fFrom, 0.8f);
            for (int i = 0; i < y.Length; i++)
            {
                float u = i / (float)y.Length;
                bp.SetFreq(fFrom * (float)Math.Pow(fTo / fFrom, u));
                float env = (float)Math.Sin(Math.PI * Math.Pow(u, 0.7));
                y[i] = bp.Band(pink.Next(rng.Noise())) * env * env;
            }
            return Finish(y, 0.55f);
        }

        static float[] Thud()
        {
            var y = New(0.24f);
            var rng = new Rng(0x7D0Du);
            // mostly a low-passed noise burst (a felt "tok"); the body modes are damped fast so no hum is left
            var body = new Mode(105f, 0.08f); var body2 = new Mode(172f, 0.05f);
            var lp = new Svf(380f, 0.7f);
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)SampleRate;
                float n = rng.Noise();
                float e = Ar(t, 0.0005f, 0.004f) * n;
                y[i] = 0.7f * body.Tick(e) + 0.35f * body2.Tick(e) + 1.4f * lp.Low(Ar(t, 0.001f, 0.022f) * n);
            }
            return Finish(y, 0.8f);
        }

        static float[] Stamp()
        {
            var y = New(1.0f);
            var rng = new Rng(0x57A3u);
            // rubber stamp on a desk: a low-passed noise thump with a fast-damped body, a broadband slap, then glitter
            var thump = new Mode(78f, 0.1f); var thump2 = new Mode(131f, 0.06f);
            var body = new Svf(170f, 0.7f);
            var slap = new Svf(1400f, 0.7f);
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)SampleRate;
                float n = rng.Noise();
                float hit = Ar(t, 0.0004f, 0.006f) * n;
                float sl = Ar(t, 0.0003f, 0.012f) * n;
                y[i] = 0.6f * thump.Tick(hit) + 0.3f * thump2.Tick(hit) + 1.6f * body.Low(Ar(t, 0.001f, 0.045f) * n) + 0.7f * slap.Band(sl);
            }
            AddGlitter(y, 0x57A4u, 0.03f, 0.45f, 40, 0.35f);
            return Finish(y, 0.9f);
        }

        static float[] KaChing()
        {
            var y = New(1.3f);
            var rng = new Rng(0xCA5Cu);
            var ka = new Svf(2500f, 2f);
            float f0 = 2600f;
            float[] ratio = { 0.5f, 1f, 1.183f, 1.506f, 2.0f, 2.514f, 2.662f, 3.011f };
            float[] amp = { 0.25f, 1f, 0.6f, 0.5f, 0.45f, 0.3f, 0.25f, 0.2f };
            var bell = new Mode[ratio.Length];
            for (int k = 0; k < bell.Length; k++) bell[k] = new Mode(f0 * ratio[k], 0.6f - 0.05f * k);
            var coin = Clink(3100f, 0xC0C0u, 0.6f);
            int coinAt = (int)(0.09f * SampleRate);
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)SampleRate;
                float n = rng.Noise();
                float clicks = Ar(t, 0.0003f, 0.004f) * n + 0.8f * Ar(t - 0.035f, 0.0003f, 0.004f) * n;
                float strike = Ar(t - 0.06f, 0.0002f, 0.0012f) * n;
                float s = 0f;
                for (int k = 0; k < bell.Length; k++) s += amp[k] * bell[k].Tick(strike);
                float c = i >= coinAt && i - coinAt < coin.Length ? coin[i - coinAt] : 0f;
                y[i] = 0.9f * ka.Band(clicks) + s * 0.55f + 0.4f * c;
            }
            return Finish(y, 0.9f);
        }

        /// <summary>Glitter, not chimes: a scatter of band-limited NOISE grains (non-resonant, Q 2), bright and thinning out.</summary>
        static float[] Sparkle()
        {
            var y = New(0.6f);
            AddGlitter(y, 0x5A4Bu, 0f, 0.42f, 44, 1f);
            return Finish(y, 0.7f);
        }

        /// <summary>Mixes `count` noise grains (1.5-5.5 ms, 5-11 kHz, Q 2) into y between start and start + spread seconds.</summary>
        static void AddGlitter(float[] y, uint seed, float start, float spread, int count, float gain)
        {
            var rng = new Rng(seed);
            var at = new float[count]; var len = new float[count]; var amp = new float[count]; var bp = new Svf[count];
            for (int k = 0; k < count; k++)
            {
                float u = (float)Math.Pow(rng.Next(), 1.6);   // denser at the start, thinning out
                at[k] = start + spread * u;
                len[k] = 0.0015f + 0.004f * rng.Next();
                amp[k] = (0.35f + 0.65f * rng.Next()) * (1f - 0.7f * u);
                bp[k] = new Svf(5000f + 6000f * rng.Next(), 2f);
            }
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)SampleRate;
                float n = rng.Noise();
                float s = 0f;
                for (int k = 0; k < count; k++) s += amp[k] * bp[k].Band(Ar(t - at[k], 0.0003f, len[k]) * n);
                y[i] += gain * s;
            }
        }

        static float[] Buy()
        {
            var up = Whoosh(0.22f, 420f, 3000f, 0xB0B0u);
            var clink = Clink(2800f, 0xB1B1u, 0f);
            var y = New(0.75f);
            int at = (int)(0.17f * SampleRate);
            for (int i = 0; i < y.Length; i++)
            {
                float a = i < up.Length ? up[i] : 0f;
                float b = i >= at && i - at < clink.Length ? clink[i - at] : 0f;
                y[i] = 0.7f * a + b;
            }
            return Finish(y, 0.85f);
        }

        static float[] Deny()
        {
            var y = New(0.32f);
            var rng = new Rng(0xDE11u);
            var body = new Mode(180f, 0.07f); var bp = new Svf(300f, 1.2f);
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)SampleRate;
                float n = rng.Noise();
                float e = Ar(t, 0.0005f, 0.01f) * n + 0.85f * Ar(t - 0.11f, 0.0005f, 0.01f) * n;
                y[i] = body.Tick(e) + 0.6f * bp.Band(e);
            }
            return Finish(y, 0.75f);
        }

        // ------------------------------------------------------------------ DSP helpers
        static float[] New(float seconds) { return new float[Math.Max(1, (int)(seconds * SampleRate))]; }

        /// <summary>Attack-release envelope: linear attack, exponential decay, 0 before t = 0.</summary>
        static float Ar(float t, float attack, float decay)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / attack;
            return (float)Math.Exp(-(t - attack) / decay);
        }

        /// <summary>Peak-normalise, then a 5 ms fade at the end so a one-shot never clicks.</summary>
        static float[] Finish(float[] y, float peak)
        {
            float m = 1e-9f;
            for (int i = 0; i < y.Length; i++) { float a = Math.Abs(y[i]); if (a > m) m = a; }
            float g = peak / m;
            int fade = Math.Min(y.Length, (int)(0.005f * SampleRate));
            for (int i = 0; i < y.Length; i++)
            {
                float f = 1f;
                int left = y.Length - 1 - i;
                if (left < fade) f = left / (float)fade;
                y[i] *= g * f;
            }
            return y;
        }

        struct Rng
        {
            uint _s;
            public Rng(uint seed) { _s = seed == 0 ? 0x9E3779B9u : seed; }
            public float Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return (_s & 0xFFFFFF) / 16777216f; }
            public float Noise() { return Next() * 2f - 1f; }
        }

        /// <summary>Paul Kellet's economy pink filter.</summary>
        sealed class Pink
        {
            float b0, b1, b2;
            public float Next(float white)
            {
                b0 = 0.99765f * b0 + white * 0.0990460f;
                b1 = 0.96300f * b1 + white * 0.2965164f;
                b2 = 0.57000f * b2 + white * 1.0526913f;
                return (b0 + b1 + b2 + white * 0.1848f) * 0.25f;
            }
        }

        /// <summary>Zavalishin TPT state-variable filter. Band() is normalised by 1/Q (unity gain at centre).</summary>
        sealed class Svf
        {
            float _g, _k, _a1, _a2, _a3, _ic1, _ic2;
            public Svf(float fc, float q) { _k = 1f / Math.Max(q, 0.05f); SetFreq(fc); }
            public void SetFreq(float fc)
            {
                fc = Math.Min(fc, SampleRate * 0.45f);
                _g = (float)Math.Tan(Math.PI * fc / SampleRate);
                _a1 = 1f / (1f + _g * (_g + _k)); _a2 = _g * _a1; _a3 = _g * _a2;
            }
            void Step(float x, out float band, out float low)
            {
                float v3 = x - _ic2;
                float v1 = _a1 * _ic1 + _a2 * v3;
                float v2 = _ic2 + _a2 * _ic1 + _a3 * v3;
                _ic1 = 2f * v1 - _ic1; _ic2 = 2f * v2 - _ic2;
                band = v1; low = v2;
            }
            public float Band(float x) { Step(x, out float b, out _); return b * _k; }
            public float Low(float x) { Step(x, out _, out float l); return l; }
        }

        /// <summary>Two-pole modal resonator ringing at f with the given T60, driven by the (noise) excitation.</summary>
        sealed class Mode
        {
            readonly float _c1, _c2, _gain;
            float _y1, _y2;
            public Mode(float f, float t60)
            {
                f = Math.Min(f, SampleRate * 0.45f);
                double r = Math.Exp(-6.907755 / (Math.Max(t60, 0.002f) * SampleRate));
                double w = 2.0 * Math.PI * f / SampleRate;
                _c1 = (float)(2.0 * r * Math.Cos(w));
                _c2 = (float)(-r * r);
                _gain = (float)(1.0 - r);   // keeps short and long modes in a similar range before the final normalise
            }
            public float Tick(float x)
            {
                float y = _gain * x + _c1 * _y1 + _c2 * _y2;
                _y2 = _y1; _y1 = y;
                return y;
            }
        }
    }
}
