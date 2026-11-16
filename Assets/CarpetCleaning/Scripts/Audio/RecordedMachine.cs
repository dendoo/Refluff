using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// A tool's machine layer played from one real recording (ToolSoundEntry.recording, Docs/AUDIO.md 9): the
    /// recorded spin-up, the steady loop and the recorded coast-down, instead of one loop swept in pitch.
    ///
    /// The start clip's last sample continues straight into the loop's first (Tools/cut_machine_recording.py cuts
    /// both from one take), so the loop is PlayScheduled for the exact moment the start clip ends: a gapless stitch,
    /// no crossfade, so the motor's whine cannot beat against itself. A release or a re-press mid-transition does not
    /// restart a clip from its top. It enters the other transition where the motor turns as fast as it does now (the
    /// speed curves measured from the recording) and crossfades there: tap the trigger and the coast-down starts from
    /// a half-spun motor; press again while it coasts and the spin-up carries on from that speed.
    ///
    /// Everything is timed on AudioSettings.dspTime, because the clips play on that clock: a scheduled start, the
    /// stitch and a crossfade line up only when all of them are computed from it. So Speed moves only while the audio
    /// engine runs (an unfocused editor does not run it), unless a capture take gives AudioTap a virtual clock.
    /// Every PlayScheduled, SetScheduledEndTime and Stop is reported to AudioTap ("rec/&lt;name&gt;/start|loop|stop|release").
    /// </summary>
    public sealed class RecordedMachine
    {
        public enum Phase { Off, Starting, Running, Stopping }

        /// <summary>One AudioSource and what it was told to do; its position and gain follow from the audio clock.</summary>
        sealed class Voice
        {
            public AudioSource src;
            public bool busy;
            public double at;                         // dsp time it starts
            public double end = double.MaxValue;      // dsp time a one-shot runs out
            public float offset, pitch = 1f;          // clip seconds at `at`, playback rate
            public float fadeIn;                      // seconds after `at`; 0 = starts at full level
            public double fadeOutAt = double.MaxValue;
            public float fadeOut;

            public float Position(double now) { return offset + (float)Math.Max(0.0, now - at) * pitch; }

            /// <summary>Equal-power fades: the two sides of a crossfade are different stretches of the take, not one signal.</summary>
            public float Gain(double now)
            {
                float g = fadeIn > 0f ? Mathf.Sin(0.5f * Mathf.PI * Mathf.Clamp01((float)((now - at) / fadeIn))) : 1f;
                if (now > fadeOutAt) g *= Mathf.Cos(0.5f * Mathf.PI * Mathf.Clamp01((float)((now - fadeOutAt) / Math.Max(1e-3, fadeOut))));
                return g;
            }
        }

        readonly Voice[] _voices;
        Voice _start, _loop, _stop;       // the current phase's voices; anything else busy is fading out
        MachineRecording _rec;            // the take those voices play (its curves give their speed)
        readonly string _name;            // "Recorded" + tool, for AudioTap
        Phase _phase;
        float _speed;
        float _loadPitch = 1f;

        public Phase State { get { return _phase; } }
        /// <summary>Motor speed, 0 (still) .. 1 (running), read off the recording's own curves.</summary>
        public float Speed { get { return _speed; } }

        /// <summary>
        /// Test hook: a clock to use instead of AudioSettings.dspTime, for CarpetDebugApi.RecordedMachineScenario (the
        /// real audio clock stands still in an unfocused editor). Null outside that call; ToolAudio.Awake clears it too.
        /// Without it the clock is AudioTap.DspTime: the real one, or a capture take's virtual clock (StoreGifApi).
        /// </summary>
        public static Func<double> ClockOverride;
        static double Now { get { return ClockOverride != null ? ClockOverride() : AudioTap.DspTime; } }

        /// <summary>
        /// Five voices: a spin-up and its scheduled loop, plus the tails of what they cut off. A release and a re-press
        /// inside one crossfade still find a free one.
        /// </summary>
        public RecordedMachine(GameObject host, string name, int voices = 5)
        {
            _name = name;
            _voices = new Voice[Mathf.Max(3, voices)];
            for (int i = 0; i < _voices.Length; i++)
            {
                var go = new GameObject(name + i);
                go.transform.SetParent(host.transform, false);
                go.hideFlags = HideFlags.DontSave;
                var s = go.AddComponent<AudioSource>();
                // the same settings as ToolAudio's loop sources: 2D, the tool is always right under the camera
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.dopplerLevel = 0f;
                s.volume = 0f;
                s.priority = 128;
                _voices[i] = new Voice { src = s };
            }
        }

        /// <summary>
        /// One frame. `on` = the tool is working. `usable` false = the recording was switched off or taken away:
        /// fade out at once, no coast-down. `volume` = master x motorVolume, `pitch` = motorPitch for every clip;
        /// `loadPitch` is the running motor's extra pitch under a fast stroke, for the loop only and eased in from 1,
        /// so the stitch out of the start clip never jumps.
        /// </summary>
        public void Tick(bool on, bool usable, MachineRecording rec, float volume, float pitch, float loadPitch, float dt)
        {
            double now = Now;
            pitch = Mathf.Clamp(pitch, 0.25f, 2.5f);

            if (!usable || rec == null || !rec.Complete)
            {
                if (_phase != Phase.Off) FadeOutAll(now, 0.05f);
                _phase = Phase.Off; _start = _loop = _stop = null;
            }
            else
            {
                // the audio clock moves the phase on: into the loop at the stitch, to rest at the end of the coast
                if (_phase == Phase.Starting && _loop != null && now >= _loop.at) { _phase = Phase.Running; _start = null; }
                if (_phase == Phase.Stopping && _stop != null && now >= _stop.end) { _phase = Phase.Off; _stop = null; }

                if (on && (_phase == Phase.Off || _phase == Phase.Stopping)) SwitchOn(rec, now, pitch);
                else if (!on && (_phase == Phase.Starting || _phase == Phase.Running)) SwitchOff(rec, now, pitch);
            }
            _speed = SpeedAt(now);

            _loadPitch += ((_phase == Phase.Running ? loadPitch : 1f) - _loadPitch) * (1f - Mathf.Exp(-3f * Mathf.Max(0f, dt)));

            foreach (var v in _voices)
            {
                if (!v.busy) continue;
                // a one-shot ends by itself and a faded voice is already silent: the margin only keeps Stop() from
                // ever landing inside a mix block that still plays their last samples
                if (now > v.end + 0.1 || now > v.fadeOutAt + v.fadeOut + 0.05) { Release(v); continue; }
                v.src.volume = Mathf.Clamp01(volume * v.Gain(now));
                if (v == _loop) v.src.pitch = pitch * _loadPitch;
            }
        }

        /// <summary>Silence now, no coast-down (component disabled, sources about to go).</summary>
        public void Silence()
        {
            foreach (var v in _voices) Release(v);
            _start = _loop = _stop = null;
            _phase = Phase.Off; _speed = 0f; _loadPitch = 1f;
        }

        // ------------------------------------------------------------------ transitions
        void SwitchOn(MachineRecording rec, double now, float pitch)
        {
            double at = now + Lead();
            bool fromRest = _phase == Phase.Off;
            // enter the spin-up where the motor turns as fast as the coasting one will when the new clip starts
            float offset = fromRest ? 0f : TimeAtSpeed(rec.startSpeed, rec.start.length, SpeedAt(at), true);
            FadeOutAll(at, rec.crossfade);
            _start = _loop = _stop = null;
            _rec = rec;
            _loadPitch = 1f;

            double remaining = (rec.start.length - offset) / pitch;
            if (remaining <= Mathf.Max(0.05f, rec.crossfade))
            {
                // still at full speed (a re-press right after a release): straight back to running
                _loop = Play(rec.loop, true, 0f, at, pitch, fromRest ? 0f : rec.crossfade, "loop");
                _phase = Phase.Running;
                return;
            }
            // from rest the clip's own 5 ms fade-in is the switch-on; entered mid-clip it needs a crossfade
            _start = Play(rec.start, false, offset, at, pitch, fromRest ? 0f : rec.crossfade, "start");
            _start.end = at + remaining;
            _start.src.SetScheduledEndTime(_start.end);                     // never under the loop, whatever happens
            if (AudioTap.Active) AudioTap.Emit(TapSrc("start"), "stop", _start.src, rec.start, 1f, _start.end - now);
            _loop = Play(rec.loop, true, 0f, at + remaining, pitch, 0f, "loop");     // the stitch, sample-exact
            _phase = Phase.Starting;
        }

        void SwitchOff(MachineRecording rec, double now, float pitch)
        {
            double at = now + Lead();
            float speed = SpeedAt(at);
            // a loop due after `at` has not reached the mixer yet: cancel it outright. One due sooner may already be
            // playing, so it fades with the rest
            if (_loop != null && _loop.at >= at) { Release(_loop); _loop = null; }
            float offset = TimeAtSpeed(rec.stopSpeed, rec.stop.length, speed, false);
            FadeOutAll(at, rec.crossfade);
            _start = _loop = null;
            _rec = rec;
            _stop = Play(rec.stop, false, offset, at, pitch, rec.crossfade, "stop");
            _stop.end = at + (rec.stop.length - offset) / pitch;
            _phase = Phase.Stopping;
        }

        float SpeedAt(double t)
        {
            switch (_phase)
            {
                case Phase.Starting:
                    return _start != null && _loop != null && t < _loop.at && _rec != null ? Mathf.Clamp01(_rec.startSpeed.Evaluate(_start.Position(t))) : 1f;
                case Phase.Running:
                    return 1f;
                case Phase.Stopping:
                    return _stop != null && _rec != null ? Mathf.Clamp01(_rec.stopSpeed.Evaluate(_stop.Position(t))) : 0f;
                default:
                    return 0f;
            }
        }

        /// <summary>First clip time at which the curve reaches `speed` (rising: at least, falling: at most); the clip's end if it never does.</summary>
        static float TimeAtSpeed(AnimationCurve c, float length, float speed, bool rising)
        {
            const float step = 0.005f;
            for (float t = 0f; t < length; t += step)
            {
                float v = c.Evaluate(t);
                if (rising ? v >= speed : v <= speed) return t;
            }
            return length;
        }

        /// <summary>
        /// How far ahead a clip is scheduled: past the mix block that may be in progress, or the start would slip to
        /// the next block and the stitch behind it would overlap. 1.5 blocks + 10 ms (42 ms at 1024 / 48 kHz).
        /// </summary>
        static double Lead()
        {
            AudioSettings.GetDSPBufferSize(out int length, out int _);
            int rate = Mathf.Max(8000, AudioSettings.outputSampleRate);
            return Mathf.Clamp(1.5f * length / rate + 0.01f, 0.02f, 0.1f);
        }

        // ------------------------------------------------------------------ voices
        Voice Play(AudioClip clip, bool loop, float offset, double at, float pitch, float fadeIn, string role)
        {
            var v = FreeVoice();
            var s = v.src;
            s.Stop();
            s.clip = clip;
            s.loop = loop;
            s.pitch = pitch;
            s.volume = 0f;                             // Tick sets the level in this same frame
            int from = Mathf.Clamp(Mathf.RoundToInt(offset * clip.frequency), 0, Mathf.Max(0, clip.samples - 1));
            s.timeSamples = from;
            s.PlayScheduled(at);
            // level 0 here: the voice's gain follows per frame (DebugJson "volume"; a take logs it every Step)
            if (AudioTap.Active) AudioTap.Emit(TapSrc(role), "scheduled", s, clip, 1f, at - Now, clip.frequency > 0 ? from / (float)clip.frequency : 0f);
            v.busy = true;
            v.at = at; v.end = double.MaxValue;
            v.offset = offset; v.pitch = pitch;
            v.fadeIn = fadeIn;
            v.fadeOutAt = double.MaxValue; v.fadeOut = 0f;
            return v;
        }

        void FadeOutAll(double at, float seconds)
        {
            foreach (var v in _voices)
                if (v.busy && v.fadeOutAt > at) { v.fadeOutAt = at; v.fadeOut = seconds; }
        }

        /// <summary>A free voice, else the quietest one already on its way out.</summary>
        Voice FreeVoice()
        {
            foreach (var v in _voices) if (!v.busy) return v;
            double now = Now;
            Voice best = null; float bestGain = float.MaxValue;
            foreach (var v in _voices)
            {
                if (v == _start || v == _loop || v == _stop) continue;
                float g = v.Gain(now);
                if (g < bestGain) { bestGain = g; best = v; }
            }
            if (best == null) best = _voices[0];
            Release(best);
            return best;
        }

        void Release(Voice v)
        {
            if (v.busy && v.src != null && AudioTap.Active) AudioTap.Emit(TapSrc("release"), "stop", v.src, v.src.clip);
            if (v.src != null) { v.src.Stop(); v.src.volume = 0f; v.src.clip = null; }
            v.busy = false;
            v.end = v.fadeOutAt = double.MaxValue;
        }

        string TapSrc(string role) { return "rec/" + _name + "/" + role; }

        /// <summary>Scripted checks (CarpetDebugApi.AudioJson): phase, speed and every busy voice against the audio clock.</summary>
        public string DebugJson()
        {
            double now = Now;
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("{\"phase\":\"").Append(_phase).Append("\",\"speed\":").Append(_speed.ToString("F3", ci))
              .Append(",\"dsp\":").Append(now.ToString("F4", ci))
              .Append(",\"leadMs\":").Append((Lead() * 1000.0).ToString("F1", ci))
              .Append(",\"voices\":[");
            bool first = true;
            foreach (var v in _voices)
            {
                if (!v.busy) continue;
                if (!first) sb.Append(',');
                first = false;
                string role = v == _start ? "start" : v == _loop ? "loop" : v == _stop ? "stop" : "fading";
                sb.Append("{\"role\":\"").Append(role)
                  .Append("\",\"clip\":\"").Append(v.src.clip != null ? v.src.clip.name : "")
                  .Append("\",\"startsInMs\":").Append(((v.at - now) * 1000.0).ToString("F1", ci))
                  .Append(",\"pos\":").Append(v.Position(now).ToString("F3", ci))
                  .Append(",\"gain\":").Append(v.Gain(now).ToString("F3", ci))
                  .Append(",\"volume\":").Append(v.src.volume.ToString("F3", ci))
                  .Append(",\"pitch\":").Append(v.src.pitch.ToString("F3", ci))
                  .Append(",\"timeSamples\":").Append(v.src.timeSamples.ToString(ci))
                  .Append(",\"isPlaying\":").Append(v.src.isPlaying ? "true" : "false")
                  .Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }
    }
}
