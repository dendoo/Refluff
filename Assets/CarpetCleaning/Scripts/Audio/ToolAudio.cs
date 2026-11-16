using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CarpetCleaning
{
    /// <summary>
    /// Turns what the tool is doing into sound (Docs/AUDIO.md). Two looping layers per tool -- the machine
    /// (spins up on press, down on release, getting brighter as it spools) and the surface (volume and
    /// brightness follow stroke speed) -- plus one-shots for power on/off and tool changes. Brightness is a
    /// non-resonant low-pass per source; pitch moves only a few percent, because a noise loop sliding in
    /// pitch is the sci-fi 'whoosh' the first voicing was built out of.
    ///
    /// Clips come from the ToolSoundBank. Any empty slot is filled by a procedural placeholder, so the
    /// prototype has sound without a single imported asset and dropping a recording into the bank silently
    /// takes over. A placeholder costs tens of ms to synthesise, which is a visible hitch on the main thread,
    /// so the samples are rendered on a worker thread (ProceduralSfx is pure maths) and only the AudioClip
    /// itself is built here, a couple per frame. Until a slot arrives its layer is simply silent. Bake the
    /// placeholders to WAV (CarpetCleaning/Audio/Bake Placeholder Sounds) and nothing is generated at all.
    ///
    /// Every layer owns TWO AudioSources and crossfades between them, because assigning a new clip to a
    /// playing source clicks: switching tool mid-stroke has to hand over, not cut.
    ///
    /// A tool whose bank entry carries a real recording of its machine (ToolSoundEntry.recording) plays that
    /// instead of the machine layer: RecordedMachine, one per such tool, so a vacuum put down mid-run keeps
    /// coasting after the tool change (Docs/AUDIO.md 9).
    /// </summary>
    [DefaultExecutionOrder(120)]
    public sealed class ToolAudio : MonoBehaviour
    {
        public ToolController tools;
        public ToolSoundBank bank;
        [Tooltip("Source of the crumb-capture events that drive the tube rattle (Docs/AUDIO.md 8). Wired by CarpetSceneBuilder.")]
        public DebrisSystem debris;

        [Header("Mix")]
        [Range(0f, 1f)] public float volume = 1f;
        public bool muted;
        [Tooltip("Toggles mute at runtime. None = no hotkey.")]
        public Key muteKey = Key.M;

        [Header("Response")]
        [Tooltip("Ease rate (1/s) of the stroke-speed follower that drives the surface layer. Low = smooth, high = twitchy.")]
        public float speedEaseHz = 9f;
        [Tooltip("Ease rate (1/s) of every layer's volume. Also the rate the retiring source of a crossfade dies at.")]
        public float volumeEaseHz = 14f;
        [Tooltip("Seconds of silence at the end of a stroke before the machine is treated as released. Stops the motor stuttering when the pointer leaves the carpet for a frame.")]
        public float strokeReleaseGrace = 0.12f;
        [Tooltip("AudioClips built per frame from finished placeholder renders. 2 keeps the frame cost under a millisecond.")]
        public int clipsPerFrame = 2;

        [Header("Debris rattle")]
        [Tooltip("Voices for crumbs in the tube. Every crumb is its own scheduled one-shot, so a burst of pickups overlaps instead of cutting itself off.")]
        public int rattleVoices = 14;
        [Tooltip("Crumbs voiced per frame. The rest of a big burst is counted but not played, or one pile would take every voice.")]
        public int rattleMaxPerFrame = 8;
        [Tooltip("Seconds from the crumb vanishing into the nozzle to it striking the tube wall (min, max). At 20-30 m/s of air a crumb reaches the first bend of the wand in that time.")]
        public Vector2 tubeDelayS = new Vector2(0.008f, 0.022f);
        [Tooltip("Crumbs at least this big (mm) use the large-crumb variations. Scraper chips are 4-12 mm.")]
        public float largeCrumbMm = 7f;

        // one-shot cache keys, kept away from the recipe keys (recipe*100 + tool)
        const int KeyPowerOn = -100000;
        const int KeyPowerOff = -200000;
        const int KeySwitch = -300000;
        const int KeyDebris = -400000;

        // ------------------------------------------------------------------ state
        LoopLayer _motor, _contact;
        AudioSource _oneShot;
        readonly Dictionary<int, AudioClip> _generated = new Dictionary<int, AudioClip>();
        readonly ConcurrentQueue<Baked> _ready = new ConcurrentQueue<Baked>();
        Thread _worker;
        volatile bool _abort;
        ToolId _tool = ToolId.Scraper;
        ToolSoundEntry _entry;
        float _spin;              // 0..1 machine spin-up
        float _speed01;           // eased stroke speed / reference speed
        float _sinceStroke = 999f;
        int _stampSeen;           // ToolController.StampCount at the last tick; a change means the tool worked
        bool _machineOn;
        bool _machineEverOn;
        GameObject _host;
        readonly Dictionary<ToolId, RecordedMachine> _recorded = new Dictionary<ToolId, RecordedMachine>();
        bool _recordedMode;       // the current tool's machine layer is its recording (ToolSoundEntry.UsesRecording)

        AudioSource[] _rattle;
        double[] _rattleFreeAt;   // dsp time each rattle voice finishes its scheduled clip
        double[] _rattleStartAt;  // dsp time each rattle voice's clip starts
        System.Random _rng;
        int _rattleFrame = -1, _rattleThisFrame;
        int _rattleScheduled, _rattleLate, _rattleDropped, _rattleStolen;
        float _lastLeadMs, _lastArrivalMs;

        struct Baked { public int key; public string name; public float[] data; }

        public float MachineSpin { get { return _spin; } }
        public float SurfaceSpeed01 { get { return _speed01; } }
        /// <summary>Placeholders still being rendered; 0 once the worker has caught up (or never had work).</summary>
        public bool PlaceholdersPending { get { return _worker != null && _worker.IsAlive; } }

        void Awake()
        {
            if (tools == null) tools = GetComponent<ToolController>();
            // Enter Play Mode has domain reload off, so a stale cache from the previous session would hand out
            // clips Unity has already destroyed.
            _generated.Clear();
            RecordedMachine.ClockOverride = null;      // a static test hook must not outlive a failed scripted run
            _spin = 0f; _speed01 = 0f; _machineOn = false; _machineEverOn = false; _sinceStroke = 999f;
            _stampSeen = tools != null ? tools.StampCount : 0;
            BuildSources();
        }

        /// <summary>
        /// The runtime AudioSources. Also called from OnEnable when they are missing: a script reload during Play
        /// mode wipes every non-serialized field but runs OnEnable, not Awake, and without this the component
        /// threw NullReferenceException every frame until Play was restarted. The orphaned child is replaced.
        /// </summary>
        void BuildSources()
        {
            var old = transform.Find("ToolAudioSources");
            if (old != null) Destroy(old.gameObject);
            var host = new GameObject("ToolAudioSources");
            host.transform.SetParent(transform, false);
            host.hideFlags = HideFlags.DontSave;
            _host = host;
            _recorded.Clear();                     // their sources went with the old host; rebuilt on demand
            _motor = new LoopLayer(host, "Motor");
            _contact = new LoopLayer(host, "Contact");
            _oneShot = LoopLayer.MakeSource(host, "OneShot", false);

            int voices = Mathf.Clamp(rattleVoices, 1, 24);
            _rattle = new AudioSource[voices];
            _rattleFreeAt = new double[voices];
            _rattleStartAt = new double[voices];
            for (int i = 0; i < voices; i++) _rattle[i] = LoopLayer.MakeSource(host, "Rattle" + i, false);
            _rng = new System.Random(20260921);
            _rattleScheduled = _rattleLate = _rattleDropped = _rattleStolen = 0;
        }

        void OnEnable()
        {
            if (_motor == null || _contact == null || _rattle == null) BuildSources();   // hot reload in Play
            if (tools != null) { _tool = tools.Current; tools.ToolChanged += OnToolChanged; }
            if (debris != null) debris.DebrisSucked += OnDebrisSucked;
            _entry = bank != null ? bank.Get(_tool) : null;
            _recordedMode = _entry != null && _entry.UsesRecording;
            StartPlaceholderRender();
            ApplyClips(false);
        }

        void OnDisable()
        {
            if (tools != null) tools.ToolChanged -= OnToolChanged;
            if (debris != null) debris.DebrisSucked -= OnDebrisSucked;
            if (_motor != null) _motor.Stop();
            if (_contact != null) _contact.Stop();
            foreach (var m in _recorded.Values) m.Silence();
            _spin = 0f; _machineOn = false;
        }

        void OnDestroy()
        {
            _abort = true;                 // the worker only writes to a queue, so abandoning it is safe
            _worker = null;
            // the generated clips carry DontUnloadUnusedAsset, so nothing else will ever collect them
            foreach (var kv in _generated) if (kv.Value != null) Destroy(kv.Value);
            _generated.Clear();
        }

        void OnToolChanged(ToolId t)
        {
            // a machine cannot keep running through a tool change: drop the spin, let the new tool earn it.
            // Before _tool moves on, so what is heard cutting out is the machine that was actually running.
            // A recorded machine needs no power-off click: it plays its own coast-down (TickRecorded).
            if (_machineOn)
            {
                _machineOn = false;
                if (_entry == null || !_entry.UsesRecording) PlayOneShot(Cached(KeyPowerOff), 0.8f, "tool/powerOff");
            }
            _tool = t;
            _entry = bank != null ? bank.Get(t) : null;
            _spin = 0f;
            _machineEverOn = false;
            ApplyClips(true);
            PlayOneShot(Cached(KeySwitch), 0.5f, "tool/switch");
        }

        void Update()
        {
            // input lives here, not in StepFrame: StepAll calls StepFrame many times inside one Unity frame, and
            // wasPressedThisFrame would toggle mute once per call
            if (muteKey != Key.None && Keyboard.current != null && Keyboard.current[muteKey].wasPressedThisFrame)
                muted = !muted;
            StepFrame(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// One audio frame. Public and dt-driven for the same reason CarpetSimulation.StepFrame is: scripted
        /// runs (CarpetDebugApi.StepAll) advance the mix exactly like real frames do, so a stroke laid from the
        /// CLI can be measured. Unscaled dt in Update, because a paused game still has to fade its loops out.
        /// </summary>
        public void StepFrame(float dt)
        {
            if (dt <= 0f) dt = 1f / 60f;

            PumpRenders();

            if (tools != null && tools.Current != _tool) OnToolChanged(tools.Current);   // SetTool before OnEnable hooked up

            // a stamp since the last tick counts even with InputEnabled off: that is exactly the state
            // DebugLiveRunner drives scripted strokes in, and they should sound like the player's
            bool stamped = false;
            if (tools != null && tools.StampCount != _stampSeen) { _stampSeen = tools.StampCount; stamped = true; }
            bool stroking = stamped || (tools != null && tools.IsStroking && tools.InputEnabled);
            if (stroking) _sinceStroke = 0f; else _sinceStroke += dt;
            bool active = _sinceStroke <= strokeReleaseGrace;

            // -------- machine layer
            var e = _entry;
            bool recorded = e != null && e.UsesRecording;
            if (recorded != _recordedMode) { _recordedMode = recorded; ApplyClips(true); }   // toggled in the inspector
            bool powered = e != null && e.powered;
            if (recorded) { _machineOn = active; if (active) _machineEverOn = true; }        // its own switch-on and coast-down
            else if (powered && active && !_machineOn) { _machineOn = true; _machineEverOn = true; PlayOneShot(Cached(KeyPowerOn), 1f, "tool/powerOn"); }
            else if (_machineOn && !active) { _machineOn = false; if (_machineEverOn) PlayOneShot(Cached(KeyPowerOff), 1f, "tool/powerOff"); }

            float spinTarget = _machineOn ? 1f : 0f;
            float spinRate = Mathf.Max(0.1f, _machineOn ? (e != null ? e.spinUpHz : 6f) : (e != null ? e.spinDownHz : 4.5f));
            _spin += (spinTarget - _spin) * (1f - Mathf.Exp(-spinRate * dt));
            if (_spin < 0.0015f) _spin = 0f;

            // -------- stroke speed follower
            float refSpeed = e != null ? Mathf.Max(1f, e.contactRefSpeedMm) : 600f;
            float rawSpeed = tools != null && active ? Mathf.Clamp01(tools.StrokeSpeedMmPerSec / refSpeed) : 0f;
            _speed01 += (rawSpeed - _speed01) * (1f - Mathf.Exp(-Mathf.Max(0.1f, speedEaseHz) * dt));
            if (_speed01 < 0.001f) _speed01 = 0f;

            float master = muted ? 0f : Mathf.Clamp01(volume) * (bank != null ? Mathf.Clamp01(bank.masterVolume) : 1f);
            float pressure = tools != null ? Mathf.Lerp(0.55f, 1f, tools.StrokePressure) : 1f;
            bool onTile = tools != null && !tools.PointerOnCarpet;
            float tileVol = onTile && bank != null ? bank.tileVolume : 1f;
            float tilePitch = onTile && bank != null ? bank.tilePitch : 1f;
            float tileBright = onTile && bank != null ? bank.tileBrightness : 1f;

            // -------- drive the layers
            float ease = Mathf.Max(0.1f, volumeEaseHz);
            if (e != null)
            {
                float motorVol = master * e.motorVolume * _spin;
                float motorPitch = e.motorPitch * Mathf.Lerp(e.spinPitchMin, 1f, _spin) * (1f + e.motorSpeedPitch * _speed01);
                // spin opens the filter on a log scale: a motor spooling up gets brighter, not just faster
                float motorCut = LogLerp(e.motorCutoffIdleHz, OpenHz, _spin);
                _motor.Tick(dt, motorVol, motorPitch, motorCut, ease);

                float drive = Mathf.Lerp(e.contactIdle, 1f, _speed01) * (active ? 1f : 0f);
                float contactVol = master * e.contactVolume * drive * pressure * tileVol;
                float contactPitch = (1f + e.contactSpeedPitch * (_speed01 - 0.5f)) * tilePitch;
                // speed opens the surface filter: a slow drag is dull, a fast one bright -- the realistic cue
                // that used to be (wrongly) carried by a pitch sweep
                float contactCut = Mathf.Min(OpenHz, LogLerp(e.contactCutoffSlowHz, OpenHz, Mathf.Pow(_speed01, 0.8f)) * tileBright);
                _contact.Tick(dt, contactVol, contactPitch, contactCut, ease);
            }
            else
            {
                _motor.Tick(dt, 0f, 1f, OpenHz, ease);
                _contact.Tick(dt, 0f, 1f, OpenHz, ease);
            }

            TickRecorded(dt, recorded, active, master);
        }

        /// <summary>
        /// Every recorded machine, not just the current tool's: one switched off by a tool change still coasts down.
        /// With a recording, the spin the rest of the driver sees is the speed read off its curves.
        /// </summary>
        void TickRecorded(float dt, bool recorded, bool active, float master)
        {
            if (bank == null || _host == null) return;
            if (recorded && !_recorded.ContainsKey(_tool)) _recorded[_tool] = new RecordedMachine(_host, "Recorded" + _tool);
            foreach (var kv in _recorded)
            {
                var me = bank.Get(kv.Key);
                bool current = kv.Key == _tool;
                // a fast stroke loads the running motor a little, as it does the procedural one (motorSpeedPitch)
                float load = current ? 1f + me.motorSpeedPitch * _speed01 : 1f;
                kv.Value.Tick(current && recorded && active, me.UsesRecording, me.recording,
                              master * me.motorVolume, me.motorPitch, load, dt);
            }
            if (recorded) _spin = _recorded[_tool].Speed;
        }

        // ------------------------------------------------------------------ debris rattle
        /// <summary>
        /// A vacuum captured a crumb (DebrisSystem, from the GPU). The event arrives 1-3 frames after the capture,
        /// but it carries the moment the crumb will vanish into the nozzle (captureDsp + arrivalS, typically
        /// 0.1-0.3 s ahead), so the strike is scheduled on the audio clock for exactly then plus the flight to the
        /// first bend of the tube. Only a readback slower than the crumb's own flight plays at once (counted late).
        /// </summary>
        void OnDebrisSucked(DebrisSuckEvent ev)
        {
            if (muted || bank == null || _rattle == null) return;
            var e = bank.Get(ev.tool);
            if (e == null || !e.debrisRattle) return;

            int frame = Time.frameCount;
            if (frame != _rattleFrame) { _rattleFrame = frame; _rattleThisFrame = 0; }
            if (_rattleThisFrame >= Mathf.Max(1, rattleMaxPerFrame)) { _rattleDropped++; return; }

            var clip = DebrisClip(e, ev.tool, ev.sizeMm >= largeCrumbMm);
            if (clip == null) { _rattleDropped++; return; }        // placeholder still rendering

            double now = AudioTap.DspTime;              // AudioSettings.dspTime, or a capture take's virtual clock
            double at = ev.captureDsp + ev.arrivalS + Mathf.Lerp(tubeDelayS.x, tubeDelayS.y, (float)_rng.NextDouble());
            _lastArrivalMs = ev.arrivalS * 1000f;
            _lastLeadMs = (float)((at - now) * 1000.0);
            if (at < now + 0.004)
            {
                at = now + 0.004 + 0.012 * _rng.NextDouble();       // spread the late ones so they never stack
                _rattleLate++;
            }

            int v = 0;
            for (int i = 1; i < _rattle.Length; i++) if (_rattleFreeAt[i] < _rattleFreeAt[v]) v = i;
            if (_rattleFreeAt[v] > at)
            {
                // Every voice busy (the nozzle ran into a pile). A new strike matters more than an old tail, so steal
                // the voice that started longest ago -- but only one already 60 ms into its clip: PlayScheduled on a
                // playing source stops it at once, and a voice still waiting for its own future strike must keep it.
                int oldest = 0;
                for (int i = 1; i < _rattle.Length; i++) if (_rattleStartAt[i] < _rattleStartAt[oldest]) oldest = i;
                if (_rattleStartAt[oldest] > now - 0.06) { _rattleDropped++; return; }
                v = oldest;
                _rattleStolen++;
            }

            float size01 = Mathf.InverseLerp(3f, 12f, ev.sizeMm);
            float pitch = Mathf.Clamp(e.debrisPitch * bank.rattlePitch, 0.25f, 3f) * Mathf.Lerp(1.12f, 0.88f, size01) * (0.96f + 0.08f * (float)_rng.NextDouble());
            float master = Mathf.Clamp01(volume) * Mathf.Clamp01(bank.masterVolume);
            var src = _rattle[v];
            src.clip = clip;
            src.pitch = pitch;
            src.volume = Mathf.Clamp01(master * e.debrisVolume * bank.rattleVolume * Mathf.Lerp(0.45f, 1f, size01) * (0.8f + 0.2f * (float)_rng.NextDouble()));
            src.PlayScheduled(at);
            if (AudioTap.Active) AudioTap.EmitAt("tool/rattle", "scheduled", src, clip, at, 1f, 0f);
            _rattleFreeAt[v] = at + clip.length / pitch;
            _rattleStartAt[v] = at;
            _rattleThisFrame++;
            _rattleScheduled++;
        }

        /// <summary>
        /// After a capture take ran the mix on a virtual audio clock (AudioTap.ClockOverride, StoreGifApi): whatever was
        /// scheduled on it lies in that clock's future. The recorded machines fall silent (the next press starts one from
        /// rest) and the rattle voices are freed, or every crumb would find them busy until the real clock caught up.
        /// Nothing else in the mix is dsp-timed.
        /// </summary>
        public void ResetScheduling()
        {
            foreach (var m in _recorded.Values) m.Silence();
            if (_rattle == null) return;
            for (int i = 0; i < _rattle.Length; i++)
            {
                if (_rattle[i] != null) _rattle[i].Stop();
                _rattleFreeAt[i] = 0.0;
                _rattleStartAt[i] = 0.0;
            }
        }

        /// <summary>Authored clips if the entry has any (first half small crumbs), else the generated variations.</summary>
        AudioClip DebrisClip(ToolSoundEntry e, ToolId tool, bool large)
        {
            var clips = e.debrisClips;
            if (clips != null && clips.Length > 0)
            {
                int half = clips.Length / 2;
                int lo = clips.Length == 1 ? 0 : (large ? half : 0);
                int hi = clips.Length == 1 ? 1 : (large ? clips.Length : half);
                var c = clips[lo + _rng.Next(Mathf.Max(1, hi - lo))];
                if (c != null) return c;
            }
            int n = ToolSfx.DebrisVariants, h2 = n / 2;
            int offset = _rng.Next(h2);
            // the right size class first, then anything already rendered
            for (int k = 0; k < n; k++)
            {
                int cls = (k < h2) == large ? h2 : 0;
                int variant = cls + (offset + k) % h2;
                if (_generated.TryGetValue(DebrisKey(tool, variant), out var g) && g != null) return g;
            }
            return null;
        }

        // ------------------------------------------------------------------ placeholder rendering
        /// <summary>
        /// Queues every empty clip slot in the bank for synthesis on one worker thread, current tool first so
        /// the tool in hand is the first to make a sound. Slots with an authored clip are never queued.
        /// </summary>
        void StartPlaceholderRender()
        {
            if (bank == null || bank.entries == null || _worker != null) return;

            var jobs = new List<Job>();
            var queued = new HashSet<int>();
            var ordered = new List<ToolSoundEntry>(bank.entries.Length);
            foreach (var e in bank.entries) if (e != null && e.tool == _tool) ordered.Add(e);
            foreach (var e in bank.entries) if (e != null && e.tool != _tool) ordered.Add(e);

            void Add(Job j) { if (queued.Add(j.key)) jobs.Add(j); }
            foreach (var e in ordered)
            {
                if (e.motorLoop == null && e.motorRecipe != SfxRecipe.None)
                    Add(new Job { key = RecipeKey(e.motorRecipe, e.tool), kind = JobKind.Recipe, recipe = e.motorRecipe, tool = e.tool });
                if (e.contactLoop == null && e.contactRecipe != SfxRecipe.None)
                    Add(new Job { key = RecipeKey(e.contactRecipe, e.tool), kind = JobKind.Recipe, recipe = e.contactRecipe, tool = e.tool });
                if (e.powered)
                {
                    if (e.powerOn == null) Add(new Job { key = KeyPowerOn - (int)e.tool, kind = JobKind.PowerOn, tool = e.tool });
                    if (e.powerOff == null) Add(new Job { key = KeyPowerOff - (int)e.tool, kind = JobKind.PowerOff, tool = e.tool });
                }
                if (e.debrisRattle && (e.debrisClips == null || e.debrisClips.Length == 0))
                    for (int v = 0; v < ToolSfx.DebrisVariants; v++)
                        Add(new Job { key = DebrisKey(e.tool, v), kind = JobKind.Debris, tool = e.tool, variant = v });
            }
            if (bank.toolSwitchClick == null) Add(new Job { key = KeySwitch, kind = JobKind.Switch });
            if (jobs.Count == 0) return;

            _abort = false;
            _worker = new Thread(() =>
            {
                foreach (var job in jobs)
                {
                    if (_abort) return;
                    float[] data; string name;
                    switch (job.kind)
                    {
                        case JobKind.Recipe:   data = ToolSfx.Render(job.recipe, job.tool); name = ToolSfx.ClipName(job.recipe, job.tool); break;
                        case JobKind.PowerOn:  data = ToolSfx.RenderPowerOn(job.tool); name = "sfx_" + job.tool + "_on"; break;
                        case JobKind.PowerOff: data = ToolSfx.RenderPowerOff(job.tool); name = "sfx_" + job.tool + "_off"; break;
                        case JobKind.Debris:   data = ToolSfx.RenderDebrisHit(job.tool, job.variant); name = "sfx_" + job.tool + "_debris_" + job.variant; break;
                        default:               data = ToolSfx.RenderSwitchClick(); name = "sfx_tool_switch"; break;
                    }
                    if (data != null) _ready.Enqueue(new Baked { key = job.key, name = name, data = data });
                }
            });
            _worker.IsBackground = true;        // a stopped play session must never be held up by it
            _worker.Priority = System.Threading.ThreadPriority.BelowNormal;
            _worker.Start();
        }

        enum JobKind { Recipe, PowerOn, PowerOff, Switch, Debris }

        /// <summary>
        /// What the worker renders. The kind is explicit: the first version inferred it from key ranges, which
        /// would have sent the debris keys (below KeyPowerOff) down the power-off branch.
        /// </summary>
        struct Job { public int key; public JobKind kind; public SfxRecipe recipe; public ToolId tool; public int variant; }

        /// <summary>Main thread: turn finished renders into AudioClips, a couple per frame.</summary>
        void PumpRenders()
        {
            int budget = Mathf.Max(1, clipsPerFrame);
            bool touchedCurrent = false;
            while (budget-- > 0 && _ready.TryDequeue(out var b))
            {
                if (_generated.ContainsKey(b.key)) continue;
                var clip = ProceduralSfx.ToClip(b.name, b.data);
                if (clip == null) continue;
                _generated[b.key] = clip;
                var e = _entry;
                if (e != null && (b.key == RecipeKey(e.motorRecipe, e.tool) || b.key == RecipeKey(e.contactRecipe, e.tool))) touchedCurrent = true;
            }
            if (touchedCurrent) ApplyClips(false);
        }

        static int RecipeKey(SfxRecipe recipe, ToolId tool) { return (int)recipe * 100 + (int)tool; }
        static int DebrisKey(ToolId tool, int variant) { return KeyDebris - (int)tool * 100 - variant; }

        /// <summary>Filter fully open. AudioLowPassFilter tops out at 22 kHz.</summary>
        const float OpenHz = 22000f;

        /// <summary>Interpolation in log-frequency, so equal steps of t are equal musical steps of brightness.</summary>
        static float LogLerp(float a, float b, float t)
        {
            a = Mathf.Clamp(a, 20f, OpenHz); b = Mathf.Clamp(b, 20f, OpenHz);
            return a * Mathf.Pow(b / a, Mathf.Clamp01(t));
        }

        // ------------------------------------------------------------------ clips
        void ApplyClips(bool crossfade)
        {
            if (_motor == null || _contact == null) return;
            var e = _entry;
            // a recorded machine plays through RecordedMachine; the procedural machine layer stays empty
            _motor.SetClip(e != null && !e.UsesRecording ? Resolve(e.motorLoop, e.motorRecipe) : null, crossfade);
            _contact.SetClip(e != null ? Resolve(e.contactLoop, e.contactRecipe) : null, crossfade);
        }

        /// <summary>An authored clip wins; otherwise the placeholder, if the worker has finished it yet.</summary>
        AudioClip Resolve(AudioClip authored, SfxRecipe recipe)
        {
            if (authored != null) return authored;
            if (recipe == SfxRecipe.None) return null;
            return _generated.TryGetValue(RecipeKey(recipe, _tool), out var c) ? c : null;
        }

        /// <summary>One-shot for the current tool: the authored clip, else the rendered placeholder, else nothing.</summary>
        AudioClip Cached(int baseKey)
        {
            var e = _entry;
            if (e != null)
            {
                if (baseKey == KeyPowerOn && e.powerOn != null) return e.powerOn;
                if (baseKey == KeyPowerOff && e.powerOff != null) return e.powerOff;
            }
            if (baseKey == KeySwitch && bank != null && bank.toolSwitchClick != null) return bank.toolSwitchClick;
            int key = baseKey == KeySwitch ? KeySwitch : baseKey - (int)_tool;
            return _generated.TryGetValue(key, out var c) ? c : null;
        }

        /// <summary>`tapSrc` names the sound for AudioTap ("tool/powerOn", "tool/powerOff", "tool/switch").</summary>
        void PlayOneShot(AudioClip clip, float scale, string tapSrc)
        {
            if (clip == null || _oneShot == null || muted) return;
            float master = Mathf.Clamp01(volume) * (bank != null ? Mathf.Clamp01(bank.masterVolume) : 1f);
            float level = Mathf.Clamp01(master * scale);
            _oneShot.PlayOneShot(clip, level);
            if (AudioTap.Active) AudioTap.Emit(tapSrc, "oneshot", _oneShot, clip, level);
        }

        /// <summary>Scripted checks (unity CLI): what the mixer is actually doing this frame.</summary>
        public string DebugStateJson()
        {
            var e = _entry;
            return "{\"tool\":\"" + _tool + "\",\"muted\":" + (muted ? "true" : "false")
                 + ",\"spin\":" + F(_spin)
                 + ",\"speed01\":" + F(_speed01)
                 + ",\"machineOn\":" + (_machineOn ? "true" : "false")
                 + ",\"motorClip\":\"" + _motor.ClipName + "\",\"motorVol\":" + F(_motor.Volume) + ",\"motorPitch\":" + F(_motor.Pitch)
                 + ",\"motorCut\":" + F(_motor.Cutoff)
                 + ",\"contactClip\":\"" + _contact.ClipName + "\",\"contactVol\":" + F(_contact.Volume) + ",\"contactPitch\":" + F(_contact.Pitch)
                 + ",\"contactCut\":" + F(_contact.Cutoff)
                 + ",\"powered\":" + (e != null && e.powered ? "true" : "false")
                 + ",\"pending\":" + (PlaceholdersPending ? "true" : "false")
                 + ",\"rattleScheduled\":" + _rattleScheduled.ToString(CultureInfo.InvariantCulture)
                 + ",\"rattleLate\":" + _rattleLate.ToString(CultureInfo.InvariantCulture)
                 + ",\"rattleDropped\":" + _rattleDropped.ToString(CultureInfo.InvariantCulture)
                 + ",\"rattleStolen\":" + _rattleStolen.ToString(CultureInfo.InvariantCulture)
                 + ",\"lastArrivalMs\":" + F(_lastArrivalMs) + ",\"lastLeadMs\":" + F(_lastLeadMs)
                 + ",\"generated\":" + _generated.Count.ToString(CultureInfo.InvariantCulture)
                 + ",\"recorded\":" + (_recordedMode ? "true" : "false")
                 + ",\"rec\":" + RecordedJson() + "}";
        }

        /// <summary>Every recorded machine by tool, the current one or not: a vacuum put down mid-run is still coasting.</summary>
        string RecordedJson()
        {
            if (_recorded.Count == 0) return "{}";
            var sb = new System.Text.StringBuilder("{");
            foreach (var kv in _recorded)
            {
                if (sb.Length > 1) sb.Append(',');
                sb.Append('"').Append(kv.Key).Append("\":").Append(kv.Value.DebugJson());
            }
            return sb.Append('}').ToString();
        }

        /// <summary>A ru-RU editor would otherwise write "0,42" and the JSON would not parse (same guard as CarpetDebugApi).</summary>
        static string F(float v) { return (float.IsNaN(v) || float.IsInfinity(v)) ? "null" : v.ToString("F3", CultureInfo.InvariantCulture); }

        // ------------------------------------------------------------------ layer
        /// <summary>A looping voice made of two sources so a clip change can crossfade instead of clicking.</summary>
        sealed class LoopLayer
        {
            AudioSource _cur, _old;
            AudioLowPassFilter _curLp, _oldLp;
            float _oldVolume;
            readonly System.Random _rng;

            public LoopLayer(GameObject host, string name)
            {
                _rng = new System.Random(20260925 ^ name.GetHashCode());
                _cur = MakeSource(host, name + "A", true);
                _old = MakeSource(host, name + "B", true);
                _curLp = AddLowPass(_cur);
                _oldLp = AddLowPass(_old);
            }

            /// <summary>
            /// Resonance 1 = no bump. Unity's default is 1 too, but it is set explicitly because a resonant
            /// sweeping low-pass is a wah pedal, and that is exactly the sound this system must not make.
            /// </summary>
            static AudioLowPassFilter AddLowPass(AudioSource s)
            {
                var lp = s.gameObject.AddComponent<AudioLowPassFilter>();
                lp.cutoffFrequency = OpenHz;
                lp.lowpassResonanceQ = 1f;
                return lp;
            }

            public static AudioSource MakeSource(GameObject host, string name, bool loop)
            {
                var go = new GameObject(name);
                go.transform.SetParent(host.transform, false);
                go.hideFlags = HideFlags.DontSave;
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = loop;
                s.spatialBlend = 0f;       // the tool is always right under the camera; 2D keeps it steady
                s.dopplerLevel = 0f;
                s.volume = 0f;
                s.priority = 128;
                return s;
            }

            public string ClipName { get { return _cur != null && _cur.clip != null ? _cur.clip.name : ""; } }
            public float Volume { get { return _cur != null ? _cur.volume : 0f; } }
            public float Pitch { get { return _cur != null ? _cur.pitch : 1f; } }
            public float Cutoff { get { return _curLp != null ? _curLp.cutoffFrequency : OpenHz; } }

            public void SetClip(AudioClip clip, bool crossfade)
            {
                if (_cur == null || _cur.clip == clip) return;
                if (crossfade && _cur.clip != null && _cur.volume > 0.001f)
                {
                    var t = _old; _old = _cur; _cur = t;       // the playing source becomes the one that fades out
                    var tl = _oldLp; _oldLp = _curLp; _curLp = tl;
                    _oldVolume = _old.volume;
                }
                else if (_old != null) { _old.Stop(); _oldVolume = 0f; if (AudioTap.Active) Tap("stop", _old, 0f); }

                _cur.Stop();
                if (AudioTap.Active) Tap("stop", _cur, 0f);
                _cur.clip = clip;
                _cur.volume = 0f;
                if (clip != null) _cur.time = 0f;      // on an empty source Unity ignores it with a console warning
            }

            public void Tick(float dt, float volume, float pitch, float cutoffHz, float easeHz)
            {
                float k = 1f - Mathf.Exp(-easeHz * dt);

                if (_cur != null)
                {
                    if (_cur.clip != null)
                    {
                        float v = _cur.volume + (volume - _cur.volume) * k;
                        float started = -1f;                   // clip seconds a Play this frame starts from, for AudioTap
                        if (v < 0.0008f && volume <= 0f) { v = 0f; if (_cur.isPlaying) { _cur.Stop(); if (AudioTap.Active) Tap("stop", _cur, 0f); } }
                        else if (!_cur.isPlaying && v > 0f)
                        {
                            // Every stroke enters the loop somewhere else. A recorded loop has landmarks (one scrape
                            // louder than the rest), and starting each stroke on the same one is what gives a loop away.
                            // Harmless for the placeholders: every point of a seamless loop is a valid start.
                            int from = 0;
                            if (_cur.clip.length > 1f) { from = _rng.Next(_cur.clip.samples); _cur.timeSamples = from; }
                            _cur.Play();
                            started = _cur.clip.frequency > 0 ? from / (float)_cur.clip.frequency : 0f;
                        }
                        _cur.volume = v;
                        _cur.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
                        if (_curLp != null) _curLp.cutoffFrequency = Mathf.Clamp(cutoffHz, 20f, OpenHz);
                        if (started >= 0f && AudioTap.Active) Tap("play", _cur, started);   // with this frame's level and pitch
                    }
                    else if (_cur.isPlaying) { _cur.Stop(); if (AudioTap.Active) Tap("stop", _cur, 0f); }
                }

                if (_old != null && _old.isPlaying)
                {
                    _oldVolume -= _oldVolume * k;
                    if (_oldVolume < 0.0008f) { _oldVolume = 0f; _old.Stop(); if (AudioTap.Active) Tap("stop", _old, 0f); _old.clip = null; }
                    _old.volume = _oldVolume;
                }
            }

            public void Stop()
            {
                if (_cur != null) { _cur.Stop(); _cur.volume = 0f; if (AudioTap.Active) Tap("stop", _cur, 0f); }
                if (_old != null) { _old.Stop(); _old.volume = 0f; _oldVolume = 0f; if (AudioTap.Active) Tap("stop", _old, 0f); }
            }

            /// <summary>AudioTap report for one of the two sources ("loop/MotorA", "loop/ContactB", ...). Callers check AudioTap.Active.</summary>
            static void Tap(string kind, AudioSource s, float start)
            {
                AudioTap.Emit("loop/" + s.name, kind, s, s.clip, 1f, 0.0, start);
            }
        }
    }
}
