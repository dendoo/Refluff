using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// Writes the procedural placeholders (ProceduralSfx) out as 16-bit PCM WAVs so they can be auditioned in
    /// the Project window, and optionally wires them into the ToolSoundBank so the runtime stops generating.
    /// Docs/AUDIO.md 5. Also the measuring tape: Report() prints length, peak, RMS and the loop-seam step of
    /// every clip, which is how the voicing is checked without ears.
    /// </summary>
    public static class ToolSoundBaker
    {
        const string BankPath = "Assets/CarpetCleaning/Settings/ToolSoundBank.asset";
        const string OutDir = "Assets/CarpetCleaning/Audio/Generated";

        [MenuItem("CarpetCleaning/Audio/Bake Placeholder Sounds (WAV)")]
        public static void BakeMenu() { Debug.Log(Bake(true)); }

        [MenuItem("CarpetCleaning/Audio/Clear Baked Sounds")]
        public static void ClearMenu() { Debug.Log(Clear()); }

        [MenuItem("CarpetCleaning/Audio/Report Placeholder Levels")]
        public static void ReportMenu() { Debug.Log(Report()); }

        [MenuItem("CarpetCleaning/Audio/Reset Voicing (Keep Clips)")]
        public static void ResetVoicingMenu() { Debug.Log(ResetVoicing()); }

        /// <summary>Pushes the C# default numbers into the .asset, keeping every assigned clip.</summary>
        public static string ResetVoicing()
        {
            var bank = AssetDatabase.LoadAssetAtPath<ToolSoundBank>(BankPath);
            if (bank == null) return "[ToolSoundBaker] no bank at " + BankPath;
            bank.ResetVoicingKeepClips();
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return "[ToolSoundBaker] voicing reset to C# defaults, clips kept";
        }

        struct Rendered { public string name; public float[] data; }

        /// <summary>
        /// Every placeholder the bank's recipes describe, rendered in parallel. ProceduralSfx is pure maths,
        /// so this is safe off the main thread, and it keeps a full bake inside the CLI's 5 s main-thread limit.
        /// `only` renders one tool's clips and skips the shared UI click.
        /// </summary>
        static List<Rendered> RenderAll(ToolSoundBank bank, ToolId? only = null)
        {
            var jobs = new List<KeyValuePair<string, System.Func<float[]>>>();
            foreach (var e in bank.entries)
            {
                if (e == null || (only.HasValue && e.tool != only.Value)) continue;
                var tool = e.tool; var mr = e.motorRecipe; var cr = e.contactRecipe;
                if (mr != SfxRecipe.None) jobs.Add(new KeyValuePair<string, System.Func<float[]>>(Name(tool, "motor"), () => ToolSfx.Render(mr, tool)));
                if (cr != SfxRecipe.None) jobs.Add(new KeyValuePair<string, System.Func<float[]>>(Name(tool, "contact"), () => ToolSfx.Render(cr, tool)));
                if (e.powered)
                {
                    jobs.Add(new KeyValuePair<string, System.Func<float[]>>(Name(tool, "on"), () => ToolSfx.RenderPowerOn(tool)));
                    jobs.Add(new KeyValuePair<string, System.Func<float[]>>(Name(tool, "off"), () => ToolSfx.RenderPowerOff(tool)));
                }
                if (e.debrisRattle)
                    for (int v = 0; v < ToolSfx.DebrisVariants; v++)
                    {
                        int variant = v;
                        jobs.Add(new KeyValuePair<string, System.Func<float[]>>(Name(tool, "debris_" + variant), () => ToolSfx.RenderDebrisHit(tool, variant)));
                    }
            }
            if (!only.HasValue) jobs.Add(new KeyValuePair<string, System.Func<float[]>>("ui_tool_switch", ToolSfx.RenderSwitchClick));

            var outp = new Rendered[jobs.Count];
            System.Threading.Tasks.Parallel.For(0, jobs.Count, i => { outp[i] = new Rendered { name = jobs[i].Key, data = jobs[i].Value() }; });
            return new List<Rendered>(outp);
        }

        /// <summary>
        /// Writes every placeholder as WAV into any folder (outside Assets = no import), for listening and for
        /// offline analysis. The fast loop for tuning the synthesis: edit, recompile, preview, measure.
        /// </summary>
        public static string WritePreview(string dir)
        {
            var bank = AssetDatabase.LoadAssetAtPath<ToolSoundBank>(BankPath);
            if (bank == null) return "[ToolSoundBaker] no bank at " + BankPath;
            Directory.CreateDirectory(dir);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var all = RenderAll(bank);
            long renderMs = sw.ElapsedMilliseconds;
            foreach (var r in all) if (r.data != null) File.WriteAllBytes(Path.Combine(dir, r.name + ".wav"), EncodeWav(r.data, ProceduralSfx.SampleRate));
            return "[ToolSoundBaker] preview: " + all.Count + " clips, " + renderMs + " ms render, " + sw.ElapsedMilliseconds + " ms total -> " + dir;
        }

        /// <summary>
        /// One tool's placeholders only: written and assigned to its own bank slots, every other slot left alone
        /// (a full Bake re-assigns them all, recordings included).
        /// </summary>
        public static string BakeTool(ToolId tool) { return Bake(true, tool); }

        /// <summary>Generates every placeholder, writes it to Audio/Generated and (optionally) fills the bank slots that hold no recording.</summary>
        public static string Bake(bool assignToBank, ToolId? only = null)
        {
            EnsureFolders();
            var bank = AssetDatabase.LoadAssetAtPath<ToolSoundBank>(BankPath);
            if (bank == null) return "[ToolSoundBaker] no bank at " + BankPath + " - run CarpetCleaning/Build Scene first";

            var written = new List<string>();
            var sb = new StringBuilder("[ToolSoundBaker] baked\n");
            foreach (var r in RenderAll(bank, only))
            {
                if (r.data == null) continue;
                string path = OutDir + "/" + r.name + ".wav";
                File.WriteAllBytes(path, EncodeWav(r.data, ProceduralSfx.SampleRate));
                written.Add(path);
                sb.Append("  ").Append(r.name).Append(".wav  ").Append((r.data.Length / (float)ProceduralSfx.SampleRate).ToString("F2", CultureInfo.InvariantCulture)).Append(" s\n");
            }
            AssetDatabase.Refresh();

            foreach (var p in written) ApplyImportSettings(p);
            AssetDatabase.Refresh();

            if (assignToBank)
            {
                foreach (var e in bank.entries)
                {
                    if (e == null || (only.HasValue && e.tool != only.Value)) continue;
                    // a recorded clip (ImportClip) stays: only empty slots and earlier placeholders are (re)assigned
                    if (!IsRecorded(e.motorLoop)) e.motorLoop = Load(Name(e.tool, "motor", e.motorRecipe));
                    if (!IsRecorded(e.contactLoop)) e.contactLoop = Load(Name(e.tool, "contact", e.contactRecipe));
                    if (!IsRecorded(e.powerOn)) e.powerOn = e.powered ? Load(Name(e.tool, "on")) : null;
                    if (!IsRecorded(e.powerOff)) e.powerOff = e.powered ? Load(Name(e.tool, "off")) : null;
                    if (e.debrisRattle)
                    {
                        var clips = new AudioClip[ToolSfx.DebrisVariants];
                        for (int v = 0; v < clips.Length; v++) clips[v] = Load(Name(e.tool, "debris_" + v));
                        e.debrisClips = clips;
                    }
                }
                if (!only.HasValue) bank.toolSwitchClick = Load("ui_tool_switch");
                EditorUtility.SetDirty(bank);
                AssetDatabase.SaveAssets();
                sb.Append("assigned into ").Append(BankPath).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>What Tools/cut_machine_recording.py writes next to its three clips (the fields the bank needs).</summary>
        [System.Serializable]
        sealed class MachineCut
        {
            public string source = "", credit = "", startFile = "", loopFile = "", stopFile = "";
            public float lineHz = 0f;
            public float[] startSpeedT = new float[0], startSpeedV = new float[0], stopSpeedT = new float[0], stopSpeedV = new float[0];
        }

        /// <summary>
        /// Puts a recording cut by Tools/cut_machine_recording.py into one tool's bank entry (Docs/AUDIO.md 9): the
        /// three clips (uncompressed, stereo kept, preloaded), the two speed curves and the credit, and switches the
        /// entry to its recording. `json` is the cutter's description next to the clips, for example
        /// Assets/CarpetCleaning/Audio/Recorded/dryvacuum_samsung.json. The placeholder slots stay as they are: they
        /// are what plays with useRecording off, and Bake/Clear never touch the recording.
        /// </summary>
        public static string ImportMachineRecording(ToolId tool, string json)
        {
            var bank = AssetDatabase.LoadAssetAtPath<ToolSoundBank>(BankPath);
            if (bank == null) return "[ToolSoundBaker] no bank at " + BankPath;
            var e = System.Array.Find(bank.entries, x => x != null && x.tool == tool);
            if (e == null) return "[ToolSoundBaker] the bank has no entry for " + tool;
            if (!e.powered) return "[ToolSoundBaker] " + tool + " has no machine to replace (powered = false)";
            if (!File.Exists(json)) return "[ToolSoundBaker] no cut description at " + json;
            var cut = JsonUtility.FromJson<MachineCut>(File.ReadAllText(json));
            if (cut.startSpeedT.Length < 2 || cut.startSpeedT.Length != cut.startSpeedV.Length || cut.stopSpeedT.Length < 2 || cut.stopSpeedT.Length != cut.stopSpeedV.Length)
                return "[ToolSoundBaker] " + json + " carries no usable speed curves";

            string dir = Path.GetDirectoryName(json).Replace('\\', '/');
            var clips = new AudioClip[3];
            var files = new[] { cut.startFile, cut.loopFile, cut.stopFile };
            for (int i = 0; i < 3; i++)
            {
                string p = dir + "/" + files[i];
                AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
                ApplyRecordingImportSettings(p);
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                if (clips[i] == null) return "[ToolSoundBaker] could not load " + p;
            }

            var keepFade = e.recording != null ? e.recording.crossfade : 0.05f;
            e.recording = new MachineRecording
            {
                start = clips[0], loop = clips[1], stop = clips[2],
                startSpeed = LinearCurve(cut.startSpeedT, cut.startSpeedV),
                stopSpeed = LinearCurve(cut.stopSpeedT, cut.stopSpeedV),
                crossfade = keepFade,
                credit = cut.credit,
            };
            e.useRecording = true;
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            var ci = CultureInfo.InvariantCulture;
            return "[ToolSoundBaker] " + tool + " now plays " + cut.source + ": start " + clips[0].length.ToString("F2", ci)
                 + " s, loop " + clips[1].length.ToString("F2", ci) + " s, stop " + clips[2].length.ToString("F2", ci) + " s, "
                 + clips[1].channels + " ch @ " + clips[1].frequency + " Hz; curves " + cut.startSpeedT.Length + " + " + cut.stopSpeedT.Length
                 + " keys; " + (cut.lineHz > 0f ? "motor line " + cut.lineHz.ToString("F1", ci) + " Hz" : "speed from the level envelope")
                 + "; credit: " + cut.credit;
        }

        /// <summary>What Tools/prepare_recording.py writes next to a loop or one-shot it made from a recording.</summary>
        [System.Serializable]
        sealed class ClipCut
        {
            public string slot = "", file = "", source = "", credit = "";
        }

        /// <summary>
        /// Puts one clip made by Tools/prepare_recording.py into a tool's bank slot (Docs/AUDIO.md 11): `json` is the
        /// description next to the WAV (slot contact / motor / on / off, file, credit). The clip is imported like a
        /// machine take (PCM, stereo kept, preloaded) and its credit replaces that slot's line in the entry's
        /// clipCredits, so the licences travel with the bank. A full Bake re-assigns every slot to its placeholder:
        /// re-import afterwards (ImportClipsIn does a whole folder).
        /// </summary>
        public static string ImportClip(ToolId tool, string json)
        {
            var bank = AssetDatabase.LoadAssetAtPath<ToolSoundBank>(BankPath);
            if (bank == null) return "[ToolSoundBaker] no bank at " + BankPath;
            var e = System.Array.Find(bank.entries, x => x != null && x.tool == tool);
            if (e == null) return "[ToolSoundBaker] the bank has no entry for " + tool;
            if (!File.Exists(json)) return "[ToolSoundBaker] no clip description at " + json;
            var cut = JsonUtility.FromJson<ClipCut>(File.ReadAllText(json));
            string p = Path.GetDirectoryName(json).Replace('\\', '/') + "/" + cut.file;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
            ApplyRecordingImportSettings(p);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            if (clip == null) return "[ToolSoundBaker] could not load " + p;

            switch (cut.slot)
            {
                case "contact": e.contactLoop = clip; break;
                case "motor": e.motorLoop = clip; break;
                case "on": e.powerOn = clip; break;
                case "off": e.powerOff = clip; break;
                default: return "[ToolSoundBaker] unknown slot '" + cut.slot + "' in " + json;
            }
            e.clipCredits = SetCreditLine(e.clipCredits, cut.slot, cut.credit);
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return "[ToolSoundBaker] " + tool + "." + cut.slot + " = " + cut.file + " (" + clip.length.ToString("F2", CultureInfo.InvariantCulture)
                 + " s, " + clip.channels + " ch @ " + clip.frequency + " Hz); credit: " + cut.credit;
        }

        /// <summary>
        /// Re-reads every recording the bank already plays from the description next to it: each machine take
        /// (&lt;name&gt;_loop.wav -&gt; &lt;name&gt;.json, curves included) and each recorded clip slot (&lt;clip&gt;.json). What to
        /// run after Tools/audio_recordings.py build has re-cut the clips (from the original downloads, say): the WAVs
        /// keep their names and GUIDs, but the speed curves and credits come from the JSONs.
        /// </summary>
        public static string ReimportRecordings()
        {
            var bank = AssetDatabase.LoadAssetAtPath<ToolSoundBank>(BankPath);
            if (bank == null) return "[ToolSoundBaker] no bank at " + BankPath;
            var sb = new StringBuilder();
            foreach (var e in bank.entries)
            {
                if (e == null) continue;
                if (e.recording != null && e.recording.loop != null)
                {
                    string loop = AssetDatabase.GetAssetPath(e.recording.loop);
                    if (loop.EndsWith("_loop.wav", System.StringComparison.Ordinal))
                        sb.Append(ImportMachineRecording(e.tool, loop.Substring(0, loop.Length - "_loop.wav".Length) + ".json")).Append('\n');
                }
                foreach (var c in new[] { e.motorLoop, e.contactLoop, e.powerOn, e.powerOff })
                {
                    if (!IsRecorded(c)) continue;
                    string json = Path.ChangeExtension(AssetDatabase.GetAssetPath(c), ".json");
                    if (File.Exists(json)) sb.Append(ImportClip(e.tool, json)).Append('\n');
                }
            }
            return sb.Length > 0 ? sb.ToString() : "[ToolSoundBaker] the bank plays no recordings";
        }

        const string RecordedDir = "Assets/CarpetCleaning/Audio/Recorded/";

        [MenuItem("CarpetCleaning/Audio/Import Shipped Recordings")]
        public static void ImportShippedMenu() { Debug.Log(ImportShippedRecordings()); }

        /// <summary>
        /// Puts every shipped recording into the bank, tool by tool (Docs/AUDIO.md 11; the cuts themselves are
        /// Tools/audio_recordings.py): machine takes replace the machine layer, clips go into their slot. The dry vacuum's
        /// machine is InspectorJ's Dyson take (CC BY 4.0, 11.4) since 2026-09-26; the Samsung take (CC BY-NC, 9.6) is kept
        /// on disk only as a reference and must never ship.
        /// CarpetSceneBuilder runs it when it has to create the bank, so a fresh project sounds like the shipped one; from
        /// the menu it restores the shipped sounds after experiments. All of it takes longer than the CLI's 5 s main-thread
        /// limit: the call times out there, the editor finishes anyway.
        /// </summary>
        public static string ImportShippedRecordings()
        {
            var shipped = new (ToolId tool, bool machine, string json)[]
            {
                (ToolId.Scraper,      false, "scraper_berberscratch_contact.json"),
                (ToolId.DryVacuum,    true,  "dryvacuum_dyson.json"),
                (ToolId.DryVacuum,    false, "dryvacuum_carpetdrag_contact.json"),
                (ToolId.Hose,         true,  "hose_pressurewasher.json"),
                (ToolId.FoamSprayer,  true,  "foamsprayer_aerosol.json"),
                (ToolId.Brush,        false, "brush_floorbuffer_motor.json"),
                (ToolId.Brush,        false, "brush_breaker_on.json"),
                (ToolId.Brush,        false, "brush_breaker_off.json"),
                (ToolId.Brush,        false, "brush_hairbrush_contact.json"),
                (ToolId.Squeegee,     false, "squeegee_waterpush_contact.json"),
                (ToolId.WetVacuum,    true,  "wetvacuum_bissell.json"),
                (ToolId.WetVacuum,    false, "wetvacuum_slurp_contact.json"),
                (ToolId.Dryer,        true,  "dryer_metalfan.json"),
                (ToolId.FloorCleaner, true,  "floorcleaner_pumpspray.json"),
                (ToolId.Mop,          false, "mop_fregona_contact.json"),
            };
            var sb = new StringBuilder();
            foreach (var (tool, machine, json) in shipped)
            {
                string p = RecordedDir + json;
                if (!File.Exists(p)) { sb.Append("[ToolSoundBaker] missing ").Append(p).Append(" (Tools/audio_recordings.py build)\n"); continue; }
                sb.Append(machine ? ImportMachineRecording(tool, p) : ImportClip(tool, p)).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Every clip description in a folder whose name starts with the tool's prefix (e.g. "wetvacuum_").</summary>
        public static string ImportClipsIn(ToolId tool, string dir, string prefix)
        {
            var sb = new StringBuilder();
            foreach (var j in Directory.GetFiles(dir, prefix + "*.json"))
            {
                var cut = JsonUtility.FromJson<ClipCut>(File.ReadAllText(j));
                if (string.IsNullOrEmpty(cut.slot)) continue;              // a machine cut, not a clip
                sb.Append(ImportClip(tool, j.Replace('\\', '/'))).Append('\n');
            }
            return sb.Length > 0 ? sb.ToString() : "[ToolSoundBaker] no clip descriptions " + dir + "/" + prefix + "*.json";
        }

        /// <summary>"slot: credit" lines, one per slot; an empty credit removes the slot's line.</summary>
        static string SetCreditLine(string credits, string slot, string credit)
        {
            var lines = new List<string>();
            foreach (var l in (credits ?? "").Split('\n'))
                if (l.Trim().Length > 0 && !l.StartsWith(slot + ":", System.StringComparison.Ordinal)) lines.Add(l.TrimEnd());
            if (!string.IsNullOrEmpty(credit)) lines.Add(slot + ": " + credit);
            lines.Sort(System.StringComparer.Ordinal);
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Every recording the bank plays and its credit, per tool: the machine take (when in use) and each recorded
        /// clip slot. What the game's credits and the store page have to carry (Docs/AUDIO_CREDITS.md).
        /// </summary>
        public static string Credits()
        {
            var bank = AssetDatabase.LoadAssetAtPath<ToolSoundBank>(BankPath);
            if (bank == null) return "[ToolSoundBaker] no bank at " + BankPath;
            var sb = new StringBuilder();
            foreach (var e in bank.entries)
            {
                if (e == null) continue;
                if (e.UsesRecording) sb.Append(e.tool).Append(" machine: ").Append(e.recording.credit).Append('\n');
                if (!string.IsNullOrEmpty(e.clipCredits))
                    foreach (var l in e.clipCredits.Split('\n'))
                        if (l.Trim().Length > 0) sb.Append(e.tool).Append(' ').Append(l.Trim()).Append('\n');
            }
            return sb.Length > 0 ? sb.ToString() : "(no recordings in the bank)";
        }

        /// <summary>Straight segments between the measured points: the cutter already smoothed them, a spline would overshoot.</summary>
        static AnimationCurve LinearCurve(float[] t, float[] v)
        {
            var c = new AnimationCurve();
            for (int i = 0; i < t.Length; i++) c.AddKey(new Keyframe(t[i], v[i]));
            for (int i = 0; i < c.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.Linear);
            }
            return c;
        }

        /// <summary>
        /// A real recording: uncompressed (a codec would smear the whine and could shift the sample-exact stitch of
        /// start into loop), its stereo kept, preloaded so the first switch-on never waits for a decode.
        /// </summary>
        static void ApplyRecordingImportSettings(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) return;
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.PCM;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            s.preloadAudioData = true;
            imp.defaultSampleSettings = s;
            imp.forceToMono = false;
            imp.loadInBackground = false;
            imp.SaveAndReimport();
        }

        /// <summary>
        /// Empties every slot holding a baked placeholder, which puts the runtime back on live generation there.
        /// Recorded clips (ImportClip) stay.
        /// </summary>
        public static string Clear()
        {
            var bank = AssetDatabase.LoadAssetAtPath<ToolSoundBank>(BankPath);
            if (bank == null) return "[ToolSoundBaker] no bank at " + BankPath;
            foreach (var e in bank.entries)
            {
                if (e == null) continue;
                if (!IsRecorded(e.motorLoop)) e.motorLoop = null;
                if (!IsRecorded(e.contactLoop)) e.contactLoop = null;
                if (!IsRecorded(e.powerOn)) e.powerOn = null;
                if (!IsRecorded(e.powerOff)) e.powerOff = null;
                e.debrisClips = new AudioClip[0];
            }
            bank.toolSwitchClick = null;
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return "[ToolSoundBaker] bank slots cleared - placeholders are generated at runtime again";
        }

        /// <summary>
        /// Length, peak, RMS (dBFS) and loop seam of every generated clip. The seam is |first - last| sample
        /// as a fraction of RMS: a value near or below 1 means the crossfade fold worked and the loop will not
        /// tick. Anything above ~3 is audible.
        /// </summary>
        public static string Report()
        {
            var bank = AssetDatabase.LoadAssetAtPath<ToolSoundBank>(BankPath);
            var sb = new StringBuilder();
            sb.Append("clip,seconds,peak,rms_dbfs,seam_over_rms\n");
            if (bank == null) return sb.Append("no bank at ").Append(BankPath).ToString();

            foreach (var e in bank.entries)
            {
                if (e == null) continue;
                // what actually ships: the assigned clip if there is one, the placeholder only for empty slots
                if (e.recording != null && e.recording.Complete)
                {
                    Measure(e.recording.start, sb, false, false);
                    Measure(e.recording.loop, sb, true, false);
                    Measure(e.recording.stop, sb, false, false);
                }
                MeasureSlot(e.motorLoop, e.motorRecipe, e.tool, sb, true);
                MeasureSlot(e.contactLoop, e.contactRecipe, e.tool, sb, true);
                if (e.powered)
                {
                    Measure(e.powerOn != null ? e.powerOn : ToolSfx.PowerOn(e.tool), sb, false, e.powerOn == null);
                    Measure(e.powerOff != null ? e.powerOff : ToolSfx.PowerOff(e.tool), sb, false, e.powerOff == null);
                }
                if (e.debrisRattle)
                {
                    bool authored = e.debrisClips != null && e.debrisClips.Length > 0;
                    int count = authored ? e.debrisClips.Length : ToolSfx.DebrisVariants;
                    for (int v = 0; v < count; v++)
                    {
                        var c = authored ? e.debrisClips[v] : null;
                        Measure(c != null ? c : ToolSfx.DebrisHit(e.tool, v), sb, false, c == null);
                    }
                }
            }
            Measure(bank.toolSwitchClick != null ? bank.toolSwitchClick : ToolSfx.SwitchClick(), sb, false, bank.toolSwitchClick == null);
            return sb.ToString();
        }

        static void MeasureSlot(AudioClip assigned, SfxRecipe recipe, ToolId tool, StringBuilder sb, bool looping)
        {
            if (assigned != null) Measure(assigned, sb, looping, false);
            else if (recipe != SfxRecipe.None) Measure(ToolSfx.Create(recipe, tool), sb, looping, true);
        }

        /// <summary>`temporary` clips were synthesised for the measurement and are destroyed again; assets are not.</summary>
        static void Measure(AudioClip c, StringBuilder sb, bool looping, bool temporary)
        {
            if (c == null) return;
            var d = new float[c.samples * c.channels];
            if (!c.GetData(d, 0)) { sb.Append(c.name).Append(",unreadable,,,\n"); return; }
            float peak = 0f, sum = 0f;
            for (int i = 0; i < d.Length; i++) { float a = Mathf.Abs(d[i]); if (a > peak) peak = a; sum += d[i] * d[i]; }
            float rms = Mathf.Sqrt(sum / Mathf.Max(1, d.Length));
            float db = 20f * Mathf.Log10(Mathf.Max(rms, 1e-7f));
            // first frame against last frame, channel by channel (the data is interleaved)
            int ch = Mathf.Max(1, c.channels);
            float seam = 0f;
            if (looping && d.Length > ch)
                for (int k = 0; k < ch; k++) seam = Mathf.Max(seam, Mathf.Abs(d[k] - d[d.Length - ch + k]) / Mathf.Max(rms, 1e-7f));
            // invariant: a ru-RU editor writes decimal commas and the CSV falls apart
            sb.Append(c.name).Append(',')
              .Append((c.samples / (float)c.frequency).ToString("F2", CultureInfo.InvariantCulture)).Append(',')
              .Append(peak.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
              .Append(db.ToString("F1", CultureInfo.InvariantCulture)).Append(',')
              .Append(seam.ToString("F2", CultureInfo.InvariantCulture)).Append('\n');
            if (temporary) Object.DestroyImmediate(c);
        }

        // ------------------------------------------------------------------ plumbing
        static string Name(ToolId t, string slot, SfxRecipe r) { return r == SfxRecipe.None ? null : "tool_" + t.ToString().ToLowerInvariant() + "_" + slot; }
        static string Name(ToolId t, string slot) { return "tool_" + t.ToString().ToLowerInvariant() + "_" + slot; }

        /// <summary>A clip that is not one of the baked placeholders in Audio/Generated: a recording, never overwritten by Bake / Clear.</summary>
        static bool IsRecorded(AudioClip c)
        {
            if (c == null) return false;
            string p = AssetDatabase.GetAssetPath(c);
            return !string.IsNullOrEmpty(p) && !p.StartsWith(OutDir + "/", System.StringComparison.Ordinal);
        }

        static AudioClip Load(string name)
        {
            return string.IsNullOrEmpty(name) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(OutDir + "/" + name + ".wav");
        }

        static void ApplyImportSettings(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) return;
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;       // short loops: no streaming, no decode hiccup
            s.compressionFormat = AudioCompressionFormat.PCM;      // placeholders are noise; a codec smears the grain
            s.preloadAudioData = true;                             // per-platform since 2022; the importer-level flag is obsolete
            imp.defaultSampleSettings = s;
            imp.forceToMono = true;
            imp.SaveAndReimport();
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/CarpetCleaning/Audio")) AssetDatabase.CreateFolder("Assets/CarpetCleaning", "Audio");
            if (!AssetDatabase.IsValidFolder(OutDir)) AssetDatabase.CreateFolder("Assets/CarpetCleaning/Audio", "Generated");
        }

        /// <summary>Mono 16-bit PCM WAV.</summary>
        static byte[] EncodeWav(float[] data, int sampleRate)
        {
            int bytes = data.Length * 2;
            using (var ms = new MemoryStream(44 + bytes))
            using (var w = new BinaryWriter(ms))
            {
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16);                       // PCM chunk size
                w.Write((short)1);                 // format = PCM
                w.Write((short)1);                 // channels
                w.Write(sampleRate);
                w.Write(sampleRate * 2);           // byte rate
                w.Write((short)2);                 // block align
                w.Write((short)16);                // bits
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                for (int i = 0; i < data.Length; i++)
                    w.Write((short)Mathf.Clamp(Mathf.RoundToInt(data[i] * 32767f), -32768, 32767));
                w.Flush();
                return ms.ToArray();
            }
        }
    }
}
