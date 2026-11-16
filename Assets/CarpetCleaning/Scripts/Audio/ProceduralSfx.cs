using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// Placeholder tool sounds synthesised in C# (Docs/AUDIO.md 3). Nothing here is sampled from anywhere,
    /// so the project carries no audio licence; a recording dropped into a ToolSoundBank slot replaces the
    /// matching generator.
    ///
    /// Everything is built from NOISE. The first version used sine stacks for motors, sine blips for bristle
    /// taps and water spatter, and periodic LFOs for movement, and it measured (and sounded) like a sci-fi
    /// soundtrack: 34-36 dB tonal peaks, 17-24 dB of clean periodic wobble, bleeps in every short frame.
    /// Real friction, air and water are broadband, so the rules now are:
    ///   - transients are filtered noise bursts, never sine grains, with a heavy-tailed amplitude
    ///     distribution (many faint, few loud), which is what crackle and crunch actually are;
    ///   - machine tones are narrow-band NOISE (a high-Q filter on noise), which drifts in pitch and level on
    ///     its own the way a real impeller whine does, instead of a locked oscillator;
    ///   - movement is slow random modulation, never a periodic LFO;
    ///   - the only tonal element is the liquid bubble (Minnaert resonance with van den Doel's rising chirp
    ///     and damping), sparse and a few milliseconds long, because that IS the sound of water.
    /// Docs/AUDIO.md 3 has the before/after measurements.
    ///
    /// Loops are made seamless by rendering n+fade samples and folding the tail back over the head with an
    /// equal-power crossfade (Finish). All of this is pure maths; only ToClip touches the engine, so the rest
    /// may run on a worker thread.
    /// </summary>
    public static class ProceduralSfx
    {
        public const int SampleRate = 44100;
        const float TwoPi = 6.2831853f;

        // ------------------------------------------------------------------ primitives
        /// <summary>xorshift32: deterministic, so the same tool sounds the same on every machine and every run.</summary>
        sealed class Rng
        {
            uint _s;
            public Rng(uint seed) { _s = seed == 0u ? 2463534242u : seed; }
            public uint NextU() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return _s; }
            public float Unit() { return NextU() * (1f / 4294967296f); }
            public float Bi() { return Unit() * 2f - 1f; }
            public float Range(float a, float b) { return a + (b - a) * Unit(); }
            /// <summary>Log-uniform: equal odds per octave, how frequencies and sizes spread in nature.</summary>
            public float LogRange(float a, float b) { return a * Mathf.Pow(b / a, Unit()); }
        }

        /// <summary>Chamberlin state-variable filter; one instance per band, fed sample by sample.</summary>
        sealed class Svf
        {
            float _low, _band;
            readonly float _f, _q;
            public Svf(float freqHz, float q)
            {
                _q = 1f / Mathf.Max(q, 0.05f);
                float f = 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp(freqHz, 10f, SampleRate * 0.45f) / SampleRate);
                // Jury test on the 2x2 state matrix gives fq < 2 and f^2 + 2fq < 4, i.e. f < -q + sqrt(q^2 + 4).
                // Low-Q bands hit that limit well below Nyquist (Q 0.35 is unstable above ~4.5 kHz), so clamp
                // with a margin instead of letting a wide band blow up.
                _f = Mathf.Min(f, 0.95f * (-_q + Mathf.Sqrt(_q * _q + 4f)));
            }
            void Step(float x, out float low, out float band, out float high)
            {
                high = x - _low - _q * _band;
                _band += _f * high;
                _low += _f * _band;
                low = _low; band = _band;
            }
            public float Low(float x) { Step(x, out float l, out _, out _); return l; }
            public float Band(float x) { Step(x, out _, out float b, out _); return b; }
            public float High(float x) { Step(x, out _, out _, out float h); return h; }
        }

        /// <summary>
        /// RBJ biquad low-pass (direct form I). Stable and accurate right up to Nyquist, unlike the Chamberlin SVF,
        /// whose stability clamp makes a low-Q band useless above ~7 kHz. Used for the rattle's final band limit.
        /// </summary>
        sealed class BiquadLp
        {
            readonly float _b0, _b1, _b2, _a1, _a2;
            float _x1, _x2, _y1, _y2;
            public BiquadLp(float cutoffHz, float q)
            {
                float w = TwoPi * Mathf.Clamp(cutoffHz, 20f, SampleRate * 0.49f) / SampleRate;
                float cs = Mathf.Cos(w), alpha = Mathf.Sin(w) / (2f * Mathf.Max(q, 0.1f));
                float a0 = 1f + alpha;
                _b0 = (1f - cs) * 0.5f / a0; _b1 = (1f - cs) / a0; _b2 = _b0;
                _a1 = -2f * cs / a0; _a2 = (1f - alpha) / a0;
            }
            public float Low(float x)
            {
                float y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
                _x2 = _x1; _x1 = x; _y2 = _y1; _y1 = y;
                return y;
            }
        }

        sealed class OnePole
        {
            float _y; readonly float _a;
            public OnePole(float cutoffHz) { _a = 1f - Mathf.Exp(-TwoPi * Mathf.Clamp(cutoffHz, 0.05f, SampleRate * 0.45f) / SampleRate); }
            public float Low(float x) { _y += (x - _y) * _a; return _y; }
            public float High(float x) { return x - Low(x); }
        }

        /// <summary>Paul Kellet's economy pink filter (-3 dB/oct), tuned for 44.1 kHz. Air and water sit close to pink.</summary>
        sealed class Pink
        {
            float _b0, _b1, _b2;
            public float Next(float white)
            {
                _b0 = 0.99765f * _b0 + white * 0.0990460f;
                _b1 = 0.96300f * _b1 + white * 0.2965164f;
                _b2 = 0.57000f * _b2 + white * 1.0526913f;
                return (_b0 + _b1 + _b2 + white * 0.1848f) * 0.25f;
            }
        }

        /// <summary>
        /// Slow random level drift: noise through two cascaded low-passes, scaled to +-depth. Replaces every
        /// periodic LFO of the first version; a periodic wobble is the single most synthetic thing a loop can do.
        /// </summary>
        sealed class Drift
        {
            readonly OnePole _a, _b; readonly float _depth, _gain;
            public Drift(float rateHz, float depth)
            {
                _a = new OnePole(rateHz); _b = new OnePole(rateHz);
                _depth = depth;
                // two one-poles at rateHz leave roughly sqrt(rate/SR) of the white level; normalise it back to ~1
                _gain = Mathf.Sqrt(SampleRate / Mathf.Max(rateHz, 0.01f)) * 0.9f;
            }
            public float Next(Rng rng) { return 1f + Mathf.Clamp(_b.Low(_a.Low(rng.Bi())) * _gain, -1f, 1f) * _depth; }
        }

        static float[] Alloc(float seconds, out int n, out int fade)
        {
            n = Mathf.Max(1024, Mathf.RoundToInt(seconds * SampleRate));
            fade = Mathf.Min(n / 4, Mathf.RoundToInt(0.06f * SampleRate));
            return new float[n + fade];
        }

        /// <summary>Folds the crossfade tail over the head, trims, removes DC and normalises.</summary>
        static float[] Finish(float[] buf, int n, int fade, float peak)
        {
            for (int i = 0; i < fade; i++)
            {
                float t = (i + 0.5f) / fade;
                float wHead = Mathf.Sin(t * Mathf.PI * 0.5f);      // 0 -> 1 across the fade
                float wTail = Mathf.Cos(t * Mathf.PI * 0.5f);      // 1 -> 0, equal power
                buf[i] = buf[i] * wHead + buf[n + i] * wTail;
            }
            var data = new float[n];
            System.Array.Copy(buf, data, n);
            return Normalise(data, peak);
        }

        /// <summary>DC removal and peak normalisation.</summary>
        static float[] Normalise(float[] data, float peak)
        {
            float mean = 0f;
            for (int i = 0; i < data.Length; i++) mean += data[i];
            mean /= data.Length;
            float max = 1e-6f;
            for (int i = 0; i < data.Length; i++) { data[i] -= mean; float a = Mathf.Abs(data[i]); if (a > max) max = a; }
            float g = peak / max;
            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i] * g, -1f, 1f);
            return data;
        }

        /// <summary>
        /// Samples -> AudioClip. The ONLY part of this file that touches the engine, so everything above it may
        /// run on a worker thread (ToolAudio generates there; a 2 s loop costs tens of ms).
        /// </summary>
        public static AudioClip ToClip(string name, float[] data)
        {
            if (data == null || data.Length == 0) return null;
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            // referenced only from a private field at runtime, so an editor asset refresh would otherwise eat it
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return clip;
        }

        // ------------------------------------------------------------------ event layers
        /// <summary>
        /// Filtered noise bursts: bristle ticks, grit crackle, foam fizz, spray spatter, rubber chatter.
        /// Each burst is white noise under a 0.3 ms attack / exponential decay, through its own low-Q band
        /// centred log-randomly within +-half an octave of centreHz. Amplitude is amp * u^tail: tail 1 is
        /// even, tail 3-4 gives the many-faint-few-loud spread of real crackle. A burst running off the end
        /// wraps to the head, where the loop continues.
        /// </summary>
        static void AddBursts(float[] buf, int n, int fade, Rng rng, float perSecond, float centreHz, float q,
                              float decayMs, float amp, float tail)
        {
            int total = n + fade;
            int count = Mathf.Max(1, Mathf.RoundToInt(perSecond * total / SampleRate));
            float attack = 0.0003f * SampleRate;
            for (int g = 0; g < count; g++)
            {
                int start = (int)(rng.Unit() * total);
                float decay = decayMs * rng.Range(0.6f, 1.5f) * 0.001f;
                float a = amp * Mathf.Pow(rng.Unit(), tail);
                if (a < amp * 0.002f) continue;                // inaudible, skip the work
                var band = new Svf(centreHz * rng.LogRange(0.7f, 1.4f), q);
                int len = Mathf.Min(total, Mathf.RoundToInt(decay * 6f * SampleRate));
                float kDecay = 1f / (decay * SampleRate);
                for (int i = 0; i < len; i++)
                {
                    int k = start + i;
                    if (k >= total) k -= total;
                    float env = Mathf.Min(1f, i / attack) * Mathf.Exp(-i * kDecay);
                    buf[k] += band.Band(rng.Bi()) * env * a;
                }
            }
        }

        /// <summary>
        /// Air bubbles in liquid, the one tonal thing water really does. Minnaert resonance f0 = 3/r (r in m),
        /// damping d = 0.13 f0 + 0.0072 f0^1.5 per second and a rising chirp f(t) = f0 (1 + 0.1 d t), after
        /// K. van den Doel, "Physically-based models for liquid sounds" (2004). Radii log-uniform; bigger
        /// bubbles are louder and lower.
        /// </summary>
        static void AddBubbles(float[] buf, int n, int fade, Rng rng, float perSecond, float rMinMm, float rMaxMm, float amp)
        {
            int total = n + fade;
            int count = Mathf.Max(1, Mathf.RoundToInt(perSecond * total / SampleRate));
            for (int g = 0; g < count; g++)
            {
                int start = (int)(rng.Unit() * total);
                float rMm = rng.LogRange(rMinMm, rMaxMm);
                float f0 = 3f / (rMm * 0.001f);
                if (f0 > SampleRate * 0.4f) continue;
                float d = 0.13f * f0 + 0.0072f * Mathf.Pow(f0, 1.5f);
                float a = amp * (rMm / rMaxMm) * rng.Range(0.3f, 1f);
                int len = Mathf.Min(total, Mathf.RoundToInt(6f / d * SampleRate));
                float phase = 0f;
                for (int i = 0; i < len; i++)
                {
                    float t = i / (float)SampleRate;
                    float f = f0 * (1f + 0.1f * d * t);
                    phase += TwoPi * f / SampleRate;
                    int k = start + i;
                    if (k >= total) k -= total;
                    buf[k] += Mathf.Sin(phase) * Mathf.Exp(-d * t) * a;
                }
            }
        }

        // ------------------------------------------------------------------ loops
        /// <summary>
        /// Canister vacuum: a pink roar of rushing air is most of it; the impeller whine is a narrow band of
        /// noise at the blade-pass frequency (plus a weaker 2nd harmonic) sitting ~20 dB under the roar;
        /// a low motor body and a slow random level drift finish it. No oscillators anywhere.
        /// </summary>
        /// <summary>Whine level against the roar; tuned by measurement (Docs/AUDIO.md 3), not by ear.</summary>
        const float WhineGain = 14f;

        public static float[] MotorLoop(float bladeHz, float bodyHz, float roarHz, float whine, float body, uint seed, float seconds = 2f)
        {
            var buf = Alloc(seconds, out int n, out int fade);
            var rng = new Rng(seed);
            var pink = new Pink();
            var roarBand = new Svf(roarHz, 0.45f);
            var roarHp = new OnePole(140f);
            // Q 16: wide enough that the whine is audibly 'noisy', like a real impeller, not a sine
            var tone1 = new Svf(bladeHz, 16f);
            var tone2 = new Svf(bladeHz * 2.01f, 18f);
            var bodyBand = new Svf(bodyHz, 6f);
            var drift = new Drift(0.9f, 0.05f);
            var toneDrift = new Drift(2.2f, 0.35f);

            for (int i = 0; i < n + fade; i++)
            {
                float w = rng.Bi();
                float roar = roarHp.High(roarBand.Band(pink.Next(w)) + pink.Next(rng.Bi()) * 0.35f);
                float w2 = rng.Bi();
                // The Chamberlin band output has gain Q at its centre, so the bands are divided by Q first; the
                // first voicing did not, and a Q-38 whine at 38x drowned the roar and read as a siren.
                float tone = (tone1.Band(w2) / 16f + tone2.Band(rng.Bi()) / 18f * 0.35f) * whine * WhineGain * toneDrift.Next(rng);
                float low = bodyBand.Band(rng.Bi()) * body * 1.6f;
                buf[i] = (roar * 2.2f + tone + low) * drift.Next(rng);
            }
            return Finish(buf, n, fade, 0.85f);
        }

        /// <summary>Air mover / dryer: broad pink air with a faint fan-blade band and random gusting.</summary>
        public static float[] AirLoop(float fanHz, float lowCutHz, float highCutHz, uint seed, float seconds = 2f)
        {
            var buf = Alloc(seconds, out int n, out int fade);
            var rng = new Rng(seed);
            var pink = new Pink();
            var hp = new OnePole(lowCutHz);
            var lp1 = new OnePole(highCutHz);
            var lp2 = new OnePole(highCutHz * 1.4f);
            var fan = new Svf(fanHz, 18f);
            var gust = new Drift(0.7f, 0.07f);

            for (int i = 0; i < n + fade; i++)
            {
                float air = lp2.Low(lp1.Low(hp.High(pink.Next(rng.Bi()))));
                float blade = fan.Band(rng.Bi()) * 0.35f;
                buf[i] = (air * 3f + blade) * gust.Next(rng);
            }
            return Finish(buf, n, fade, 0.8f);
        }

        /// <summary>
        /// Hose spray landing on pile: a broad pink "shhh", a dense sizzle of tiny impacts, the odd bubble,
        /// and a restless random flutter of the jet.
        /// </summary>
        public static float[] WaterJetLoop(float centreHz, float spatter, float bubbles, uint seed, float seconds = 2f)
        {
            var buf = Alloc(seconds, out int n, out int fade);
            var rng = new Rng(seed);
            var pink = new Pink();
            var band = new Svf(centreHz, 0.4f);
            var hp = new OnePole(280f);
            var flutter = new Drift(5f, 0.12f);

            for (int i = 0; i < n + fade; i++)
                buf[i] = hp.High(band.Band(pink.Next(rng.Bi())) * 1.6f + pink.Next(rng.Bi()) * 0.3f) * flutter.Next(rng);
            AddBursts(buf, n, fade, rng, 2400f * spatter, 3200f, 0.6f, 1.4f, 0.10f, 3f);
            AddBursts(buf, n, fade, rng, 120f * spatter, 1100f, 0.7f, 5f, 0.14f, 3f);
            AddBubbles(buf, n, fade, rng, 40f * bubbles, 0.8f, 4f, 0.05f);
            return Finish(buf, n, fade, 0.8f);
        }

        /// <summary>Aerosol / trigger sprayer: high hiss from the nozzle, optional foam fizz of tiny pops.</summary>
        public static float[] HissLoop(float lowCutHz, float centreHz, float fizz, uint seed, float seconds = 2f)
        {
            var buf = Alloc(seconds, out int n, out int fade);
            var rng = new Rng(seed);
            // shelves only: a nozzle hiss is flat above its cut-in, and a wide SVF band this high is unstable
            var hp1 = new OnePole(lowCutHz);
            var hp2 = new OnePole(lowCutHz);
            var lp = new OnePole(centreHz * 2f);
            var jitter = new Drift(9f, 0.08f);

            for (int i = 0; i < n + fade; i++)
                buf[i] = lp.Low(hp2.High(hp1.High(rng.Bi()))) * jitter.Next(rng);
            if (fizz > 0f) AddBursts(buf, n, fade, rng, 900f * fizz, 4200f, 0.7f, 0.6f, 0.35f, 4f);
            return Finish(buf, n, fade, 0.72f);
        }

        /// <summary>
        /// Stiff brush through pile: a wide "shh" bed, a dense spray of individual bristle ticks, and slower
        /// swishes where tufts drag through together.
        /// </summary>
        public static float[] ScrubLoop(uint seed, float seconds = 2f)
        {
            var buf = Alloc(seconds, out int n, out int fade);
            var rng = new Rng(seed);
            var pink = new Pink();
            var band = new Svf(1900f, 0.45f);
            var hp = new OnePole(260f);
            var drift = new Drift(3f, 0.15f);

            for (int i = 0; i < n + fade; i++) buf[i] = hp.High(band.Band(pink.Next(rng.Bi()))) * 1.4f * drift.Next(rng);
            AddBursts(buf, n, fade, rng, 1800f, 2600f, 0.6f, 1.1f, 0.22f, 3f);   // single bristles
            AddBursts(buf, n, fade, rng, 14f, 950f, 0.5f, 28f, 0.25f, 1.5f);     // tufts dragging together
            return Finish(buf, n, fade, 0.78f);
        }

        /// <summary>Blade through dried crust: a dry scrape bed with heavy-tailed crackle and the odd chunk cracking off.</summary>
        public static float[] CrunchLoop(uint seed, float seconds = 2f)
        {
            var buf = Alloc(seconds, out int n, out int fade);
            var rng = new Rng(seed);
            var pink = new Pink();
            var band = new Svf(1400f, 0.5f);
            var hp = new OnePole(300f);
            var drift = new Drift(4f, 0.2f);

            for (int i = 0; i < n + fade; i++) buf[i] = hp.High(band.Band(pink.Next(rng.Bi()))) * 0.9f * drift.Next(rng);
            AddBursts(buf, n, fade, rng, 380f, 2600f, 0.6f, 2.5f, 0.9f, 4f);    // grit crackle
            AddBursts(buf, n, fade, rng, 22f, 800f, 0.55f, 9f, 1.0f, 3f);       // chunks cracking off
            return Finish(buf, n, fade, 0.85f);
        }

        /// <summary>
        /// Rubber blade on a wet surface: a damp swish, plus stick-slip chatter -- irregular bursts ~55/s whose
        /// density drifts, so the blade grips and lets go instead of whistling. No squeak oscillator.
        /// </summary>
        public static float[] SqueegeeLoop(uint seed, float seconds = 2f)
        {
            var buf = Alloc(seconds, out int n, out int fade);
            var rng = new Rng(seed);
            var pink = new Pink();
            var wet = new Svf(650f, 0.55f);
            var top = new Svf(2400f, 0.5f);
            var hp = new OnePole(120f);
            var drift = new Drift(2f, 0.1f);

            for (int i = 0; i < n + fade; i++)
            {
                float p = pink.Next(rng.Bi());
                buf[i] = hp.High(wet.Band(p) * 1.5f + top.Band(rng.Bi()) * 0.25f) * drift.Next(rng);
            }
            AddBursts(buf, n, fade, rng, 55f, 1150f, 1.1f, 2.2f, 0.25f, 2f);
            AddBubbles(buf, n, fade, rng, 12f, 1f, 3f, 0.04f);
            return Finish(buf, n, fade, 0.72f);
        }

        /// <summary>Saturated mop on tile: low wet slap, splats and larger bubbles, all drifting randomly.</summary>
        public static float[] SlopLoop(uint seed, float seconds = 2f)
        {
            var buf = Alloc(seconds, out int n, out int fade);
            var rng = new Rng(seed);
            var pink = new Pink();
            var lp = new OnePole(1300f);
            var hp = new OnePole(90f);
            var drift = new Drift(5f, 0.18f);

            for (int i = 0; i < n + fade; i++) buf[i] = hp.High(lp.Low(pink.Next(rng.Bi()))) * 2f * drift.Next(rng);
            AddBursts(buf, n, fade, rng, 40f, 520f, 0.5f, 12f, 0.35f, 2f);
            AddBubbles(buf, n, fade, rng, 45f, 1.5f, 5f, 0.12f);
            return Finish(buf, n, fade, 0.75f);
        }

        /// <summary>Wet vacuum nozzle: rushing air with water gulped in slugs -- gated gurgle, splats and a busy bubble field.</summary>
        public static float[] SlurpLoop(uint seed, float seconds = 2f)
        {
            var buf = Alloc(seconds, out int n, out int fade);
            var rng = new Rng(seed);
            var pink = new Pink();
            var air = new Svf(1600f, 0.5f);
            var gurgle = new Svf(380f, 0.8f);
            var slugA = new OnePole(3f); var slugB = new OnePole(3f);

            for (int i = 0; i < n + fade; i++)
            {
                float p = pink.Next(rng.Bi());
                // water arrives in slugs: a slow random signal, rectified, opens the gurgle band
                float slug = Mathf.Clamp01(slugB.Low(slugA.Low(rng.Bi())) * 60f + 0.4f);
                buf[i] = air.Band(p) * 0.9f + gurgle.Band(pink.Next(rng.Bi())) * slug * 1.8f;
            }
            AddBubbles(buf, n, fade, rng, 160f, 1f, 4.5f, 0.10f);
            AddBursts(buf, n, fade, rng, 30f, 600f, 0.6f, 10f, 0.3f, 2f);
            return Finish(buf, n, fade, 0.8f);
        }

        /// <summary>A head dragged over pile with no machine in it: the quiet bed under the vacuum.</summary>
        public static float[] FrictionLoop(float centreHz, uint seed, float seconds = 2f)
        {
            var buf = Alloc(seconds, out int n, out int fade);
            var rng = new Rng(seed);
            var pink = new Pink();
            var band = new Svf(centreHz, 0.5f);
            var lp = new OnePole(3500f);
            var drift = new Drift(3f, 0.12f);
            for (int i = 0; i < n + fade; i++) buf[i] = lp.Low(band.Band(pink.Next(rng.Bi()))) * drift.Next(rng);
            AddBursts(buf, n, fade, rng, 500f, 1800f, 0.6f, 1.2f, 0.08f, 3f);
            return Finish(buf, n, fade, 0.65f);
        }

        // ------------------------------------------------------------------ debris in the tube
        /// <summary>
        /// One crumb going up a vacuum wand (Docs/AUDIO.md 8): 2-5 impacts on the tube wall, the first at 1.5 ms so
        /// PlayScheduled puts the strike on the scheduled instant, each ricochet later, quieter and duller than the
        /// last because the crumb is travelling away into the hose. Each impact is a sub-millisecond noise burst
        /// ringing three wall modes (ratios 1 : 1.47 : 2.09, Q 12, ~1-2 ms ring: a tick, never a bleep) plus the
        /// hollow body of the air column behind the wall. Large crumbs: lower modes, more body, more ricochets.
        /// Wet: a heavier, damped tube (water film on the wall) with a little slosh.
        /// </summary>
        static float ModeFade(float hz) { return 1f - Mathf.Clamp01((hz - 15000f) / 2000f); }

        public static float[] TubeRattle(uint seed, bool large, bool wet)
        {
            var rng = new Rng(seed);
            int n = Mathf.RoundToInt(0.2f * SampleRate);
            var data = new float[n];
            int hits = large ? 3 + (int)(rng.Unit() * 3f) : 2 + (int)(rng.Unit() * 2f);
            // A white burst gives each mode energy in proportion to its bandwidth f/Q, so the upper modes come out
            // hot; measured, the first cut had its spectral centroid at 7-12 kHz ("tss", not "tok"). Lower base
            // modes, weaker upper modes, more body and a two-pole roll-off put it at the plastic-tube 3-5 kHz.
            float baseHz = (large ? rng.Range(6300f, 8800f) : rng.Range(9300f, 12300f)) * (wet ? 0.85f : 1f);   // ~4.2x the first voicing, raised three times on request
            float t = 0.0015f;
            float amp = 1f;
            for (int h = 0; h < hits && t < 0.16f; h++)
            {
                float dull = h / (float)Mathf.Max(1, hits - 1);           // 0 = at the nozzle, 1 = deep in the hose
                float spot = rng.Range(0.9f, 1.12f);                       // each ricochet strikes a different spot
                float f1 = baseHz * spot, f2 = f1 * 1.47f, f3 = f1 * 2.09f;
                var m1 = new Svf(f1, 12f);
                var m2 = new Svf(f2, 12f);
                var m3 = new Svf(f3, 12f);
                // At this pitch the upper modes of a small crumb land at 14-26 kHz; past ~17 kHz they would sit on the
                // filter clamp next to Nyquist and alias, so they fade out between 15 and 17 kHz instead.
                float w1 = ModeFade(f1), w2 = ModeFade(f2) * 0.5f, w3 = ModeFade(f3) * 0.25f;
                var body = new Svf(rng.Range(2800f, 3900f) * (wet ? 0.8f : 1f), 4f);
                float cut = Mathf.Lerp(18000f, 9500f, dull) * (wet ? 0.7f : 1f);
                var lp = new OnePole(cut);
                var lp2 = new OnePole(cut * 1.3f);
                float burst = rng.Range(0.00015f, 0.0004f) * (large ? 1.6f : 1f);
                float bodyGain = large ? 0.6f : 0.35f;   // halved on the third raise: a wide body band out-powered the wall mode and read as a lower "tok"
                float a = amp * rng.Range(0.75f, 1f);
                int start = Mathf.RoundToInt(t * SampleRate);
                int len = Mathf.Min(n - start, Mathf.RoundToInt(0.045f * SampleRate));
                for (int i = 0; i < len; i++)
                {
                    float ti = i / (float)SampleRate;
                    float exc = ti < burst ? rng.Bi() * (1f - ti / burst) : 0f;
                    // bands divided by Q: the Chamberlin band has gain Q at its centre
                    float x = m1.Band(exc) / 12f * w1 + m2.Band(exc) / 12f * w2 + m3.Band(exc) / 12f * w3
                            + body.Band(exc) / 4f * bodyGain + exc * 0.05f;
                    data[start + i] += lp2.Low(lp.Low(x)) * a;
                }
                t += rng.Range(0.006f, 0.014f) * (1f + h * 0.8f);          // ricochets spread out as it slows
                amp *= rng.Range(0.45f, 0.65f) * (wet ? 0.8f : 1f);
            }
            // Band limit before normalising: raised three times, the white strike burst and the SVF modes near the top
            // put up to 35 % of the energy above 18 kHz -- inaudible to most, wasted headroom after normalising, and
            // alias-prone once the clip is resampled at a playback pitch above 1. Two Butterworth sections at 14.5 kHz.
            var bl1 = new BiquadLp(14500f, 0.7071f);
            var bl2 = new BiquadLp(14500f, 0.7071f);
            for (int i = 0; i < n; i++) data[i] = bl2.Low(bl1.Low(data[i]));
            Normalise(data, 0.9f);
            if (wet)
            {
                // a film of water on the wall: normalised on its own and laid a fixed 24 dB under the strikes.
                // Mixed before normalising (first cut) the bubbles matched the strikes, whose raw level is tiny,
                // and turned 3-5 impacts into 12-15 events of clutter.
                var slosh = new float[n + 1];
                AddBubbles(slosh, n, 1, rng, 25f, 1f, 3f, 1f);
                float m = 1e-6f;
                for (int i = 0; i < n; i++) m = Mathf.Max(m, Mathf.Abs(slosh[i]));
                float g = 0.06f / m;
                for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i] + slosh[i] * g * Mathf.Min(1f, i / (0.01f * SampleRate)), -1f, 1f);
            }
            // fade the last 5 ms so a clip cut short by its voice never clicks
            int tail = Mathf.RoundToInt(0.005f * SampleRate);
            for (int i = 0; i < tail; i++) data[n - 1 - i] *= i / (float)tail;
            return data;
        }

        // ------------------------------------------------------------------ one-shots
        /// <summary>Plastic switch / trigger click: a broadband noise snap, low Q so it ticks instead of rings.</summary>
        public static float[] Click(float freqHz, uint seed)
        {
            int n = Mathf.RoundToInt(0.04f * SampleRate);
            var data = new float[n];
            var rng = new Rng(seed);
            var band = new Svf(freqHz, 0.9f);
            var body = new Svf(freqHz * 0.35f, 0.8f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float x = rng.Bi();
                data[i] = band.Band(x) * Env.Exp01(t, 0.0025f) + body.Band(x) * Env.Exp01(t, 0.006f) * 0.5f;
            }
            return Normalise(data, 0.5f);
        }

        /// <summary>Machine kicking in: switch snap, then a noise thud as the motor takes the load.</summary>
        public static float[] PowerOn(float thudHz, uint seed)
        {
            int n = Mathf.RoundToInt(0.3f * SampleRate);
            var data = new float[n];
            var rng = new Rng(seed);
            var snap = new Svf(2600f, 0.9f);
            var thud1 = new OnePole(thudHz * 2.5f); var thud2 = new OnePole(thudHz * 2.5f);
            var gust = new Svf(900f, 0.5f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float x = rng.Bi();
                float click = snap.Band(x) * Env.Exp01(t, 0.003f);
                float thud = thud2.Low(thud1.Low(rng.Bi())) * Env.Exp01(Mathf.Max(0f, t - 0.008f), 0.06f) * 9f;
                float air = gust.Band(rng.Bi()) * Mathf.Min(1f, t / 0.05f) * Env.Exp01(t, 0.12f) * 0.5f;
                data[i] = click * 0.8f + thud + air;
            }
            return Normalise(data, 0.6f);
        }

        /// <summary>Machine cutting out: switch snap, then noise falling through a closing filter.</summary>
        public static float[] PowerOff(uint seed)
        {
            int n = Mathf.RoundToInt(0.4f * SampleRate);
            var data = new float[n];
            var rng = new Rng(seed);
            var snap = new Svf(2200f, 0.9f);
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float cutoff = Mathf.Lerp(2400f, 160f, Mathf.Clamp01(t / 0.32f));
                float a = 1f - Mathf.Exp(-TwoPi * cutoff / SampleRate);
                y += (rng.Bi() - y) * a;
                data[i] = y * Env.Exp01(t, 0.14f) + snap.Band(rng.Bi()) * Env.Exp01(t, 0.003f) * 0.6f;
            }
            return Normalise(data, 0.5f);
        }

        static class Env
        {
            /// <summary>exp(-t/tau) with t, tau in seconds.</summary>
            public static float Exp01(float t, float tau) { return Mathf.Exp(-t / Mathf.Max(tau, 1e-5f)); }
        }
    }
}
