using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// One sound the game started or stopped, as AudioTap reports it (Docs/AUDIO.md 12). `volume` is the final level
    /// (AudioSource.volume x the one-shot's volumeScale), `delay` the seconds from the call to the start (PlayScheduled:
    /// scheduled dsp - AudioTap.DspTime; a stop scheduled with SetScheduledEndTime: the end, from now), `start` the clip
    /// position (seconds) it starts from, `clock` AudioTap.DspTime at the call.
    /// </summary>
    public struct AudioTapEvent
    {
        public string src;          // who: "tool/powerOn", "loop/MotorA", "rec/RecordedDryVacuum/start", "ui/KaChing", ...
        public string kind;         // "oneshot" | "scheduled" | "play" | "stop" | "playing" (already playing when the tap began)
        public AudioClip clip;
        public float volume;        // source.volume * volumeScale at the call
        public float scale;         // the volumeScale / requested level alone (a one-shot on a silent source still says what it asked for)
        public float pitch;
        public double delay;
        public float pan;           // panStereo
        public bool loop;
        public int sourceId;        // AudioTap.IdOf(source): stable for the source's lifetime, 0 = none
        public float start;         // clip seconds the voice starts from
        public double clock;        // AudioTap.DspTime at the call
        public AudioSource source;
    }

    /// <summary>
    /// A listening tap on every sound the game plays, for offline audio (the trailer foley, Docs/STORE_CLIPS.md 5): each
    /// PlayOneShot, Play, PlayScheduled, SetScheduledEndTime and Stop site calls Emit behind `if (AudioTap.Active)`, so a
    /// game with no listener pays one static bool read per site and never builds a string. Emitting changes nothing that
    /// is heard. `ClockOverride` replaces AudioSettings.dspTime for every dsp-timed decision (ToolAudio's crumb rattle,
    /// DebrisSystem's capture stamp, RecordedMachine): a take stepping real frames in an unfocused Editor, where the real
    /// audio clock stands still, gives them a virtual clock that advances with the Steps.
    /// </summary>
    public static class AudioTap
    {
        /// <summary>Listening. Set by the capture tool (StoreGifApi); false in the game.</summary>
        public static bool Active;

        /// <summary>Every event while Active. Called on the main thread from inside the sound's call site.</summary>
        public static event Action<AudioTapEvent> Played;

        /// <summary>
        /// A clock to use instead of AudioSettings.dspTime (virtual audio clock of a capture take). Null in the game.
        /// Every change bumps ClockEpoch.
        /// </summary>
        public static Func<double> ClockOverride
        {
            get { return s_clock; }
            set { if (s_clock != value) { s_clock = value; ClockEpoch++; } }
        }
        static Func<double> s_clock;

        /// <summary>
        /// Which clock DspTime is on: bumped whenever ClockOverride changes (a take starts or ends). A time stamped on
        /// another epoch means nothing on this one: DebrisSystem drops crumb captures read back across the switch, or a
        /// capture from a take's last frames would be scheduled ~1000 s ahead after it (and pass into the next take).
        /// </summary>
        public static int ClockEpoch { get; private set; }

        /// <summary>The audio clock every dsp-timed decision reads: AudioSettings.dspTime unless a take installed ClockOverride.</summary>
        public static double DspTime { get { return ClockOverride != null ? ClockOverride() : AudioSettings.dspTime; } }

        static readonly Dictionary<AudioSource, int> s_ids = new Dictionary<AudioSource, int>();
        static readonly List<AudioSource> s_sources = new List<AudioSource>();
        static int s_nextId = 1;

        /// <summary>Every source seen by Emit or Register since the last ClearSources, in first-seen order (may hold destroyed ones).</summary>
        public static IReadOnlyList<AudioSource> Sources { get { return s_sources; } }

        /// <summary>Forget the sources (their ids restart at 1).</summary>
        public static void ClearSources() { s_ids.Clear(); s_sources.Clear(); s_nextId = 1; }

        /// <summary>The tap's id for a source, registering it on first sight. 0 for null.</summary>
        public static int IdOf(AudioSource s)
        {
            if (ReferenceEquals(s, null)) return 0;
            if (s_ids.TryGetValue(s, out int id)) return id;
            id = s_nextId++;
            s_ids[s] = id;
            s_sources.Add(s);
            return id;
        }

        /// <summary>Registers a source without an event (a take lists the sources that play before its first Step).</summary>
        public static int Register(AudioSource s) { return IdOf(s); }

        // domain reload is off in Enter Play Mode: nothing of a previous session's tap may survive into the next one
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active = false;
            Played = null;
            s_clock = null;
            ClockEpoch = 0;
            ClearSources();
        }

        /// <summary>
        /// Reports one sound. `volumeScale` = the one-shot's volumeScale (1 for Play / PlayScheduled / Stop), `delay` =
        /// seconds until it starts (or stops), `start` = clip seconds it starts from (negative = read the source's
        /// timeSamples for play / scheduled, 0 otherwise). No-op while !Active; a listener that throws is logged, never
        /// passed on to the game code that played the sound.
        /// </summary>
        public static void Emit(string src, string kind, AudioSource s, AudioClip clip, float volumeScale = 1f, double delay = 0.0, float start = -1f)
        {
            if (!Active) return;
            var handler = Played;
            bool alive = s != null;
            if (clip == null && alive) clip = s.clip;
            if (start < 0f)
            {
                start = 0f;
                if (alive && clip != null && clip.frequency > 0 && (kind == "play" || kind == "scheduled" || kind == "playing"))
                    start = s.timeSamples / (float)clip.frequency;
            }
            var ev = new AudioTapEvent
            {
                src = src,
                kind = kind,
                clip = clip,
                volume = (alive ? s.volume : 1f) * volumeScale,
                scale = volumeScale,
                pitch = alive ? s.pitch : 1f,
                delay = delay,
                pan = alive ? s.panStereo : 0f,
                loop = kind != "oneshot" && alive && s.loop,
                sourceId = IdOf(s),
                start = start,
                clock = DspTime,
                source = s
            };
            if (handler == null) return;
            try { handler(ev); }
            catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>Emit for a dsp-timed call (PlayScheduled, SetScheduledEndTime): the delay is dspAt - DspTime.</summary>
        public static void EmitAt(string src, string kind, AudioSource s, AudioClip clip, double dspAt, float volumeScale = 1f, float start = -1f)
        {
            if (!Active) return;
            Emit(src, kind, s, clip, volumeScale, dspAt - DspTime, start);
        }
    }
}
