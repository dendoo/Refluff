using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// One-shot sounds of the payout card and the upgrade shop (Docs/AUDIO.md 10). Its own small pool of 2D voices, so the
    /// count-up ticks can climb in pitch and a burst of coin clinks overlaps instead of cutting itself off. Follows the
    /// tool mix: silent while ToolAudio is muted (M), scaled by ToolAudio.volume x its bank's masterVolume.
    /// Clips come from UiSoundBank; an empty slot is rendered from its UiSfxSynth recipe on a worker thread at startup
    /// (the recipes are pure maths; only AudioClip.Create touches the engine, a couple per frame).
    /// </summary>
    [DefaultExecutionOrder(125)]
    public sealed class UiAudio : MonoBehaviour
    {
        public UiSoundBank bank;
        public ToolAudio toolAudio;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Voices in the pool. Coin landings and ticks steal the oldest voice when all are busy.")]
        public int voices = 12;

        AudioSource[] _src;
        int _next;
        readonly AudioClip[] _generated = new AudioClip[UiSfxSynth.Count];
        readonly ConcurrentQueue<(int id, float[] data)> _ready = new ConcurrentQueue<(int, float[])>();
        Thread _worker;
        volatile bool _abort;
        readonly float[] _lastPlayed = new float[UiSfxSynth.Count];

        public int Played { get; private set; }

        void Awake()
        {
            if (toolAudio == null) toolAudio = UnityEngine.Object.FindAnyObjectByType<ToolAudio>();
            if (bank != null) bank.Normalize();
            for (int i = 0; i < _lastPlayed.Length; i++) _lastPlayed[i] = -1f;
            BuildSources();
            StartRender();
        }

        void OnEnable() { if (_src == null) BuildSources(); }

        void OnDestroy()
        {
            _abort = true;
            _worker = null;
            for (int i = 0; i < _generated.Length; i++) if (_generated[i] != null) { Destroy(_generated[i]); _generated[i] = null; }
        }

        void BuildSources()
        {
            var old = transform.Find("UiAudioSources");
            if (old != null) Destroy(old.gameObject);
            var host = new GameObject("UiAudioSources");
            host.transform.SetParent(transform, false);
            host.hideFlags = HideFlags.DontSave;
            int n = Mathf.Clamp(voices, 2, 32);
            _src = new AudioSource[n];
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("UiVoice" + i);
                go.transform.SetParent(host.transform, false);
                go.hideFlags = HideFlags.DontSave;
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false; s.loop = false; s.spatialBlend = 0f; s.dopplerLevel = 0f; s.priority = 96;
                _src[i] = s;
            }
        }

        void StartRender()
        {
            var todo = new System.Collections.Generic.List<int>();
            for (int i = 0; i < UiSfxSynth.Count; i++) if (bank == null || bank.Clip((UiSfx)i) == null) todo.Add(i);
            if (todo.Count == 0) return;
            _abort = false;
            _worker = new Thread(() =>
            {
                foreach (int id in todo)
                {
                    if (_abort) return;
                    _ready.Enqueue((id, UiSfxSynth.Render((UiSfx)id)));
                }
            }) { IsBackground = true, Name = "UiSfxSynth" };
            _worker.Start();
        }

        void Update()
        {
            int built = 0;
            while (built < 3 && _ready.TryDequeue(out var job))
            {
                var clip = AudioClip.Create(UiSfxSynth.Name((UiSfx)job.id), job.data.Length, 1, UiSfxSynth.SampleRate, false);
                clip.SetData(job.data, 0);
                clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
                _generated[job.id] = clip;
                built++;
            }
        }

        public bool Ready(UiSfx id) { return Clip(id) != null; }

        AudioClip Clip(UiSfx id)
        {
            var c = bank != null ? bank.Clip(id) : null;
            return c != null ? c : _generated[(int)id];
        }

        float Master
        {
            get
            {
                float m = Mathf.Clamp01(volume) * (bank != null ? bank.masterGain : 0.8f);
                if (toolAudio != null)
                {
                    if (toolAudio.muted) return 0f;
                    m *= Mathf.Clamp01(toolAudio.volume) * (toolAudio.bank != null ? Mathf.Clamp01(toolAudio.bank.masterVolume) : 1f);
                }
                return m;
            }
        }

        /// <summary>
        /// Plays a one-shot. `minGap` (s, unscaled) drops a repeat of the same sound that comes sooner (a fast count-up
        /// would otherwise machine-gun its ticks).
        /// </summary>
        public void Play(UiSfx id, float vol = 1f, float pitch = 1f, float minGap = 0f)
        {
            if (_src == null) return;
            float m = Master;
            if (m <= 0f) return;
            var clip = Clip(id);
            if (clip == null) return;
            float now = Time.unscaledTime;
            int i = (int)id;
            if (minGap > 0f && _lastPlayed[i] >= 0f && now - _lastPlayed[i] < minGap) return;
            _lastPlayed[i] = now;
            var s = _src[_next];
            _next = (_next + 1) % _src.Length;
            s.Stop();                                  // the voice's previous one-shot is cut (round-robin stealing)
            if (AudioTap.Active) AudioTap.Emit("ui/voice", "stop", s, null);
            s.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
            float level = Mathf.Clamp01(m * vol * (bank != null ? bank.Gain(id) : 1f));
            s.PlayOneShot(clip, level);
            if (AudioTap.Active) AudioTap.Emit("ui/" + id, "oneshot", s, clip, level);
            Played++;
        }
    }
}
