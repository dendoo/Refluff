using System;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>Which ProceduralSfx generator fills an empty clip slot (Docs/AUDIO.md 3).</summary>
    public enum SfxRecipe
    {
        None = 0,
        VacuumMotor = 1,
        WetVacuumMotor = 2,
        Blower = 3,
        WaterJet = 4,
        SprayHiss = 5,
        FoamHiss = 6,
        Scrub = 7,
        Crunch = 8,
        Squeegee = 9,
        Slop = 10,
        Slurp = 11,
        Friction = 12,
        RotaryMotor = 13,
    }

    /// <summary>
    /// One real recording of a machine being switched on, run and switched off, cut by
    /// Tools/cut_machine_recording.py into the three clips below (Docs/AUDIO.md 9). When all of it is present it
    /// replaces the entry's machine layer (RecordedMachine): the recorded spin-up runs straight on into the loop, and
    /// a release plays the recorded coast-down from whatever speed the motor has reached, instead of one loop swept
    /// in pitch. Filled by ToolSoundBaker.ImportMachineRecording, speed curves included.
    /// </summary>
    [Serializable]
    public sealed class MachineRecording
    {
        [Tooltip("The recorded spin-up, from switch-on to running. Its last sample continues straight into the loop's first (both are cut from one take), so the loop is scheduled to start exactly where it ends.")]
        public AudioClip start;
        [Tooltip("Steady running, seamless.")]
        public AudioClip loop;
        [Tooltip("The recorded coast-down after switch-off, from full speed to silence.")]
        public AudioClip stop;
        [Tooltip("Motor speed (1 = the loop) against the start clip's time, tracked on the motor's whine. A re-press while the motor still coasts enters the start clip where it turns this fast.")]
        public AnimationCurve startSpeed = new AnimationCurve();
        [Tooltip("Motor speed against the stop clip's time. A release during the spin-up enters the stop clip where it turns as fast as the motor did.")]
        public AnimationCurve stopSpeed = new AnimationCurve();
        [Tooltip("Crossfade (s) when a release or a re-press cuts into a clip that is already playing.")]
        [Range(0.01f, 0.3f)] public float crossfade = 0.05f;
        [Tooltip("Source and licence of the recording. Shipping the game has to respect that licence.")]
        public string credit = "";

        public bool Complete
        {
            get
            {
                return start != null && loop != null && stop != null
                    && startSpeed != null && startSpeed.length >= 2 && stopSpeed != null && stopSpeed.length >= 2;
            }
        }
    }

    /// <summary>
    /// One tool's voice: up to four clips and the numbers that drive them. Every clip slot may stay empty —
    /// the matching recipe then synthesises a placeholder at runtime, so the bank works before a single
    /// recording exists and stops generating the moment a real clip is dropped in.
    /// </summary>
    [Serializable]
    public sealed class ToolSoundEntry
    {
        public ToolId tool;
        public string label = "";

        [Header("Recorded machine - replaces the machine layer while useRecording is on")]
        [Tooltip("Play the recorded start / loop / stop below instead of the machine layer's clips and recipe. Off = the procedural machine again (compare by ear; works in Play mode). Ignored until all three clips and both curves are set.")]
        public bool useRecording = true;
        public MachineRecording recording = new MachineRecording();

        /// <summary>The machine layer is the recording: a powered tool, switched on, clips and curves complete.</summary>
        public bool UsesRecording { get { return powered && useRecording && recording != null && recording.Complete; } }

        [Header("Clips - leave empty to use the procedural recipe")]
        [Tooltip("Machine layer: runs while the button is held, spun up and down. Vacuums, dryer, sprayers.")]
        public AudioClip motorLoop;
        [Tooltip("Surface layer: what the head does to the carpet. Volume and pitch follow stroke speed.")]
        public AudioClip contactLoop;
        [Tooltip("Played once when the machine starts (powered tools only).")]
        public AudioClip powerOn;
        [Tooltip("Played once when the machine stops (powered tools only).")]
        public AudioClip powerOff;
        [Tooltip("Source and licence of every recorded clip in the slots above, one 'slot: credit' line each. Filled by ToolSoundBaker.ImportClip; shipping the game has to respect these licences (Docs/AUDIO_CREDITS.md). The machine recording carries its own credit.")]
        [TextArea(1, 6)] public string clipCredits = "";

        [Header("Procedural fallback")]
        public SfxRecipe motorRecipe = SfxRecipe.None;
        public SfxRecipe contactRecipe = SfxRecipe.None;

        [Header("Machine layer")]
        [Tooltip("True = a motor/pump that spins up on press and down on release, with the two one-shots.")]
        public bool powered;
        [Range(0f, 1f)] public float motorVolume = 0.5f;
        [Tooltip("Pitch at full spin-up.")]
        public float motorPitch = 1f;
        [Tooltip("Spin-up rate (1/s): 12 is a sprayer valve, 4 is a heavy vacuum.")]
        public float spinUpHz = 6f;
        public float spinDownHz = 4.5f;
        [Tooltip("Pitch multiplier at zero spin, i.e. how far the motor sags when it dies.")]
        [Range(0.2f, 1f)] public float spinPitchMin = 0.6f;
        [Tooltip("Pitch added at the reference stroke speed: a head pushed fast loads the motor.")]
        public float motorSpeedPitch = 0.05f;

        [Header("Surface layer")]
        [Range(0f, 1f)] public float contactVolume = 0.55f;
        [Tooltip("Fraction of the contact volume left when the tool is held still on the carpet.")]
        [Range(0f, 1f)] public float contactIdle = 0.10f;
        [Tooltip("Stroke speed (mm/s) that counts as a full-strength drag.")]
        public float contactRefSpeedMm = 600f;
        [Tooltip("Pitch added at the reference stroke speed. Keep it small: a noise texture sliding in pitch is the sci-fi 'whoosh'; real friction gets BRIGHTER when faster, which is contactCutoffSlowHz's job.")]
        public float contactSpeedPitch = 0.06f;
        [Tooltip("Surface low-pass cutoff (Hz) at zero speed; opens towards 22 kHz at the reference speed. A slow drag is dull, a fast one bright.")]
        public float contactCutoffSlowHz = 2200f;

        [Header("Machine brightness")]
        [Tooltip("Machine low-pass cutoff (Hz) at zero spin; opens fully at full spin, so a motor spooling up gets brighter as well as faster. 22000 = unfiltered (sprayer valves).")]
        public float motorCutoffIdleHz = 1200f;

        [Header("Debris in the tube (vacuums)")]
        [Tooltip("Crumbs this tool really picks up rattle up the tube. Driven by the GPU capture events, not by a timer (Docs/AUDIO.md 8).")]
        public bool debrisRattle;
        [Tooltip("One crumb per clip, the first impact at the very start. Empty = 8 generated variations: 0-3 small crumbs, 4-7 large. With your own clips, put small ones in the first half.")]
        public AudioClip[] debrisClips = new AudioClip[0];
        [Range(0f, 1f)] public float debrisVolume = 0.55f;
        [Tooltip("Playback pitch of the rattle on top of the size variation. Tune by ear here; no rebake needed.")]
        [Range(0.5f, 2.5f)] public float debrisPitch = 1f;

        public ToolSoundEntry() { }

        /// <summary>Fluent setter: this tool rattles the crumbs it sucks up, at the given volume.</summary>
        public ToolSoundEntry Rattle(float volume)
        {
            debrisRattle = true;
            debrisVolume = volume;
            return this;
        }

        /// <summary>Fluent setter for the two brightness fields, so the defaults table stays one line per tool.</summary>
        public ToolSoundEntry Bright(float contactCutoffSlowHz, float motorCutoffIdleHz)
        {
            this.contactCutoffSlowHz = contactCutoffSlowHz;
            this.motorCutoffIdleHz = motorCutoffIdleHz;
            return this;
        }

        public ToolSoundEntry(ToolId tool, string label, bool powered,
                              SfxRecipe motorRecipe, float motorVolume, float motorPitch, float spinUpHz, float spinDownHz, float spinPitchMin, float motorSpeedPitch,
                              SfxRecipe contactRecipe, float contactVolume, float contactIdle, float contactRefSpeedMm, float contactSpeedPitch)
        {
            this.tool = tool; this.label = label; this.powered = powered;
            this.motorRecipe = motorRecipe; this.motorVolume = motorVolume; this.motorPitch = motorPitch;
            this.spinUpHz = spinUpHz; this.spinDownHz = spinDownHz; this.spinPitchMin = spinPitchMin; this.motorSpeedPitch = motorSpeedPitch;
            this.contactRecipe = contactRecipe; this.contactVolume = contactVolume; this.contactIdle = contactIdle;
            this.contactRefSpeedMm = contactRefSpeedMm; this.contactSpeedPitch = contactSpeedPitch;
        }
    }

    /// <summary>
    /// Every tool's sound in one asset (Settings/ToolSoundBank.asset, built by CarpetSceneBuilder).
    /// Docs/AUDIO.md. Order follows ToolTable, and Get() falls back to a silent default so a half-filled
    /// bank can never null-ref the audio driver.
    /// </summary>
    [CreateAssetMenu(menuName = "CarpetCleaning/Tool Sound Bank", fileName = "ToolSoundBank")]
    public sealed class ToolSoundBank : ScriptableObject
    {
        [Range(0f, 1f)] public float masterVolume = 0.85f;
        [Tooltip("Tick played when the selected tool changes. Empty = generated.")]
        public AudioClip toolSwitchClick;
        [Tooltip("Contact pitch multiplier on bare tile. Small on purpose; the tile mostly sounds brighter (tileBrightness).")]
        public float tilePitch = 1.04f;
        [Tooltip("Contact low-pass cutoff multiplier on bare tile: a hard floor passes more top end than pile.")]
        public float tileBrightness = 1.6f;
        [Tooltip("Contact volume multiplier on bare tile.")]
        public float tileVolume = 1.15f;
        [Header("Vacuum crumb rattle - quick tuning")]
        [Tooltip("Pitch of the crumb-in-the-tube rattle for both vacuums, applied live (no rebake). Multiplies each entry's own debrisPitch. Above 1 the ricochets also speed up.")]
        [Range(0.5f, 2.5f)] public float rattlePitch = 1f;
        [Tooltip("Volume of the crumb rattle for both vacuums, multiplies each entry's debrisVolume.")]
        [Range(0f, 2f)] public float rattleVolume = 1f;

        public ToolSoundEntry[] entries = new ToolSoundEntry[0];

        static readonly ToolSoundEntry Silent = new ToolSoundEntry();

        public ToolSoundEntry Get(ToolId t)
        {
            if (entries != null)
                for (int i = 0; i < entries.Length; i++)
                    if (entries[i] != null && entries[i].tool == t) return entries[i];
            return Silent;
        }

        /// <summary>
        /// The shipped voicing. Kept in C# as well as in the .asset so a fresh project builds the same sound;
        /// CarpetSceneBuilder calls it only when it has to create the asset, so hand-tuned values survive.
        /// </summary>
        public void ResetToDefaults()
        {
            masterVolume = 0.85f;
            tilePitch = 1.04f;
            tileBrightness = 1.6f;
            tileVolume = 1.15f;
            toolSwitchClick = null;
            // Pitch ranges are deliberately narrow (the first voicing swept vacuums from 0.45x to 1x in 0.3 s and
            // slid surfaces +-22% with speed, which is what made everything sound like a spaceship). Spin-down is
            // slow because a real impeller coasts; brightness, not pitch, carries speed and spin now.
            // The volumes are set for the real recordings the bank ships with (Docs/AUDIO.md 11): each recording was
            // levelled to the placeholder it replaced, and where its crest factor kept it quieter (crackle, water spray)
            // the volume here makes up the difference, so the mix is the one the placeholders were balanced to. The
            // brush's surface is the exception: its hairbrush crackle sits ~4.6 dB under the old scrub even at 1.
            entries = new[]
            {
                //                   tool                  label           powered  motor recipe            vol   pitch  up    down  pMin  spdP   contact recipe       vol   idle  refMm  spdP
                new ToolSoundEntry(ToolId.Scraper,      "Scraper",      false, SfxRecipe.None,           0f,   1f,    6f,   5f,   1f,    0f,    SfxRecipe.Crunch,    1.00f, 0.02f, 450f, 0.08f).Bright(3000f, 22000f),
                new ToolSoundEntry(ToolId.DryVacuum,    "Dry vacuum",   true,  SfxRecipe.VacuumMotor,    0.42f, 1f,   3.5f, 1.8f, 0.72f, 0.02f, SfxRecipe.Friction,  0.34f, 0.06f, 600f, 0.05f).Bright(1800f, 900f).Rattle(0.6f),
                new ToolSoundEntry(ToolId.Hose,         "Hose",         true,  SfxRecipe.WaterJet,       0.92f, 1f,   11f,  9f,   0.97f, 0.01f, SfxRecipe.None,      0f,    0f,    600f, 0f).Bright(2200f, 6000f),
                new ToolSoundEntry(ToolId.FoamSprayer,  "Foam sprayer", true,  SfxRecipe.FoamHiss,       1.00f, 1f,   13f,  10f,  0.98f, 0.01f, SfxRecipe.None,      0f,    0f,    600f, 0f).Bright(2200f, 22000f),
                // the brush is a rotary floor machine (Docs/ROTARY_BRUSH.md): its spin rates match ToolModelSet's, so the
                // hum and the bristles wind up and coast down together; the pad scrubs held still, hence the high idle
                new ToolSoundEntry(ToolId.Brush,        "Brush",        true,  SfxRecipe.RotaryMotor,    0.50f, 1f,   4f,   1.2f, 0.80f, 0.01f, SfxRecipe.Scrub,     1.00f, 0.60f, 550f, 0.07f).Bright(2200f, 700f),
                new ToolSoundEntry(ToolId.Squeegee,     "Squeegee",     false, SfxRecipe.None,           0f,   1f,    6f,   5f,   1f,    0f,    SfxRecipe.Squeegee,  0.60f, 0.02f, 700f, 0.06f).Bright(1600f, 22000f),
                new ToolSoundEntry(ToolId.WetVacuum,    "Wet vacuum",   true,  SfxRecipe.WetVacuumMotor, 0.44f, 1f,   3.5f, 1.6f, 0.72f, 0.02f, SfxRecipe.Slurp,     0.66f, 0.10f, 500f, 0.05f).Bright(1500f, 800f).Rattle(0.45f),
                new ToolSoundEntry(ToolId.Dryer,        "Dryer",        true,  SfxRecipe.Blower,         0.44f, 1f,   3.5f, 1.5f, 0.70f, 0.01f, SfxRecipe.None,      0f,    0f,    600f, 0f).Bright(2200f, 1000f),
                new ToolSoundEntry(ToolId.FloorCleaner, "Detergent",    true,  SfxRecipe.SprayHiss,      0.84f, 1f,   13f,  10f,  0.98f, 0.01f, SfxRecipe.None,      0f,    0f,    600f, 0f).Bright(2200f, 22000f),
                new ToolSoundEntry(ToolId.Mop,          "Mop",          false, SfxRecipe.None,           0f,   1f,    6f,   5f,   1f,    0f,    SfxRecipe.Slop,      1.00f, 0.05f, 650f, 0.06f).Bright(1400f, 22000f),
            };
        }

        /// <summary>
        /// Puts every number back to the shipped voicing but keeps whatever clips are assigned, so a voicing
        /// change in C# can be pushed into the .asset without losing recordings (CLAUDE.md: keep .asset and
        /// C# defaults in sync). Menu: CarpetCleaning/Audio/Reset Voicing (Keep Clips).
        /// With `only`, just that tool's entry is reset and everything else in the bank stays as tuned.
        /// </summary>
        public void ResetVoicingKeepClips(ToolId? only = null)
        {
            var keep = new System.Collections.Generic.Dictionary<ToolId, ToolSoundEntry>();
            if (entries != null) foreach (var e in entries) if (e != null) keep[e.tool] = e;
            var tuned = entries;
            float master = masterVolume, tp = tilePitch, tb = tileBrightness, tv = tileVolume, rp = rattlePitch, rv = rattleVolume;
            var click = toolSwitchClick;
            ResetToDefaults();
            toolSwitchClick = click;
            if (only.HasValue)
            {
                masterVolume = master; tilePitch = tp; tileBrightness = tb; tileVolume = tv; rattlePitch = rp; rattleVolume = rv;
                var fresh = entries;
                entries = tuned ?? new ToolSoundEntry[0];
                int slot = System.Array.FindIndex(entries, x => x != null && x.tool == only.Value);
                var reset = System.Array.Find(fresh, x => x.tool == only.Value);
                if (reset == null) return;
                if (slot >= 0) entries[slot] = reset;
                else { System.Array.Resize(ref entries, entries.Length + 1); entries[entries.Length - 1] = reset; }
            }
            foreach (var e in entries)
            {
                if (e == null || !keep.TryGetValue(e.tool, out var old) || old == e) continue;
                e.motorLoop = old.motorLoop; e.contactLoop = old.contactLoop;
                e.powerOn = old.powerOn; e.powerOff = old.powerOff;
                e.clipCredits = old.clipCredits;                            // the licences travel with the clips
                if (old.debrisClips != null && old.debrisClips.Length > 0) e.debrisClips = old.debrisClips;
                if (old.recording != null) e.recording = old.recording;     // clips + the curves measured from them
                e.useRecording = old.useRecording;
            }
        }

        void OnValidate()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            tilePitch = Mathf.Clamp(tilePitch, 0.25f, 4f);
            tileVolume = Mathf.Clamp(tileVolume, 0f, 4f);
            tileBrightness = Mathf.Clamp(tileBrightness, 0.25f, 8f);
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e == null) continue;
                // AudioSource.pitch is clamped to [-3,3] by Unity; keep the authored value inside a sane band
                e.motorPitch = Mathf.Clamp(e.motorPitch, 0.25f, 2.5f);
                e.spinUpHz = Mathf.Max(0.1f, e.spinUpHz);
                e.spinDownHz = Mathf.Max(0.1f, e.spinDownHz);
                e.contactRefSpeedMm = Mathf.Max(1f, e.contactRefSpeedMm);
                e.contactCutoffSlowHz = Mathf.Clamp(e.contactCutoffSlowHz, 200f, 22000f);
                e.motorCutoffIdleHz = Mathf.Clamp(e.motorCutoffIdleHz, 200f, 22000f);
            }
        }
    }

    /// <summary>
    /// Recipe -> generator. Shared by the runtime driver and the editor WAV baker. Render* returns raw samples
    /// and is thread-safe (pure maths); Create* turns them into an AudioClip and must run on the main thread.
    /// </summary>
    public static class ToolSfx
    {
        /// <summary>Stable per-slot seed, so every tool's noise differs but never changes between runs.</summary>
        public static uint Seed(SfxRecipe recipe, ToolId tool) { return (uint)(((int)recipe * 733 + (int)tool * 97 + 17) * 2654435761u); }

        public static string ClipName(SfxRecipe recipe, ToolId tool) { return "sfx_" + tool + "_" + recipe; }

        public static AudioClip Create(SfxRecipe recipe, ToolId tool)
        {
            return ProceduralSfx.ToClip(ClipName(recipe, tool), Render(recipe, tool));
        }

        /// <summary>Worker-thread half: samples only, no engine objects.</summary>
        public static float[] Render(SfxRecipe recipe, ToolId tool)
        {
            uint s = Seed(recipe, tool);
            switch (recipe)
            {
                //                                                      blade Hz  body Hz  roar Hz  whine  body
                case SfxRecipe.VacuumMotor:    return ProceduralSfx.MotorLoop(1180f,   235f,    1500f,   1.0f,  0.6f, s);
                case SfxRecipe.WetVacuumMotor: return ProceduralSfx.MotorLoop(820f,    160f,    1050f,   0.8f,  0.9f, s);
                // floor machine: a heavy induction motor's 100 Hz hum, a dull cooling-air roar and a faint gearbox whine
                case SfxRecipe.RotaryMotor:    return ProceduralSfx.MotorLoop(340f,    100f,    520f,    0.5f,  1.6f, s);
                //                                                      fan Hz  low cut  high cut
                case SfxRecipe.Blower:         return ProceduralSfx.AirLoop(360f,   180f,    5200f, s);
                case SfxRecipe.WaterJet:       return ProceduralSfx.WaterJetLoop(2400f, 1f, 1f, s);
                //                                                      low cut  top   fizz
                case SfxRecipe.SprayHiss:      return ProceduralSfx.HissLoop(1600f, 5500f, 0f, s);
                case SfxRecipe.FoamHiss:       return ProceduralSfx.HissLoop(1100f, 4200f, 1f, s);
                case SfxRecipe.Scrub:          return ProceduralSfx.ScrubLoop(s);
                case SfxRecipe.Crunch:         return ProceduralSfx.CrunchLoop(s);
                case SfxRecipe.Squeegee:       return ProceduralSfx.SqueegeeLoop(s);
                case SfxRecipe.Slop:           return ProceduralSfx.SlopLoop(s);
                case SfxRecipe.Slurp:          return ProceduralSfx.SlurpLoop(s);
                case SfxRecipe.Friction:       return ProceduralSfx.FrictionLoop(900f, s);
                default: return null;
            }
        }

        /// <summary>Sprayers click when the valve opens, motors thump.</summary>
        static bool IsSpray(ToolId tool) { return tool == ToolId.Hose || tool == ToolId.FoamSprayer || tool == ToolId.FloorCleaner; }

        public static float[] RenderPowerOn(ToolId tool)
        {
            uint s = Seed(SfxRecipe.None, tool) ^ 0x5f3759dfu;
            // the floor machine's big motor thumps lowest
            float thudHz = tool == ToolId.Brush ? 55f : tool == ToolId.WetVacuum ? 70f : 95f;
            return IsSpray(tool) ? ProceduralSfx.Click(1800f, s)
                                 : ProceduralSfx.PowerOn(thudHz, s);
        }

        public static float[] RenderPowerOff(ToolId tool)
        {
            uint s = Seed(SfxRecipe.None, tool) ^ 0x9e3779b9u;
            return IsSpray(tool) ? ProceduralSfx.Click(1100f, s)
                                 : ProceduralSfx.PowerOff(s);
        }

        /// <summary>Generated tube-rattle variations per vacuum: the first half small crumbs, the second half large.</summary>
        public const int DebrisVariants = 8;

        public static float[] RenderDebrisHit(ToolId tool, int variant)
        {
            uint s = Seed(SfxRecipe.None, tool) ^ (0xA511E9B3u + (uint)variant * 0x9E3779B9u);
            return ProceduralSfx.TubeRattle(s, variant >= DebrisVariants / 2, tool == ToolId.WetVacuum);
        }

        public static AudioClip DebrisHit(ToolId tool, int variant)
        {
            return ProceduralSfx.ToClip("sfx_" + tool + "_debris_" + variant, RenderDebrisHit(tool, variant));
        }

        public static float[] RenderSwitchClick() { return ProceduralSfx.Click(2600f, 0xC0FFEEu); }

        public static AudioClip PowerOn(ToolId tool) { return ProceduralSfx.ToClip("sfx_" + tool + "_on", RenderPowerOn(tool)); }
        public static AudioClip PowerOff(ToolId tool) { return ProceduralSfx.ToClip("sfx_" + tool + "_off", RenderPowerOff(tool)); }
        public static AudioClip SwitchClick() { return ProceduralSfx.ToClip("sfx_tool_switch", RenderSwitchClick()); }
    }
}
