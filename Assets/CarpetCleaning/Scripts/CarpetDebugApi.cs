using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>Static scripted-stroke / screenshot API (section 9.1). Every call is safe from an editor eval in Play mode.</summary>
    public static class CarpetDebugApi
    {
        // cached lookups (statics survive play sessions because domain reload is disabled; a destroyed object compares == null and is re-found)
        static CarpetSimulation s_sim; static ToolController s_tools; static FloorSimulation s_floor; static DebrisSystem s_debris; static CarpetHandover s_ho; static ToolAudio s_audio;
        public static CarpetSimulation Sim => s_sim != null ? s_sim : (s_sim = UnityEngine.Object.FindFirstObjectByType<CarpetSimulation>());
        public static ToolController Tools => s_tools != null ? s_tools : (s_tools = UnityEngine.Object.FindFirstObjectByType<ToolController>());
        public static FloorSimulation Floor => s_floor != null ? s_floor : (s_floor = UnityEngine.Object.FindFirstObjectByType<FloorSimulation>());
        public static DebrisSystem Debris => s_debris != null ? s_debris : (s_debris = UnityEngine.Object.FindFirstObjectByType<DebrisSystem>());
        public static CarpetHandover Handover => s_ho != null ? s_ho : (s_ho = UnityEngine.Object.FindFirstObjectByType<CarpetHandover>());
        public static ToolAudio Audio => s_audio != null ? s_audio : (s_audio = UnityEngine.Object.FindFirstObjectByType<ToolAudio>());

        /// <summary>
        /// Crumb-capture event bookkeeping (Docs/AUDIO.md 8). Flushes every pending GPU readback first, so after a
        /// scripted stroke gpuTotal == received + overflow proves no capture was lost or counted twice.
        /// </summary>
        public static string DebrisSuckJson()
        {
            var d = Debris;
            if (d == null) return "{\"error\":\"no DebrisSystem\"}";
            UnityEngine.Rendering.AsyncGPUReadback.WaitAllRequests();
            return "{\"gpuTotal\":" + d.SuckEventsGpuTotal.ToString(CultureInfo.InvariantCulture)
                 + ",\"received\":" + d.SuckEventsReceived.ToString(CultureInfo.InvariantCulture)
                 + ",\"overflow\":" + d.SuckEventsOverflow.ToString(CultureInfo.InvariantCulture) + "}";
        }

        // ------------------------------------------------------------------ crumb suction timing probe (Docs/AUDIO.md 8)
        // Proves the tube rattle lands on the visual: for every chunk a vacuum captures, the GPU predicts when it will
        // vanish into the nozzle (arrivalS); the probe steps the sims one frame at a time, reads the particle pool back
        // after each step and records the step the chunk actually died on. Split into Begin / Run / Report so each CLI
        // call stays under the 5 s main-thread limit.
        sealed class SuckProbe
        {
            public readonly System.Collections.Generic.List<Vector2> path = new System.Collections.Generic.List<Vector2>();
            public int cursor, step;
            public readonly System.Collections.Generic.Dictionary<int, Vector3> live = new System.Collections.Generic.Dictionary<int, Vector3>();   // particle -> (capture step, arrival s, size)
            public readonly System.Collections.Generic.List<float> errMs = new System.Collections.Generic.List<float>();
            public readonly System.Collections.Generic.List<float> arrivalMs = new System.Collections.Generic.List<float>();
            public int events, duplicates, notSuckedAtCapture;
            public System.Action<DebrisSuckEvent> handler;
            public int savedRattleCap;
        }
        static SuckProbe s_probe;

        /// <summary>Reset, scrape crumbs loose over the middle of the carpet, let them settle, lay out a vacuum path over them.</summary>
        public static string SuckTimingBegin(int seed = 1)
        {
            var deb = Debris; var tools = Tools;
            if (deb == null || tools == null || Sim == null) return "{\"error\":\"scene not ready\"}";
            SuckTimingEnd();
            Reset(seed);
            Zigzag((int)ToolId.Scraper, 0.15f, 0.3f, 0.85f, 0.7f, 5, 30);
            RunFrames(90);
            var p = new SuckProbe();
            for (int r = 0; r < 5; r++)                                  // the same rows the scraper worked, 40 frames each
            {
                float v = Mathf.Lerp(0.3f, 0.7f, r / 4f);
                float u0 = (r & 1) == 0 ? 0.1f : 0.9f, u1 = 0.9f + 0.1f - u0;
                for (int i = 0; i <= 40; i++) p.path.Add(new Vector2(Mathf.Lerp(u0, u1, i / 40f), v));
            }
            p.handler = e =>
            {
                p.events++;
                if (p.live.ContainsKey(e.particle)) { p.duplicates++; return; }
                p.live[e.particle] = new Vector3(p.step, e.arrivalS, e.sizeMm);
                p.arrivalMs.Add(e.arrivalS * 1000f);
            };
            deb.DebrisSucked += p.handler;
            var au = Audio;
            if (au != null) { p.savedRattleCap = au.rattleMaxPerFrame; au.rattleMaxPerFrame = 100000; }   // scripted steps share one Unity frame
            s_probe = p;
            int alive = deb.CountAliveSync(out int inRect);
            return "{\"alive\":" + alive.ToString(CultureInfo.InvariantCulture) + ",\"inRect\":" + inRect.ToString(CultureInfo.InvariantCulture)
                 + ",\"pathSteps\":" + p.path.Count.ToString(CultureInfo.InvariantCulture) + "}";
        }

        /// <summary>Advances the probe by up to `steps` frames (vacuum along the path, then idle until every captured chunk died).</summary>
        public static string SuckTimingRun(int steps = 40)
        {
            var p = s_probe; var deb = Debris; var tools = Tools;
            if (p == null || deb == null || tools == null) return "{\"error\":\"call SuckTimingBegin first\"}";
            for (int k = 0; k < steps; k++)
            {
                if (p.cursor > 0 && p.cursor < p.path.Count)
                {
                    if (p.cursor % 41 == 0) tools.SnapHeadingOnNextMove();
                    else tools.StrokeSegment(ToolId.DryVacuum, p.path[p.cursor - 1], p.path[p.cursor], FrameDt, 1f);
                }
                else if (p.cursor >= p.path.Count && p.live.Count == 0) break;
                p.cursor++;
                StepAll(FrameDt);
                UnityEngine.Rendering.AsyncGPUReadback.WaitAllRequests();   // delivers this step's capture events (handler sees p.step)

                var req = UnityEngine.Rendering.AsyncGPUReadback.Request(deb.Particles);
                req.WaitForCompletion();
                if (!req.hasError && p.live.Count > 0)
                {
                    var data = req.GetData<DebrisParticle>();
                    var dead = new System.Collections.Generic.List<int>();
                    foreach (var kv in p.live)
                    {
                        uint state = (data[kv.Key].kindState >> 8) & 0xFFu;
                        if ((int)kv.Value.x == p.step) { if (state != 4u) p.notSuckedAtCapture++; continue; }
                        if (state == 4u) continue;
                        // gone after this step: it was last drawn after step-1, so it vanished (step - capture) frames later
                        float actual = (p.step - kv.Value.x) * FrameDt;
                        p.errMs.Add((actual - kv.Value.y) * 1000f);
                        dead.Add(kv.Key);
                    }
                    foreach (var d in dead) p.live.Remove(d);
                }
                p.step++;
            }
            return "{\"step\":" + p.step.ToString(CultureInfo.InvariantCulture) + ",\"events\":" + p.events.ToString(CultureInfo.InvariantCulture)
                 + ",\"inFlight\":" + p.live.Count.ToString(CultureInfo.InvariantCulture)
                 + ",\"done\":" + (p.cursor >= p.path.Count && p.live.Count == 0 ? "true" : "false") + "}";
        }

        /// <summary>Timing error stats (actual vanish - predicted arrival, ms), event bookkeeping, audio scheduling counters.</summary>
        public static string SuckTimingReport()
        {
            var p = s_probe;
            if (p == null) return "{\"error\":\"no probe\"}";
            float mean = 0f, min = float.MaxValue, max = float.MinValue;
            foreach (var e in p.errMs) { mean += e; min = Mathf.Min(min, e); max = Mathf.Max(max, e); }
            if (p.errMs.Count > 0) mean /= p.errMs.Count; else { min = max = 0f; }
            float aMin = float.MaxValue, aMax = 0f;
            foreach (var a in p.arrivalMs) { aMin = Mathf.Min(aMin, a); aMax = Mathf.Max(aMax, a); }
            if (p.arrivalMs.Count == 0) aMin = 0f;
            var ci = CultureInfo.InvariantCulture;
            return "{\"captured\":" + p.events.ToString(ci) + ",\"measured\":" + p.errMs.Count.ToString(ci)
                 + ",\"duplicates\":" + p.duplicates.ToString(ci) + ",\"notSuckedAtCapture\":" + p.notSuckedAtCapture.ToString(ci)
                 + ",\"errMeanMs\":" + mean.ToString("F2", ci) + ",\"errMinMs\":" + min.ToString("F2", ci) + ",\"errMaxMs\":" + max.ToString("F2", ci)
                 + ",\"arrivalMinMs\":" + aMin.ToString("F1", ci) + ",\"arrivalMaxMs\":" + aMax.ToString("F1", ci)
                 + ",\"events\":" + DebrisSuckJson() + ",\"audio\":" + AudioJson() + "}";
        }

        /// <summary>Unhooks the probe and restores the audio per-frame cap.</summary>
        public static void SuckTimingEnd()
        {
            var p = s_probe;
            if (p == null) return;
            var deb = Debris; if (deb != null && p.handler != null) deb.DebrisSucked -= p.handler;
            var au = Audio; if (au != null && p.savedRattleCap > 0) au.rattleMaxPerFrame = p.savedRattleCap;
            s_probe = null;
        }

        /// <summary>Audio mixer state for scripted checks (Docs/AUDIO.md 6): which loops play, at what gain and pitch.</summary>
        public static string AudioJson()
        {
            var a = Audio;
            return a != null ? a.DebugStateJson() : "{\"error\":\"no ToolAudio\"}";
        }

        /// <summary>The dry vacuum's recorded machine on a virtual clock; see the overload with a ToolId.</summary>
        public static string RecordedMachineScenario(float holdS = 1f, float coastS = 1f, float againS = 7.5f, float reportS = 0.5f)
        {
            return RecordedMachineScenario(ToolId.DryVacuum, holdS, coastS, againS, reportS);
        }

        /// <summary>
        /// A tool's recorded machine (Docs/AUDIO.md 9 and 11: dry vacuum, wet vacuum, dryer, the sprayers) on a virtual
        /// audio clock, because the real one stands still in an unfocused editor: press for holdS, release and coast for
        /// coastS, press again for againS, release and coast out. A private RecordedMachine on a temporary object plays the
        /// bank's recording in 1/60 s steps, scheduled 1000 s ahead of the real clock so nothing can sound. Returns its
        /// state at every event and every reportS in between: phase, speed, and each voice's role, clip, start relative
        /// to now, clip position and gain.
        /// </summary>
        public static string RecordedMachineScenario(ToolId tool, float holdS = 1f, float coastS = 1f, float againS = 7.5f, float reportS = 0.5f)
        {
            var a = Audio;
            var bank = a != null ? a.bank : null;
            if (bank == null) return "{\"error\":\"no ToolAudio / bank\"}";
            var e = bank.Get(tool);
            if (!e.UsesRecording) return "{\"error\":\"" + tool + " plays no recording\"}";
            var host = new GameObject("RecordedMachineScenario") { hideFlags = HideFlags.DontSave };
            double clock = AudioSettings.dspTime + 1000.0;
            RecordedMachine.ClockOverride = () => clock;
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder("[");
            try
            {
                var m = new RecordedMachine(host, "Scenario");
                const float dt = 1f / 60f;
                float t = 0f, nextReport = 0f;
                var steps = new[] { (holdS, true, "press"), (coastS, false, "release"), (againS, true, "press again"), (10f, false, "release") };
                foreach (var (seconds, on, label) in steps)
                {
                    int frames = Mathf.Max(1, Mathf.RoundToInt(seconds / dt));
                    for (int i = 0; i < frames; i++)
                    {
                        m.Tick(on, true, e.recording, 1f, e.motorPitch, 1f, dt);
                        if (i == 0 || t >= nextReport)
                        {
                            if (sb.Length > 1) sb.Append(',');
                            sb.Append("{\"t\":").Append(t.ToString("F3", ci));
                            if (i == 0) sb.Append(",\"event\":\"").Append(label).Append('"');
                            sb.Append(",\"m\":").Append(m.DebugJson()).Append('}');
                            nextReport = t + reportS;
                        }
                        clock += dt; t += dt;
                    }
                }
                m.Silence();
            }
            finally
            {
                RecordedMachine.ClockOverride = null;
                UnityEngine.Object.Destroy(host);
            }
            return sb.Append(']').ToString();
        }

        // ------------------------------------------------------------------ tool heads (Docs/TOOL_MODELS.md 7)
        static ToolModelView s_heads;
        public static ToolModelView Heads => s_heads != null ? s_heads : (s_heads = UnityEngine.Object.FindFirstObjectByType<ToolModelView>());

        /// <summary>Shows the current tool's head at (u, v) over the carpet regardless of the pointer; `working` runs its spinning part.</summary>
        public static bool PinToolHead(int tool, float u, float v, bool working)
        {
            var h = Heads;
            if (h == null) return false;
            SetTool(tool);
            h.DebugPin(true, new Vector2(u, v), working);
            return true;
        }

        public static void UnpinToolHead() { var h = Heads; if (h != null) h.DebugPin(false, Vector2.zero, false); }

        /// <summary>Spin state of the current tool's head: share of spinRpm, angle, this frame's step and the smear copies drawn.</summary>
        public static string ToolHeadJson()
        {
            var h = Heads; var t = Tools;
            if (h == null || t == null) return "{\"error\":\"no ToolModelView\"}";
            float rpm = h.set != null && h.set.TryGet(t.Current, out var e) ? e.spinRpm : 0f;
            return "{\"tool\":" + ((int)t.Current).ToString(CultureInfo.InvariantCulture)
                 + ",\"shown\":" + (h.Shown ? "true" : "false")
                 + ",\"stroking\":" + (t.IsStroking ? "true" : "false")
                 + ",\"stamps\":" + t.StampCount.ToString(CultureInfo.InvariantCulture)
                 + ",\"rpm\":" + F(rpm)
                 + ",\"spin01\":" + F(h.Spin01)
                 + ",\"spinDeg\":" + F(h.SpinDeg)
                 + ",\"stepDeg\":" + F(h.StepDeg)
                 + ",\"dt\":" + F(Time.deltaTime)
                 + ",\"smearCopies\":" + h.SmearCopies.ToString(CultureInfo.InvariantCulture)
                 + ",\"frame\":" + Time.frameCount.ToString(CultureInfo.InvariantCulture) + "}";
        }

        const float FrameDt = 1f / 60f;

        /// <summary>
        /// Full reset to the carpet of `seed` (same picture). dirtLevel defaults to 1, the fully soiled carpet every scripted
        /// test before 2026-09-24 was measured on; a negative level = the seed's own roll, as the game does; 0..1 = the seed's
        /// mix at that level (Docs/DIRT_LEVEL.md). The game's Reset button / R is CarpetHandover.ResetToNewCarpet instead.
        /// </summary>
        public static void Reset(int seed = 1, float dirtLevel = 1f)
        {
            var sim = Sim;
            if (sim == null) { Debug.LogWarning("[CarpetDebugApi] no CarpetSimulation in scene"); return; }
            // scripted recipes run at factory tool tuning unless EconomyDebugApi.LiveTuning(true) (Docs/ECONOMY.md 7)
            ToolTuning.EnterScriptedRun();
            sim.ResetCarpet(seed, true, dirtLevel);
            var tools = Tools;
            if (tools != null) tools.SnapHeadingOnNextMove();   // a scripted run must not inherit the last heading
        }

        /// <summary>
        /// Full reset like Reset(seed, level), with the dirt mix forced (store staging, Docs/DIRT_LEVEL.md 8). crust = share of the
        /// carpet in dried-mud cake patches (1 = every well-soiled cell), mud = ground-in grime, dust = the grey film, spill = share of
        /// the 12 spill stains. A negative value keeps the seed's own roll for that field; level 1 = soil everywhere.
        /// </summary>
        public static void ResetWithProfile(int seed, float level, float crust, float mud = -1f, float dust = -1f, float spill = -1f)
        {
            var sim = Sim;
            if (sim == null) { Debug.LogWarning("[CarpetDebugApi] no CarpetSimulation in scene"); return; }
            // the seed's mix and noise offset (RollDirt at level 1 would return FullySoiled and drop them)
            var d = CarpetSimulation.RollDirt(seed, Mathf.Clamp(level, 0f, 0.999f));
            d.level = Mathf.Clamp01(level);
            if (crust >= 0f) d.crust = Mathf.Clamp01(crust);
            if (mud >= 0f) d.mud = Mathf.Clamp01(mud);
            if (dust >= 0f) d.dust = Mathf.Clamp01(dust);
            if (spill >= 0f) d.spill = Mathf.Clamp01(spill);
            // scripted recipes run at factory tool tuning unless EconomyDebugApi.LiveTuning(true) (Docs/ECONOMY.md 7)
            ToolTuning.EnterScriptedRun();
            sim.ResetCarpet(seed, true, d);
            var tools = Tools;
            if (tools != null) tools.SnapHeadingOnNextMove();   // a scripted run must not inherit the last heading
        }

        /// <summary>The dirt profile of the carpet on the sim (Docs/DIRT_LEVEL.md): seed, level, spread and the mix.</summary>
        public static string DirtJson()
        {
            var sim = Sim;
            if (sim == null) return "{\"error\":\"no simulation\"}";
            var d = sim.CarpetDirt;
            return "{\"seed\":" + sim.Seed.ToString(CultureInfo.InvariantCulture) + ",\"level\":" + F(d.level) + ",\"spread\":" + F(d.spread)
                 + ",\"dust\":" + F(d.dust) + ",\"mud\":" + F(d.mud) + ",\"crust\":" + F(d.crust) + ",\"spill\":" + F(d.spill) + "}";
        }

        /// <summary>The Reset button / R: next picture, next seed, full reset (Docs/DIRT_LEVEL.md).</summary>
        public static void NewCarpetNow() { var h = Handover; if (h != null) h.ResetToNewCarpet(); }

        public static void SetTool(int tool)
        {
            var t = Tools;
            if (t != null) t.SetTool((ToolId)Mathf.Clamp(tool, 0, ToolTable.All.Length - 1));
        }

        /// <summary>Synchronous: splits the segment into `stamps` frames of 1/60 s; each frame StrokeSegment then StepFrame.</summary>
        public static void StrokeNow(int tool, float u0, float v0, float u1, float v1, int stamps = 40, float pressure = 1f)
        {
            var sim = Sim; var tools = Tools;
            if (sim == null || tools == null) { Debug.LogWarning("[CarpetDebugApi] scene not ready"); return; }
            stamps = Mathf.Max(1, stamps);
            // each scripted stroke is its own straight line, so the head starts pointing along it instead of
            // easing round from wherever the previous stroke left it (ToolController heading filter)
            tools.SnapHeadingOnNextMove();
            var a = new Vector2(u0, v0); var b = new Vector2(u1, v1);
            Vector2 prev = a;
            for (int i = 1; i <= stamps; i++)
            {
                Vector2 cur = Vector2.Lerp(a, b, i / (float)stamps);
                tools.StrokeSegment((ToolId)tool, prev, cur, FrameDt, pressure);
                StepAll(FrameDt);
                prev = cur;
            }
        }

        /// <summary>rows horizontal strokes alternating direction from v0 to v1 inside [u0, u1].</summary>
        public static void Zigzag(int tool, float u0, float v0, float u1, float v1, int rows, int stampsPerRow = 30, float pressure = 1f)
        {
            rows = Mathf.Max(1, rows);
            for (int r = 0; r < rows; r++)
            {
                float v = rows > 1 ? Mathf.Lerp(v0, v1, r / (float)(rows - 1)) : 0.5f * (v0 + v1);
                if ((r & 1) == 0) StrokeNow(tool, u0, v, u1, v, stampsPerRow, pressure);
                else StrokeNow(tool, u1, v, u0, v, stampsPerRow, pressure);
            }
        }

        /// <summary>Stationary hold stamps for `seconds` (hose / sprayer / dryer / vacuums).</summary>
        public static void Hold(int tool, float u, float v, float seconds)
        {
            var sim = Sim; var tools = Tools;
            if (sim == null || tools == null) { Debug.LogWarning("[CarpetDebugApi] scene not ready"); return; }
            int frames = Mathf.Max(1, Mathf.RoundToInt(seconds / FrameDt));
            var uv = new Vector2(u, v);
            for (int i = 0; i < frames; i++)
            {
                tools.StrokeSegment((ToolId)tool, uv, uv, FrameDt, 1f);
                StepAll(FrameDt);
            }
        }

        public static void RunFrames(int n, float dt = 1f / 60f)
        {
            var sim = Sim;
            if (sim == null) return;
            for (int i = 0; i < n; i++) StepAll(dt);
        }

        // ------------------------------------------------------------------ foam (Docs/FOAM_LOOK.md)

        /// <summary>Repeatable foam scenes after Reset(1): 1 spray centre 2 s; 2 spray + brush smear; 3 spray + wet vac; 4 edge spray + outward brush (floor foam); 5 Zigzag(3) whole carpet.</summary>
        public static string FoamScene(int preset)
        {
            Reset(1);
            switch (preset)
            {
                case 1: Hold(3, 0.5f, 0.5f, 2f); break;
                case 2: Hold(3, 0.5f, 0.5f, 2f); StrokeNow(4, 0.35f, 0.5f, 0.75f, 0.5f, 40); break;
                case 3: Hold(3, 0.5f, 0.5f, 2f); Hold(6, 0.55f, 0.5f, 2f); break;
                case 4: Hold(3, 0.04f, 0.5f, 2f); StrokeNow(4, 0.12f, 0.5f, -0.06f, 0.5f, 40); RunFrames(60); break;
                case 5: Zigzag(3, 0.1f, 0.1f, 0.9f, 0.9f, 8, 30); break;
                default: break;
            }
            return StatsJson();
        }

        /// <summary>Max foam F over the carpet (sync readback of the soap texture, channel y).</summary>
        public static float FoamMaxF()
        {
            var sim = Sim;
            if (sim == null || !sim.Ready) return -1f;
            var px = ReadbackPixels(sim.Soap, out _, out _);
            float m = 0f;
            for (int i = 0; i < px.Length; i++) if (px[i].g > m) m = px[i].g;
            return m;
        }

        /// <summary>Shows / hides the two foam surfaces (CarpetRoot/FoamSurface, FloorRoot/FloorFoam) for A/B captures; returns how many were found.</summary>
        public static int SetFoamVisible(bool visible)
        {
            int n = 0;
            foreach (var name in new[] { "FoamSurface", "FloorFoam" })
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) { mr.enabled = visible; n++; }
            }
            return n;
        }

        // ------------------------------------------------------------------ hand-over (Docs/HANDOVER.md)

        /// <summary>Starts the fly-out. force skips the clean-and-dry gate.</summary>
        public static bool HandoverNow(bool force = true) { var h = Handover; return h != null && h.Begin(force); }
        public static bool TakeNewCarpetNow() { var h = Handover; return h != null && h.TakeNewCarpet(); }
        /// <summary>Makes the real HUD button appear without cleaning a carpet first.</summary>
        public static void SetForceWin(bool on) { var h = Handover; if (h != null) h.debugForceWin = on; }
        public static void HandoverStep(float dt) { var h = Handover; if (h != null) h.Tick(dt); }
        /// <summary>Places the flight at t01 without accumulating time, so a capture right after is deterministic.</summary>
        public static void HandoverSeek(float t01) { var h = Handover; if (h != null) h.Seek(t01); }
        public static void HandoverSnap() { var h = Handover; if (h != null) h.SnapToIdle(); }

        public static string HandoverJson()
        {
            var h = Handover;
            if (h == null) return "{\"error\":\"no handover\"}";
            var sim = Sim;
            var sb = new StringBuilder(160);
            sb.Append('{');
            sb.Append("\"state\":\"").Append(h.State.ToString()).Append("\",\"t\":").Append(F(h.Phase01));
            sb.Append(",\"canHand\":").Append(h.CanHandOver ? "true" : "false");
            sb.Append(",\"canTake\":").Append(h.CanTakeNew ? "true" : "false");
            sb.Append(",\"pic\":").Append(h.PictureIndex.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"picName\":\"").Append(h.PictureName ?? "").Append("\"");
            sb.Append(",\"pictures\":").Append((h.ActivePictures != null ? h.ActivePictures.Length : 0).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"seed\":").Append((sim != null ? sim.Seed : -1).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"dirtLevel\":").Append(sim != null ? F(sim.DirtLevel) : "null");
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>The four carpet corners in viewport space: y > 1 on all four proves the carpet is off the top of the frame.</summary>
        public static string StageCornersJson()
        {
            var view = UnityEngine.Object.FindFirstObjectByType<CarpetRenderer>();
            var cam = Camera.main;
            if (view == null || cam == null) return "{\"error\":\"no stage or camera\"}";
            var st = view.Stage;
            var size = CarpetSimulation.CarpetSizeM;
            var sb = new StringBuilder(220);
            sb.Append("{\"corners\":[");
            float minY = 1e9f;
            for (int i = 0; i < 4; i++)
            {
                float sx = (i == 0 || i == 3) ? -0.5f : 0.5f;
                float sz = (i < 2) ? -0.5f : 0.5f;
                Vector3 world = st.TransformPoint(new Vector3(sx * size.x, 0f, sz * size.y));
                Vector3 vp = cam.WorldToViewportPoint(world);
                if (i > 0) sb.Append(',');
                sb.Append('[').Append(F(vp.x)).Append(',').Append(F(vp.y)).Append(',').Append(F(vp.z)).Append(']');
                if (vp.y < minY) minY = vp.y;
            }
            sb.Append("],\"minY\":").Append(F(minY)).Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Live particle count, how many of them sit inside the carpet rect (the ring head never decreases, so it cannot show
        /// this), live particles per kind [chunk, dust, drop] and the splash drops in the air (above 25 mm).
        /// </summary>
        public static string DebrisAliveJson()
        {
            var d = Debris;
            if (d == null) return "{\"error\":\"no debris system\"}";
            var kinds = new int[3];
            int alive = d.CountAliveSync(out int inRect, kinds, out int dropsAir);
            var c = CultureInfo.InvariantCulture;
            return "{\"alive\":" + alive.ToString(c) + ",\"inRect\":" + inRect.ToString(c)
                 + ",\"kinds\":[" + kinds[0].ToString(c) + "," + kinds[1].ToString(c) + "," + kinds[2].ToString(c) + "]"
                 + ",\"dropsAir\":" + dropsAir.ToString(c) + "}";
        }

        // ------------------------------------------------------------------ Steam (Docs/STEAM.md 6)
        /// <summary>Steam state plus the rich-presence mapping, which is computed even when Steam is not running.</summary>
        public static string SteamJson()
        {
            var rp = UnityEngine.Object.FindAnyObjectByType<SteamRichPresence>();
            if (rp != null) rp.Evaluate();
            var sim = Sim; var tools = Tools;
            var sb = new StringBuilder(320);
            sb.Append("{\"service\":").Append(SteamService.Instance != null ? "true" : "false");
            sb.Append(",\"ready\":").Append(SteamService.IsReady ? "true" : "false");
            sb.Append(",\"appId\":").Append(SteamService.RunningAppId.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"configAppId\":").Append(SteamConfig.AppId.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"persona\":\"").Append(Esc(SteamService.PersonaName)).Append('"');
            sb.Append(",\"error\":\"").Append(Esc(SteamService.LastError)).Append('"');
            sb.Append(",\"overlay\":").Append(SteamService.OverlayActive ? "true" : "false");
            sb.Append(",\"simPaused\":").Append(sim != null && sim.Paused ? "true" : "false");
            sb.Append(",\"uiPaused\":").Append(sim != null && sim.UiPaused ? "true" : "false");
            sb.Append(",\"toolsSuspended\":").Append(tools != null && tools.Suspended ? "true" : "false");
            sb.Append(",\"simTime\":").Append(F(sim != null ? sim.SimTime : -1f));
            if (rp != null)
            {
                sb.Append(",\"rp\":{\"token\":\"").Append(rp.Token).Append("\",\"clean\":\"").Append(rp.Clean);
                sb.Append("\",\"sentToken\":\"").Append(rp.SentToken).Append("\",\"sentClean\":\"").Append(rp.SentClean);
                sb.Append("\",\"pushes\":").Append(rp.PushCount.ToString(CultureInfo.InvariantCulture)).Append('}');
            }
            sb.Append(",\"cloud\":").Append(SteamAchievements.CloudJson());   // Docs/STEAM.md 11
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Runs the overlay-activated path without Steam (the real overlay rarely draws over the Editor).</summary>
        public static bool SteamSimulateOverlay(bool active)
        {
            var s = SteamService.Instance;
            if (s == null) return false;
            s.SetOverlay(active);
            return true;
        }

        static string Esc(string s) { return string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\""); }

        // ------------------------------------------------------------------ achievements (Docs/ACHIEVEMENTS.md)
        static SteamAchievements AchievementsComponent
        {
            get { return SteamAchievements.Instance != null ? SteamAchievements.Instance : UnityEngine.Object.FindAnyObjectByType<SteamAchievements>(); }
        }

        static Economy EconomyComponent
        {
            get { return Economy.Instance != null ? Economy.Instance : UnityEngine.Object.FindAnyObjectByType<Economy>(); }
        }

        /// <summary>Steam flags, the save's side (earned, tools used, carpets), what Steam holds (null = not read or Steam not
        /// active), progress, stores and the names the published schema lacks (SteamAchievements.DebugJson).</summary>
        public static string AchievementsJson()
        {
            var a = AchievementsComponent;
            if (a != null) return a.DebugJson();
            return "{\"component\":false,\"steamReady\":" + (SteamService.IsReady ? "true" : "false")
                 + ",\"error\":\"no SteamAchievements (CarpetCleaning/Build Scene)\"}";
        }

        /// <summary>
        /// Pure: what a hand-over with these numbers unlocks. PayoutResult.Compute with the live EconomySettings (else the
        /// defaults), then Achievements.PayoutUnlocks with the component's boneDryPeakWetness (else the default). No side effects.
        /// </summary>
        public static string AchievementsPreview(float jobValue, float clean, float dry, float seconds, float peakWetness)
        {
            var e = EconomyComponent;
            EconomySettings tmp = null;
            var s = e != null && e.settings != null ? e.settings : (tmp = ScriptableObject.CreateInstance<EconomySettings>());
            var r = PayoutResult.Compute(s, clean, dry, seconds, jobValue);
            if (tmp != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(tmp);
                else UnityEngine.Object.DestroyImmediate(tmp);
            }
            var a = AchievementsComponent;
            float boneDryPeak = a != null ? a.boneDryPeakWetness : Achievements.DefaultBoneDryPeakWetness;
            var list = new System.Collections.Generic.List<AchievementId>(Achievements.Count);
            Achievements.PayoutUnlocks(r, peakWetness, boneDryPeak, list);
            var sb = new StringBuilder(200);
            sb.Append("{\"V\":").Append(F(r.jobValue)).Append(",\"par\":").Append(F(r.parSeconds));
            sb.Append(",\"soil\":\"").Append(PayoutResult.SoilName(r.jobValue)).Append('"');
            sb.Append(",\"boneDryPeak\":").Append(F(boneDryPeak));
            sb.Append(",\"unlocks\":[");
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(Achievements.Api(list[i])).Append('"');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// Tests only: clears the save's earned achievements and tools-used bits and writes the save (Steam keeps its copy).
        /// Snapshot carpet_save_editor.json first and restore it afterwards: the user plays between turns.
        /// </summary>
        public static string AchievementsForgetLocal()
        {
            var e = EconomyComponent;
            if (e == null || e.Data == null) return "{\"error\":\"no Economy\"}";
            var d = e.Data;
            if (d.achieved != null) for (int i = 0; i < d.achieved.Length; i++) d.achieved[i] = 0;
            d.toolsUsed = 0;
            e.SaveNow();
            return AchievementsJson();
        }

        /// <summary>Runs the Start sync with Steam again (needs Steam active: a player, or steamInEditor in the Editor).</summary>
        public static string AchievementsSteamSync()
        {
            var a = AchievementsComponent;
            if (a != null) a.SyncWithSteam();
            return AchievementsJson();
        }

        /// <summary>Lets Editor Play push to Steam (the developer's own account). Follow with AchievementsSteamSync.</summary>
        public static string AchievementsSetSteamInEditor(bool on)
        {
            var a = AchievementsComponent;
            if (a != null) a.steamInEditor = on;
            return AchievementsJson();
        }

        /// <summary>
        /// ClearAchievement + StoreStats on the REAL Steam account, even with steamInEditor off. Developer account only, to
        /// undo a test unlock; the save keeps its copy (AchievementsForgetLocal).
        /// </summary>
        public static string AchievementsSteamClear(string api)
        {
            var a = AchievementsComponent;
            if (a == null) return AchievementsJson();
            string result = a.DebugClearOnSteam(api);
            return "{\"result\":\"" + Esc(result) + "\",\"state\":" + a.DebugJson() + "}";
        }

        // ------------------------------------------------------------------ pause menu (Docs/PAUSE_MENU.md)
        static PauseMenu Menu => UnityEngine.Object.FindAnyObjectByType<PauseMenu>();

        /// <summary>Menu state: open, page, credit lines built, volume (live and saved), the modal flags, Quit presses.</summary>
        public static string MenuJson()
        {
            var m = Menu;
            return m != null ? m.DebugJson() : "{\"error\":\"no PauseMenu (CarpetCleaning/Build Scene)\"}";
        }

        /// <summary>Opens / closes the menu like Esc; credits = true shows the Credits page.</summary>
        public static string MenuOpen(bool open, bool credits = false)
        {
            var m = Menu;
            if (m == null) return MenuJson();
            m.SetOpen(open);
            if (open && credits) m.ShowCredits(true);
            return m.DebugJson();
        }

        /// <summary>Sets the master volume as the slider does (saved to PlayerPrefs) and moves the slider.</summary>
        public static string MenuVolume(float v)
        {
            var m = Menu;
            if (m == null) return MenuJson();
            m.SetVolume(v, true);
            return m.DebugJson();
        }

        // ------------------------------------------------------------------ localization (Docs/LOCALIZATION.md)
        static HudLocalizer Localizer => UnityEngine.Object.FindAnyObjectByType<HudLocalizer>();

        /// <summary>Language state: code, how it was chosen, saved choice, Steam / OS language, key counts, registered labels, font.</summary>
        public static string LocJson()
        {
            var l = Localizer;
            return l != null ? l.DebugJson() : "{\"code\":\"" + Loc.Code + "\",\"error\":\"no HudLocalizer (CarpetCleaning/Build Scene)\"}";
        }

        /// <summary>Switches the language. remember = false (default) leaves the player's saved choice alone: tests and
        /// captures must not change what the game starts in next time.</summary>
        public static string SetLanguage(string code, bool remember = false)
        {
            bool ok = Loc.SetLanguage(code, remember);
            return ok ? LocJson() : "{\"error\":\"unknown language '" + Esc(code) + "'\"}";
        }

        /// <summary>Picks a language through the menu's drop-down, exactly as a click on its row (saved like the player's pick).</summary>
        public static string LanguagePick(string code)
        {
            var m = Menu;
            if (m == null || m.Picker == null) return "{\"error\":\"no language picker\"}";
            bool ok = m.Picker.DebugPick(code);
            return ok ? LocJson() : "{\"error\":\"pick failed\"}";
        }

        /// <summary>Opens / closes the language list of the menu (the menu must be open).</summary>
        public static string LanguageList(bool open)
        {
            var m = Menu;
            if (m == null || m.Picker == null) return "{\"error\":\"no language picker\"}";
            if (open) m.Picker.Open(); else m.Picker.Close();
            return m.DebugJson();
        }

        /// <summary>Forgets the saved language: the next start (and now) follows Steam / the OS again.</summary>
        public static string LanguageForget()
        {
            Loc.ClearSaved();
            return LocJson();
        }

        /// <summary>Every shown label whose text does not fit its box in the current language (HudLocalizer.OverflowJson).</summary>
        public static string LocOverflow()
        {
            var l = Localizer;
            return l != null ? l.OverflowJson() : "{\"error\":\"no HudLocalizer\"}";
        }

        // ------------------------------------------------------------------ inspection lamp (Docs/HIGHLIGHTS.md)
        static SurfaceHighlight s_lamp;
        public static SurfaceHighlight Lamp => s_lamp != null ? s_lamp : (s_lamp = UnityEngine.Object.FindFirstObjectByType<SurfaceHighlight>());

        /// <summary>
        /// Switches a lamp (0 moisture / Z, 1 dirt / X) WITHOUT the ownership check, for captures. instant = no fade, no scan
        /// ring, globals pushed at once (a capture right after sees the final look).
        /// </summary>
        public static string SetHighlight(int which, bool on, bool instant = true)
        {
            var l = Lamp;
            if (l == null) return "{\"error\":\"no SurfaceHighlight\"}";
            l.Set((ShopItem)which, on, instant);
            return HighlightJson();
        }

        /// <summary>What the Z / X key does in the game (refuses an item that is not owned, plays the sounds).</summary>
        public static string ToggleHighlight(int which)
        {
            var l = Lamp;
            if (l == null) return "{\"error\":\"no SurfaceHighlight\"}";
            l.Toggle((ShopItem)which);
            return HighlightJson();
        }

        public static string HighlightJson()
        {
            var l = Lamp;
            if (l == null) return "{\"error\":\"no SurfaceHighlight\"}";
            var g = Shader.GetGlobalVector(SurfaceHighlight.HighlightId);
            var sb = new StringBuilder(256);
            sb.Append("{\"moisture\":{\"on\":").Append(l.MoistureOn ? "true" : "false").Append(",\"owned\":").Append(l.Owned(ShopItem.MoistureHighlight) ? "true" : "false")
              .Append(",\"amount\":").Append(F(l.Amount(ShopItem.MoistureHighlight))).Append(",\"sweep\":").Append(F(l.Sweep(ShopItem.MoistureHighlight))).Append('}');
            sb.Append(",\"dirt\":{\"on\":").Append(l.DirtOn ? "true" : "false").Append(",\"owned\":").Append(l.Owned(ShopItem.DirtHighlight) ? "true" : "false")
              .Append(",\"amount\":").Append(F(l.Amount(ShopItem.DirtHighlight))).Append(",\"sweep\":").Append(F(l.Sweep(ShopItem.DirtHighlight))).Append('}');
            sb.Append(",\"global\":[").Append(F(g.x)).Append(',').Append(F(g.y)).Append(',').Append(F(g.z)).Append(',').Append(F(g.w)).Append(']');
            // the carpet cut (Docs/HIGHLIGHTS.md 4): what the HUD shows, the cut a cell must exceed (1e30 = nothing lit), and the
            // histogram means next to the stats they must match (K_Reduce)
            var sim = Sim;
            if (sim != null)
            {
                var s = sim.Stats;
                float wcap = sim.settings != null ? sim.settings.W_CAP : 6f;
                sb.Append(",\"shown\":{\"clean\":").Append(CarpetSimulation.ShownPercent(sim.Cleanliness).ToString(CultureInfo.InvariantCulture))
                  .Append(",\"dry\":").Append(CarpetSimulation.ShownPercent(1f - sim.Wetness).ToString(CultureInfo.InvariantCulture)).Append('}');
                sb.Append(",\"cut\":{\"dirt\":").Append(F(sim.LampCutDirt)).Append(",\"wet\":").Append(F(sim.LampCutWet)).Append('}');
                sb.Append(",\"mean\":{\"dirtStats\":").Append(F(CarpetSimulation.DirtScore(s))).Append(",\"dirtHist\":").Append(F(sim.HistMean(0)))
                  .Append(",\"wetStats\":").Append(F(s.pileWater + s.water / wcap)).Append(",\"wetHist\":").Append(F(sim.HistMean(1))).Append('}');
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static void SetTimeScale(float s) { var sim = Sim; if (sim != null) sim.TimeScale = s; }
        public static void SetInput(bool enabled) { var t = Tools; if (t != null) t.InputEnabled = enabled; }

        /// <summary>One scripted frame of the whole chain in LateUpdate order: carpet (100) -> debris (105) -> floor (110).</summary>
        public static void StepAll(float dt)
        {
            var sim = Sim;
            if (sim == null) return;
            sim.StepFrame(dt);
            var d = Debris; if (d != null) d.StepFrame(dt);
            var f = Floor; if (f != null) f.StepFrame(dt);
            var a = Audio; if (a != null) a.StepFrame(dt);      // scripted strokes drive the mix like real ones
            var h = Heads; if (h != null) h.StepFrame(dt);      // and spin the brush like real ones
        }

        // ------------------------------------------------------------------ live (real-frame) scripting, for mid-flight screenshots
        static DebugLiveRunner Live
        {
            get
            {
                var r = UnityEngine.Object.FindFirstObjectByType<DebugLiveRunner>();
                if (r == null) r = new GameObject("DebugLiveRunner").AddComponent<DebugLiveRunner>();
                return r;
            }
        }
        /// <summary>Queue a stroke that plays over real frames (the normal LateUpdate chain runs, so debris/floor react like in gameplay).</summary>
        public static void LiveStroke(int tool, float u0, float v0, float u1, float v1, float seconds, float pressure = 1f)
        { Live.Enqueue(new DebugLiveRunner.Item { tool = tool, from = new Vector2(u0, v0), to = new Vector2(u1, v1), seconds = Mathf.Max(seconds, 0.02f), pressure = pressure, kind = 0 }); }
        public static void LiveHold(int tool, float u, float v, float seconds)
        { Live.Enqueue(new DebugLiveRunner.Item { tool = tool, from = new Vector2(u, v), to = new Vector2(u, v), seconds = Mathf.Max(seconds, 0.02f), pressure = 1f, kind = 1 }); }
        public static void LiveWait(float seconds) { Live.Enqueue(new DebugLiveRunner.Item { seconds = Mathf.Max(seconds, 0f), kind = 2 }); }
        public static bool LiveBusy() { var r = UnityEngine.Object.FindFirstObjectByType<DebugLiveRunner>(); return r != null && r.Busy; }
        public static void LiveClear() { var r = UnityEngine.Object.FindFirstObjectByType<DebugLiveRunner>(); if (r != null) r.Clear(); }

        static string F(float v) { return (float.IsNaN(v) || float.IsInfinity(v)) ? "null" : v.ToString("G6", CultureInfo.InvariantCulture); }

        /// <summary>Forces K_Reduce + WaitForCompletion and returns a JSON object of the section 9.1 keys.</summary>
        public static string StatsJson()
        {
            var sim = Sim;
            if (sim == null) return "{\"error\":\"no simulation\"}";
            var s = sim.RefreshStatsSync();
            float dirtTotal = s.crust + s.mud + s.stain + s.grit + s.slurry + s.foamDirt;
            var sb = new StringBuilder(256);
            sb.Append('{');
            sb.Append("\"crust\":").Append(F(s.crust)).Append(",\"mud\":").Append(F(s.mud)).Append(",\"stain\":").Append(F(s.stain));
            sb.Append(",\"grit\":").Append(F(s.grit)).Append(",\"slurry\":").Append(F(s.slurry)).Append(",\"foamDirt\":").Append(F(s.foamDirt));
            sb.Append(",\"water\":").Append(F(s.water)).Append(",\"pileWater\":").Append(F(s.pileWater)).Append(",\"soap\":").Append(F(s.soap));
            sb.Append(",\"foam\":").Append(F(s.foam)).Append(",\"clean\":").Append(F(sim.Cleanliness)).Append(",\"wet\":").Append(F(sim.Wetness));
            sb.Append(",\"dirtTotal\":").Append(F(dirtTotal)).Append(",\"wetCells\":").Append(F(s.wetCells)).Append(",\"pooledCells\":").Append(F(s.pooledCells));
            sb.Append(",\"seed\":").Append(sim.Seed.ToString(CultureInfo.InvariantCulture)).Append(",\"dirtLevel\":").Append(F(sim.DirtLevel));
            sb.Append(",\"valid\":").Append(s.valid ? "true" : "false").Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Means over the carpet-UV rect [u0,u1]x[v0,v1] of the pile state and its wet-pressed set (Docs/FIBER_SET.md):
        /// |b|, bend along +u/+v, f, w from _Fiber and |bSet|, fSet from _FiberSet. Blocking float readback.
        /// </summary>
        public static string FiberProbeJson(float u0, float v0, float u1, float v1)
        {
            var sim = Sim;
            if (sim == null || !sim.Ready) return "{\"error\":\"no simulation\"}";
            var fib = ReadFloatRT(sim.Fiber);
            var set = ReadFloatRT(sim.FiberSet);
            int w = sim.Fiber.width, h = sim.Fiber.height;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(u0, u1) * w), 0, w - 1), x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(u0, u1) * w), x0 + 1, w);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(v0, v1) * h), 0, h - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(v0, v1) * h), y0 + 1, h);
            double bl = 0, bx = 0, by = 0, f = 0, wet = 0, sl = 0, fs = 0; int n = 0;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    Color c = fib[y * w + x], s = set[y * w + x];
                    bl += Mathf.Sqrt(c.r * c.r + c.g * c.g); bx += c.r; by += c.g; f += c.b; wet += c.a;
                    sl += Mathf.Sqrt(s.r * s.r + s.g * s.g); fs += s.b; n++;
                }
            double k = 1.0 / Mathf.Max(n, 1);
            var sb = new StringBuilder(192);
            sb.Append("{\"b\":").Append(F((float)(bl * k))).Append(",\"bu\":").Append(F((float)(bx * k))).Append(",\"bv\":").Append(F((float)(by * k)));
            sb.Append(",\"f\":").Append(F((float)(f * k))).Append(",\"w\":").Append(F((float)(wet * k)));
            sb.Append(",\"bSet\":").Append(F((float)(sl * k))).Append(",\"fSet\":").Append(F((float)(fs * k))).Append(",\"cells\":").Append(n).Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Surface load over the carpet-UV rect [u0,u1]x[v0,v1] (Docs/SQUEEGEE.md): mean / max of free water h (mm), foam F,
        /// pile moisture w and slurry s, and the fraction of cells still carrying foam (F > 0.05) or water (h > 0.05 mm).
        /// Blocking float readback.
        /// </summary>
        public static string SurfaceProbeJson(float u0, float v0, float u1, float v1)
        {
            var sim = Sim;
            if (sim == null || !sim.Ready) return "{\"error\":\"no simulation\"}";
            var flu = ReadFloatRT(sim.Fluid);
            var soa = ReadFloatRT(sim.Soap);
            var fib = ReadFloatRT(sim.Fiber);
            int w = sim.Fluid.width, h = sim.Fluid.height;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(u0, u1) * w), 0, w - 1), x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(u0, u1) * w), x0 + 1, w);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(v0, v1) * h), 0, h - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(v0, v1) * h), y0 + 1, h);
            double sh = 0, sF = 0, sw = 0, ss = 0; float mh = 0, mF = 0, mw = 0, ms = 0; int n = 0, nF = 0, nH = 0;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    int i = y * w + x;
                    float hh = flu[i].r, s = flu[i].a, fo = soa[i].g, pw = fib[i].a;
                    sh += hh; sF += fo; sw += pw; ss += s; n++;
                    mh = Mathf.Max(mh, hh); mF = Mathf.Max(mF, fo); mw = Mathf.Max(mw, pw); ms = Mathf.Max(ms, s);
                    if (fo > 0.05f) nF++;
                    if (hh > 0.05f) nH++;
                }
            double k = 1.0 / Mathf.Max(n, 1);
            var sb = new StringBuilder(256);
            sb.Append("{\"h\":").Append(F((float)(sh * k))).Append(",\"hMax\":").Append(F(mh));
            sb.Append(",\"F\":").Append(F((float)(sF * k))).Append(",\"FMax\":").Append(F(mF));
            sb.Append(",\"w\":").Append(F((float)(sw * k))).Append(",\"wMax\":").Append(F(mw));
            sb.Append(",\"s\":").Append(F((float)(ss * k))).Append(",\"sMax\":").Append(F(ms));
            sb.Append(",\"foamCells\":").Append(F(nF * (float)k)).Append(",\"waterCells\":").Append(F(nH * (float)k));
            sb.Append(",\"cells\":").Append(n).Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// What the squeegee has pushed off the carpet and is still waiting in the spill ring to pour onto the floor
        /// (K_PlowDrain), in carpet-cell units summed over the ring: water mm, s, sigma, F, Fd. Blocking buffer read.
        /// </summary>
        public static string PlowSpillJson()
        {
            var sim = Sim;
            if (sim == null || !sim.Ready || sim.PlowAccBuffer == null) return "{\"error\":\"no simulation\"}";
            var data = new uint[CarpetSimulation.PlowAccLength];
            sim.PlowAccBuffer.GetData(data);
            int W = CarpetSimulation.SimW + 2, H = CarpetSimulation.SimH + 2, C = CarpetSimulation.PlowChannels;
            float[] scale = { 16384f, 4096f, 4096f, 16384f, 4096f, 32f };   // = kPlowScale in CarpetSim.compute
            var ring = new double[C]; var inner = new double[C];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    bool onRing = x == 0 || y == 0 || x == W - 1 || y == H - 1;
                    int b = (y * W + x) * C;
                    for (int c = 0; c < C; c++) { if (onRing) ring[c] += data[b + c] / scale[c]; else inner[c] += data[b + c] / scale[c]; }
                }
            var sb = new StringBuilder(200);
            sb.Append("{\"water\":").Append(F((float)ring[0])).Append(",\"s\":").Append(F((float)ring[1])).Append(",\"sigma\":").Append(F((float)ring[2]));
            sb.Append(",\"F\":").Append(F((float)ring[3])).Append(",\"Fd\":").Append(F((float)ring[4]));
            sb.Append(",\"innerLeft\":").Append(F((float)(inner[0] + inner[1] + inner[2] + inner[3] + inner[4]))).Append('}');
            return sb.ToString();
        }

        static Color[] ReadFloatRT(RenderTexture src)
        {
            int w = src.width, h = src.height;
            var tmp = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat);
            var prev = RenderTexture.active;
            Graphics.Blit(src, tmp);
            RenderTexture.active = tmp;
            var tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);
            var px = tex.GetPixels();
            UnityEngine.Object.Destroy(tex);
            return px;
        }

        /// <summary>which in {fluid, soap, dirt, crust, fiber, fiberset, bed}: channel-mapped 8-bit PNG via a float readback.</summary>
        public static void SaveDebugView(string which, string pngPath)
        {
            var sim = Sim;
            if (sim == null || !sim.Ready) { Debug.LogWarning("[CarpetDebugApi] scene not ready"); return; }
            RenderTexture src;
            which = (which ?? "").ToLowerInvariant();
            switch (which)
            {
                case "fluid": src = sim.Fluid; break;
                case "soap": src = sim.Soap; break;
                case "dirt": src = sim.Dirt; break;
                case "crust": src = sim.Crust; break;
                case "fiber": src = sim.Fiber; break;
                case "fiberset": src = sim.FiberSet; break;
                case "bed": src = sim.Bed; break;
                default: Debug.LogWarning("[CarpetDebugApi] unknown view " + which); return;
            }
            int w = src.width, h = src.height;
            var tmp = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat);
            var prev = RenderTexture.active;
            Graphics.Blit(src, tmp);
            RenderTexture.active = tmp;
            var tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);

            var px = tex.GetPixels();
            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            var outPx = new Color32[px.Length];
            float hmax = sim.settings != null ? sim.settings.H_MAX : 10f;
            float fmax = sim.settings != null ? sim.settings.F_MAX : 12f;
            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i]; Color m;
                switch (which)
                {
                    case "fluid": m = new Color(c.r / hmax, (c.g + 350f) / 700f, (c.b + 350f) / 700f, Mathf.Clamp01(c.a)); break;
                    case "soap": m = new Color(c.r, c.g / fmax, c.b, c.a / 30f); break;
                    case "fiber": m = new Color((c.r + 1f) * 0.5f, (c.g + 1f) * 0.5f, c.b, c.a); break;
                    case "fiberset": m = new Color((c.r + 1f) * 0.5f, (c.g + 1f) * 0.5f, c.b, 1f); break;
                    case "bed": m = new Color((c.r + 2f) * 0.25f, (c.r + 2f) * 0.25f, (c.r + 2f) * 0.25f, 1f); break;
                    default: m = c; break;
                }
                outPx[i] = new Color32(ToByte(m.r), ToByte(m.g), ToByte(m.b), ToByte(m.a));
            }
            outTex.SetPixels32(outPx);
            outTex.Apply(false);
            var bytes = outTex.EncodeToPNG();
            UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(outTex);
            try
            {
                var dir = Path.GetDirectoryName(pngPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(pngPath, bytes);
                Debug.Log("[CarpetDebugApi] saved " + which + " view to " + pngPath);
            }
            catch (System.Exception e) { Debug.LogException(e); }
        }

        // ------------------------------------------------------------------ floor + debris (FLOOR_SYSTEM.md 6)

        /// <summary>Floor sums via FloorSimulation.FloorStatsSync() (K_FReduce + WaitForCompletion) as a JSON object; also carries DebrisCount().</summary>
        public static string FloorStatsJson()
        {
            var floor = Floor;
            if (floor == null) return "{\"error\":\"no floor simulation\"}";
            FloorStats s = floor.FloorStatsSync();
            var sb = new StringBuilder(256);
            sb.Append('{');
            sb.Append("\"water\":").Append(F(s.water)).Append(",\"slurry\":").Append(F(s.slurry)).Append(",\"foam\":").Append(F(s.foam));
            sb.Append(",\"foamDirt\":").Append(F(s.foamDirt)).Append(",\"dust\":").Append(F(s.dust)).Append(",\"residue\":").Append(F(s.residue));
            sb.Append(",\"wetFilm\":").Append(F(s.wetFilm)).Append(",\"pooledCells\":").Append(F(s.pooledCells));
            sb.Append(",\"detergent\":").Append(F(s.detergent)).Append(",\"dirtyCells\":").Append(F(s.dirtyCells));
            sb.Append(",\"debris\":").Append(DebrisCount().ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"valid\":").Append(s.valid ? "true" : "false").Append('}');
            return sb.ToString();
        }

        /// <summary>Total debris particles spawned since the last reset (readback of the ring head _Head[0]); -1 without a DebrisSystem.</summary>
        public static int DebrisCount()
        {
            var d = Debris;
            if (d == null) return -1;
            return (int)d.ReadSpawnedTotalSync();
        }

        /// <summary>
        /// which in {ffluid, fsoap, fdeposit, fbed}: channel-mapped 8-bit PNG of a floor state texture (768x768).
        /// ffluid: r = h/F_H_MAX, g,b = (u,v + 600)/1200, a = s. fsoap: r = sigma, g = F/F_F_MAX, b = Fd, a = Fage/30.
        /// fdeposit: raw (r dust, g residue, b wetFilm, a detergent film). fbed: (mm + 45)/90 grey.
        /// </summary>
        public static void SaveFloorView(string which, string pngPath)
        {
            var floor = Floor;
            if (floor == null) { Debug.LogWarning("[CarpetDebugApi] no FloorSimulation in scene"); return; }
            which = (which ?? "").ToLowerInvariant();
            RenderTexture src;
            switch (which)
            {
                case "ffluid": src = floor.Fluid; break;
                case "fsoap": src = floor.Soap; break;
                case "fdeposit": src = floor.Deposit; break;
                case "fbed": src = floor.Bed; break;
                default: Debug.LogWarning("[CarpetDebugApi] unknown floor view " + which + " (ffluid|fsoap|fdeposit|fbed)"); return;
            }
            if (src == null) { Debug.LogWarning("[CarpetDebugApi] floor view " + which + " is not allocated yet"); return; }
            float hmax = floor.settings != null ? floor.settings.F_H_MAX : 12f;
            float fmax = floor.settings != null ? floor.settings.F_F_MAX : 12f;
            hmax = Mathf.Max(hmax, 1e-3f); fmax = Mathf.Max(fmax, 1e-3f);

            var px = ReadbackPixels(src, out int w, out int h);
            var outPx = new Color32[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i]; Color m;
                switch (which)
                {
                    case "ffluid": m = new Color(c.r / hmax, (c.g + 600f) / 1200f, (c.b + 600f) / 1200f, c.a); break;
                    case "fsoap": m = new Color(c.r, c.g / fmax, c.b, c.a / 30f); break;
                    case "fbed": { float g = (c.r + 45f) / 90f; m = new Color(g, g, g, 1f); break; }
                    default: m = c; break;
                }
                outPx[i] = new Color32(ToByte(m.r), ToByte(m.g), ToByte(m.b), ToByte(m.a));
            }
            WritePng(outPx, w, h, pngPath, which);
        }

        /// <summary>Blit to an ARGBFloat temporary and ReadPixels (works for R32 single-channel RTs too: g,b read 0, a reads 1).</summary>
        static Color[] ReadbackPixels(RenderTexture src, out int w, out int h)
        {
            w = src.width; h = src.height;
            var tmp = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat);
            var prev = RenderTexture.active;
            Graphics.Blit(src, tmp);
            RenderTexture.active = tmp;
            var tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);
            var px = tex.GetPixels();
            UnityEngine.Object.Destroy(tex);
            return px;
        }

        static void WritePng(Color32[] outPx, int w, int h, string pngPath, string label)
        {
            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            outTex.SetPixels32(outPx);
            outTex.Apply(false);
            var bytes = outTex.EncodeToPNG();
            UnityEngine.Object.Destroy(outTex);
            try
            {
                var dir = Path.GetDirectoryName(pngPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(pngPath, bytes);
                Debug.Log("[CarpetDebugApi] saved " + label + " view to " + pngPath);
            }
            catch (System.Exception e) { Debug.LogException(e); }
        }

        static byte ToByte(float v)
        {
            if (float.IsNaN(v)) return 255;
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);
        }
    }

    /// <summary>Plays queued strokes/holds over real frames (Update), so the whole carpet -> debris -> floor chain runs as in gameplay.</summary>
    public sealed class DebugLiveRunner : MonoBehaviour
    {
        public struct Item { public int tool; public Vector2 from, to; public float seconds, pressure; public int kind; } // 0 stroke, 1 hold, 2 wait
        readonly System.Collections.Generic.Queue<Item> _queue = new System.Collections.Generic.Queue<Item>();
        bool _active; Item _cur; float _t; Vector2 _prev;
        public bool Busy => _active || _queue.Count > 0;
        public void Enqueue(Item it) { _queue.Enqueue(it); }
        public void Clear() { _queue.Clear(); _active = false; }

        void Update()
        {
            var sim = CarpetDebugApi.Sim; var tools = CarpetDebugApi.Tools;
            if (sim == null || tools == null) return;
            if (!_active)
            {
                if (_queue.Count == 0) return;
                _cur = _queue.Dequeue(); _t = 0f; _prev = _cur.from; _active = true;
                tools.InputEnabled = false;
                tools.SnapHeadingOnNextMove();
            }
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f) * sim.TimeScale;
            _t += dt;
            float a = Mathf.Clamp01(_t / Mathf.Max(_cur.seconds, 1e-3f));
            if (_cur.kind == 0)
            {
                Vector2 cur = Vector2.Lerp(_cur.from, _cur.to, a);
                tools.StrokeSegment((ToolId)_cur.tool, _prev, cur, dt, _cur.pressure);
                _prev = cur;
            }
            else if (_cur.kind == 1) tools.StrokeSegment((ToolId)_cur.tool, _cur.from, _cur.from, dt, _cur.pressure);
            if (_t >= _cur.seconds) { _active = false; if (_queue.Count == 0) tools.InputEnabled = true; }
        }
    }
}
