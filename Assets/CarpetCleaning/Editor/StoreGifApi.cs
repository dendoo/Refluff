using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace CarpetCleaning
{
    /// <summary>
    /// Store-description clip and trailer takes (Docs/STORE_CLIPS.md 5), driven from `unity command eval` in Play mode. A
    /// take is REAL player-loop frames: EditorApplication.Step() (dt = Time.fixedDeltaTime: 0.02 s by default, 1/fps with
    /// SeqBegin's fps; Update, LateUpdate, UI Toolkit and render run even while the Editor is unfocused) with the tool driven by ToolController's scripted pointer, so the head, the heading filter,
    /// the brush spin, the HUD fade, the flight, the sparkles and the water ripples (_Time) all behave as in play. Every
    /// `every` Steps (2 = 25 fps at the default step) the game camera is rendered like StoreShotApi does (ARGBHalf HDR, then an sRGB blit) and
    /// the crop is written as SteamStore/_work/clips/&lt;take&gt;/f0001.png; a HUD take adds f0001_ui.png, the UI Toolkit
    /// panel (premultiplied RGBA) captured at the Game view size and cropped to the same area. seq.json records the Step
    /// of every frame, the render and crop sizes and the cadence. Tools/steam_art/make_clips.py composites, downscales and
    /// encodes the MP4. Flow:
    ///   SeqBegin(...) ; Stroke / Move / Hold / Hover / Up / Away / Tool / Call / Each / Every (queue) ;
    ///   Run() until "done":true ; SeqEnd() until "pendingPng":0.
    /// An eval times out after ~5 s of main-thread work, so Run stops at budgetMs and the next Run carries on. Keep
    /// EditorApplication.isPaused = true between evals (SeqBegin sets it); unpause before `editor_stop`. The Editor audio is
    /// muted during a take and put back by SeqEnd / SeqAbort, and a take left open is aborted on leaving Play mode and
    /// before a script reload (both also put Time.fixedDeltaTime back). audioLog = the take also writes audio.jsonl (the
    /// mix per Step + every AudioTap event) and audio_clips.json, for the offline foley render: the Editor cannot record
    /// the game's audio itself (its dsp clock stands still while unfocused).
    /// </summary>
    [InitializeOnLoad]
    public static class StoreGifApi
    {
        static readonly CultureInfo C = CultureInfo.InvariantCulture;
        /// <summary>
        /// What one EditorApplication.Step advances Time.deltaTime and unscaledDeltaTime by: Time.fixedDeltaTime (measured
        /// 2026-09-26; Time.captureDeltaTime is ignored by Step). 0.02 s with the project default, 1/fps for a take started
        /// with SeqBegin(fps: n). Read at SeqBegin, so the queue builders called after it (StepsFor) count the take's own Steps.
        /// </summary>
        static float StepDt { get { return s_stepDt; } }
        static float s_stepDt = 0.02f, s_prevFixedDt = 0.02f;
        static bool s_fpsSet;

        /// <summary>SteamStore/_work/clips: one folder of frames per take.</summary>
        public static string ClipDir => Path.Combine(StoreShotApi.WorkDir, "clips");

        static StoreGifApi()
        {
            // statics survive Play sessions here (domain reload is off), so a take must never outlive Play mode or a script
            // reload: the Editor would stay muted, the tool would ignore the mouse and the targets would leak
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.ExitingPlayMode || s == PlayModeStateChange.EnteredEditMode) SeqAbort();
            };
            AssemblyReloadEvents.beforeAssemblyReload += SeqAbort;
        }

        [InitializeOnEnterPlayMode]
        static void OnEnterPlayMode() { SeqAbort(); s_debris = null; }

        // ------------------------------------------------------------------ take state
        static bool s_active, s_hud, s_async = true, s_flip, s_prevMute, s_prevMarketing, s_seqWritten = true, s_aborted;
        static string s_name, s_dir;
        static int s_w, s_h, s_uiW, s_uiH, s_every = 2, s_steps, s_frames, s_lastCapStep;
        static RectInt s_crop, s_cropAsked, s_uiCrop;
        static double s_dtSum, s_udtSum, s_captureMs, s_stepMs;
        static RenderTexture s_hdr, s_ldr, s_ui;
        static Texture2D s_rgb, s_rgba;
        static DebrisSystem s_debris;
        static readonly List<Task> s_tasks = new List<Task>();
        static readonly List<int> s_frameSteps = new List<int>();
        static readonly List<Vector2Int> s_cadence = new List<Vector2Int>();   // (first frame number, every) per change
        static readonly List<string> s_errors = new List<string>();

        // ------------------------------------------------------------------ timeline
        enum Kind { Move, Call, Each }

        sealed class Item
        {
            public Kind kind;
            public Vector2 a, b;
            public int steps;
            public bool down, right, present = true;
            public System.Action call;
            public System.Action<float> each;
        }

        static readonly Queue<Item> s_queue = new Queue<Item>();
        static Item s_cur;
        static int s_i;                                    // Steps done inside s_cur
        static Vector2 s_qUv = new Vector2(0.5f, 0.5f);   // where the queued pointer ends up, for Up()

        static ToolController Tools => CarpetDebugApi.Tools;

        static int StepsFor(float seconds) { return Mathf.Max(1, Mathf.RoundToInt(seconds / StepDt)); }

        /// <summary>
        /// Starts a take. The game camera renders at w x h (up to 4096; 3840 x 2160 = 2x supersampling for a 1560 px clip)
        /// and the crop (cx, cy, cw, ch; top-left origin, cw or ch 0 = the whole frame) is saved every `every` Steps
        /// (2 = 25 fps). hud = also the UI Toolkit panel: it lays out only at the Game view size (uiW x uiH, 2560 x 1440), so
        /// it is captured at that size and cut with the crop scaled by uiW / w; the render must have the Game view's
        /// aspect, and the crop is widened to the nearest pixels that land on whole panel pixels (3840 -> 2560: multiples of 3).
        /// Clears the take folder, points the scripted pointer off the play area (no head until the first Move), pauses the
        /// Editor and mutes its audio. fps &gt; 0 = one Step is 1/fps s for this take (Time.fixedDeltaTime, put back by
        /// SeqEnd / SeqAbort; 60 with every 1 = a 60 fps take); 0 keeps the current step (0.02 s). Queue the take's items
        /// after SeqBegin: their Step counts use its step. audioLog = also write audio.jsonl + audio_clips.json.
        /// </summary>
        public static string SeqBegin(string name, int w = 3840, int h = 2160, int cx = 0, int cy = 0, int cw = 0, int ch = 0,
                                      bool hud = false, int every = 2, bool asyncPng = true, int uiW = 2560, int uiH = 1440,
                                      int fps = 0, bool audioLog = false)
        {
            // a take still running is dropped (no seq.json); a released one (SeqEnd returned "pendingPng" > 0) is finished
            // first: its PNG writers must be done before this folder is cleared, and its seq.json keeps the frame timing
            if (s_active) SeqAbort();
            s_queue.Clear(); s_cur = null;
            int pending = WaitPending(3000);   // an eval times out after ~5 s
            if (pending == 0) FinishReleased();
            if (pending > 0)
                return "{\"error\":\"the previous take is still writing " + pending.ToString(C) + " PNGs: call SeqEnd, then SeqBegin again\"}";
            if (!EditorApplication.isPlaying) return "{\"error\":\"not in Play mode\"}";
            if (Camera.main == null) return "{\"error\":\"no main camera\"}";
            if (Tools == null) return "{\"error\":\"no ToolController\"}";
            if (string.IsNullOrEmpty(name) || name.Contains("..") || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return "{\"error\":\"take name must be a plain folder name\"}";
            if (w < 16 || h < 16 || w > 4096 || h > 4096) return "{\"error\":\"render size must be 16..4096\"}";
            if (fps < 0 || fps > 240) return "{\"error\":\"fps must be 0 (keep the step) or 1..240\"}";
            if (cw <= 0 || ch <= 0) { cx = 0; cy = 0; cw = w; ch = h; }
            if (cx < 0 || cy < 0 || cw < 2 || ch < 2 || cx + cw > w || cy + ch > h) return "{\"error\":\"crop outside the render\"}";
            var asked = new RectInt(cx, cy, cw, ch);
            var crop = asked;
            var uiCrop = new RectInt();
            if (hud)
            {
                if (uiW < 16 || uiH < 16 || (long)w * uiH != (long)h * uiW)
                    return "{\"error\":\"a HUD take needs the render at the Game view aspect (uiW x uiH)\"}";
                int g = Gcd(w, uiW), p = w / g, q = uiW / g;   // render px : panel px = p : q
                int x0 = cx / p * p, y0 = cy / p * p;
                int x1 = Mathf.Min(w, (cx + cw + p - 1) / p * p), y1 = Mathf.Min(h, (cy + ch + p - 1) / p * p);
                crop = new RectInt(x0, y0, x1 - x0, y1 - y0);
                uiCrop = new RectInt(x0 / p * q, y0 / p * q, crop.width / p * q, crop.height / p * q);
            }

            s_name = name; s_w = w; s_h = h; s_uiW = uiW; s_uiH = uiH;
            s_crop = crop; s_cropAsked = asked; s_uiCrop = uiCrop;
            s_hud = hud; s_every = Mathf.Max(1, every); s_async = asyncPng;
            s_steps = 0; s_frames = 0; s_lastCapStep = 0; s_dtSum = 0; s_udtSum = 0; s_captureMs = 0; s_stepMs = 0;
            s_frameSteps.Clear(); s_errors.Clear(); s_cadence.Clear(); s_queue.Clear(); s_cur = null; s_i = 0;
            s_cadence.Add(new Vector2Int(1, s_every));
            s_debris = null;

            s_dir = Path.Combine(ClipDir, name);
            Directory.CreateDirectory(s_dir);
            foreach (var f in Directory.GetFiles(s_dir, "f*.png")) File.Delete(f);
            File.Delete(Path.Combine(s_dir, "seq.json"));
            File.Delete(Path.Combine(s_dir, "audio.jsonl"));          // a stale audio log must never pass for this take's
            File.Delete(Path.Combine(s_dir, "audio_clips.json"));
            string clipDump = Path.Combine(s_dir, "_clips");
            if (Directory.Exists(clipDump)) foreach (var f in Directory.GetFiles(clipDump, "*.wav")) File.Delete(f);
            s_seqWritten = false; s_aborted = false;

            s_hdr = StoreShotApi.NewCaptureTarget(w, h, true);
            s_ldr = StoreShotApi.NewCaptureTarget(w, h, false);
            s_rgb = new Texture2D(crop.width, crop.height, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
            s_prevMarketing = StoreShotApi.MarketingHudOn();
            if (hud)
            {
                s_ui = StoreShotApi.NewCaptureTarget(uiW, uiH, false);
                s_rgba = new Texture2D(uiCrop.width, uiCrop.height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                StoreShotApi.MarketingHud(true);
            }
            s_prevMute = EditorUtility.audioMasterMute;
            EditorUtility.audioMasterMute = true;
            EditorApplication.isPaused = true;
            s_prevFixedDt = Time.fixedDeltaTime;
            s_fpsSet = fps > 0;
            if (s_fpsSet) Time.fixedDeltaTime = 1f / fps;
            s_stepDt = Time.fixedDeltaTime;
            var t = Tools;
            s_qUv = t.ScriptedPointer ? t.ScriptedUv : new Vector2(0.5f, 0.5f);
            s_active = true;
            if (audioLog)
            {
                try { BeginAudioLog(); }
                catch (System.Exception e) { s_errors.Add("audio log: " + e.Message); EndAudioLog(false); }
            }
            Pointer(s_qUv, false, false, false);   // from here on the mouse is never read
            return "{\"take\":\"" + name + "\",\"dir\":\"" + s_dir.Replace("\\", "/") + "\",\"render\":[" + w.ToString(C) + "," + h.ToString(C)
                 + "],\"crop\":" + RectJson(crop) + ",\"cropAsked\":" + RectJson(asked)
                 + (hud ? ",\"ui\":[" + uiW.ToString(C) + "," + uiH.ToString(C) + "],\"uiCrop\":" + RectJson(uiCrop) : "")
                 + ",\"every\":" + s_every.ToString(C) + ",\"stepDt\":" + StepDt.ToString("R", C)
                 + ",\"stepFps\":" + (1f / StepDt).ToString("0.###", C) + ",\"audioLog\":" + (s_audioLog ? "true" : "false") + "}";
        }

        static int Gcd(int a, int b) { while (b != 0) { int r = a % b; a = b; b = r; } return a; }

        static string RectJson(RectInt r)
        {
            return "[" + r.x.ToString(C) + "," + r.y.ToString(C) + "," + r.width.ToString(C) + "," + r.height.ToString(C) + "]";
        }

        // ------------------------------------------------------------------ queue builders (nothing runs until Run)
        /// <summary>The pointer glides from (u0,v0) to (u1,v1) in `seconds` of game time, pressed while `down`.</summary>
        public static void Move(float u0, float v0, float u1, float v1, float seconds, bool down = true, bool right = false)
        {
            s_queue.Enqueue(new Item { kind = Kind.Move, a = new Vector2(u0, v0), b = new Vector2(u1, v1), steps = StepsFor(seconds), down = down, right = right });
            s_qUv = new Vector2(u1, v1);
        }

        /// <summary>
        /// A player's straight drag: snap the heading, hover in along the line for `approachMm` (the head turns onto it
        /// like it does under a real pointer), then press and drag to (u1,v1) in `seconds`.
        /// </summary>
        public static void Stroke(float u0, float v0, float u1, float v1, float seconds, float approachMm = 40f, bool right = false)
        {
            Vector2 mm = CarpetSimulation.CarpetSizeM * 1000f;
            Vector2 d = Vector2.Scale(new Vector2(u1 - u0, v1 - v0), mm);
            Vector2 back = d.sqrMagnitude > 1e-6f ? -d.normalized * approachMm : Vector2.zero;
            Vector2 s = new Vector2(u0 + back.x / mm.x, v0 + back.y / mm.y);
            Call(() => { var t = Tools; if (t != null) t.SnapHeadingOnNextMove(); });
            Move(s.x, s.y, u0, v0, 0.16f, false);
            Move(u0, v0, u1, v1, seconds, true, right);
        }

        /// <summary>Pressed and still at (u, v): the tool works one spot (hose puddle, foam mound).</summary>
        public static void Hold(float u, float v, float seconds) { Move(u, v, u, v, seconds, true); }

        /// <summary>Button up at (u, v): the head rests there.</summary>
        public static void Hover(float u, float v, float seconds) { Move(u, v, u, v, seconds, false); }

        /// <summary>Button up, pointer where the queue left it (the head stays, a spinning pad coasts down).</summary>
        public static void Up(float seconds) { Move(s_qUv.x, s_qUv.y, s_qUv.x, s_qUv.y, seconds, false); }

        /// <summary>Pointer off the play area: no head, no cursor (a still shot of the scene).</summary>
        public static void Away(float seconds)
        {
            s_queue.Enqueue(new Item { kind = Kind.Move, a = s_qUv, b = s_qUv, steps = StepsFor(seconds), present = false });
        }

        /// <summary>Switches the tool at this point of the take, like its hotkey.</summary>
        public static void Tool(int tool) { Call(() => CarpetDebugApi.SetTool(tool)); }

        /// <summary>Runs `a` at this point of the take, before the next Step (HandoverNow, TakeNewCarpetNow, ShopOpen, ...).</summary>
        public static void Call(System.Action a) { s_queue.Enqueue(new Item { kind = Kind.Call, call = a }); }

        /// <summary>
        /// For `seconds`, calls perStep(local time) before every Step (e.g. t => EconomyDebugApi.PayoutSeek(0.3f + t)).
        /// The pointer keeps whatever the previous item left it at.
        /// </summary>
        public static void Each(float seconds, System.Action<float> perStep)
        {
            s_queue.Enqueue(new Item { kind = Kind.Each, steps = StepsFor(seconds), each = perStep });
        }

        /// <summary>Capture cadence from here on: a frame every n Steps (at 0.02 s: 2 = 25 fps, 3 = 16.7, 4 = 12.5; at fps 60: 1 = 60, 2 = 30). seq.json keeps the steps.</summary>
        public static void Every(int n)
        {
            Call(() =>
            {
                int e = Mathf.Max(1, n);
                if (e == s_every) return;
                s_every = e;
                s_cadence.Add(new Vector2Int(s_frames + 1, e));
            });
        }

        // ------------------------------------------------------------------ running
        /// <summary>
        /// Plays the queue with real frames for at most budgetMs of wall time, capturing every `every` Steps. Call again
        /// until "done":true. dtMean / udtMean report what Time.deltaTime / unscaledDeltaTime really were per Step.
        /// </summary>
        public static string Run(int budgetMs = 3500)
        {
            if (!s_active) return "{\"error\":\"no take (SeqBegin)\"}";
            if (!EditorApplication.isPlaying) { SeqAbort(); return "{\"error\":\"not in Play mode\"}"; }
            var sw = Stopwatch.StartNew();
            EditorApplication.isPaused = true;
            int steps0 = s_steps, frames0 = s_frames;
            while (sw.ElapsedMilliseconds < budgetMs)
            {
                if (s_cur == null)
                {
                    if (s_queue.Count == 0) break;
                    s_cur = s_queue.Dequeue();
                    s_i = 0;
                    if (s_cur.kind == Kind.Call)
                    {
                        try { if (s_cur.call != null) s_cur.call(); }
                        catch (System.Exception e) { s_errors.Add("call: " + e.Message); }
                        s_cur = null;
                        continue;
                    }
                }
                if (s_cur.kind == Kind.Move)
                {
                    float a = (s_i + 1) / (float)s_cur.steps;   // the last Step of the item lands exactly on b
                    Pointer(Vector2.Lerp(s_cur.a, s_cur.b, a), s_cur.present, s_cur.down, s_cur.right);
                }
                else if (s_cur.kind == Kind.Each && s_cur.each != null)
                {
                    try { s_cur.each(s_i * StepDt); } catch (System.Exception e) { s_errors.Add("each: " + e.Message); }
                }
                if (s_audioLog) s_clock = s_clockBase + (s_steps + 1) * (double)StepDt;   // the virtual audio clock of this frame
                var st = Stopwatch.StartNew();
                EditorApplication.Step();
                s_stepMs += st.Elapsed.TotalMilliseconds;
                s_steps++;
                s_dtSum += Time.deltaTime;
                s_udtSum += Time.unscaledDeltaTime;
                if (s_audioLog)
                {
                    // a failing log line must not throw out of Run mid-item (the Step is done; the item would repeat it)
                    try { AudioLogStep(); }
                    catch (System.Exception e) { if (s_errors.Count < 50) s_errors.Add("audio.jsonl at step " + s_steps.ToString(C) + ": " + e.Message); }
                }
                if (s_steps - s_lastCapStep >= s_every) CaptureFrame();
                if (++s_i >= s_cur.steps) s_cur = null;
            }
            bool done = s_cur == null && s_queue.Count == 0;
            if (s_audioOut != null) { try { s_audioOut.Flush(); } catch (System.Exception e) { s_errors.Add("audio.jsonl: " + e.Message); } }
            return "{\"done\":" + (done ? "true" : "false") + ",\"steps\":" + (s_steps - steps0).ToString(C) + ",\"frames\":" + (s_frames - frames0).ToString(C)
                 + ",\"totalFrames\":" + s_frames.ToString(C) + ",\"totalSteps\":" + s_steps.ToString(C) + ",\"queued\":" + s_queue.Count.ToString(C)
                 + ",\"ms\":" + sw.ElapsedMilliseconds.ToString(C)
                 + ",\"stepMsMean\":" + (s_steps > 0 ? s_stepMs / s_steps : 0).ToString("0.0", C)
                 + ",\"captureMsMean\":" + (s_frames > 0 ? s_captureMs / s_frames : 0).ToString("0.0", C)
                 + ",\"dtMean\":" + (s_steps > 0 ? s_dtSum / s_steps : 0).ToString("0.0000", C)
                 + ",\"udtMean\":" + (s_steps > 0 ? s_udtSum / s_steps : 0).ToString("0.0000", C)
                 + ",\"pendingPng\":" + Pending().ToString(C) + ",\"errors\":" + s_errors.Count.ToString(C) + "}";
        }

        static void Pointer(Vector2 uv, bool present, bool down, bool right)
        {
            var t = Tools;
            if (t == null) return;
            t.ScriptedPointer = true;
            t.ScriptedUv = present ? uv : new Vector2(-5f, -5f);   // outside the floor domain = not hovering, no head
            t.ScriptedDown = present && down;
            t.ScriptedRight = right;
        }

        /// <summary>Captures one frame now, outside the Step cadence (e.g. a first frame before any Step).</summary>
        public static string SeqFrame()
        {
            if (!s_active) return "{\"error\":\"no take (SeqBegin)\"}";
            CaptureFrame();
            return "{\"frame\":" + s_frames.ToString(C) + "}";
        }

        // ------------------------------------------------------------------ capture
        /// <summary>ReadPixels counts rows from the bottom; crops here are top-left based.</summary>
        static Rect ReadRect(RectInt r, int fullH) { return new Rect(r.x, fullH - r.y - r.height, r.width, r.height); }

        static void CaptureFrame()
        {
            var cam = Camera.main;
            if (cam == null) { s_errors.Add("no main camera"); return; }
            var sw = Stopwatch.StartNew();
            if (s_debris == null) s_debris = Object.FindAnyObjectByType<DebrisSystem>();
            var prevActive = RenderTexture.active;
            try
            {
                // the live LateUpdate draws are still queued for this frame; RenderHdr adds only a kind that is missing
                StoreShotApi.RenderHdr(cam, s_hdr, s_ldr, s_debris);
                RenderTexture.active = s_ldr;
                s_rgb.ReadPixels(ReadRect(s_crop, s_h), 0, 0, false);
                s_frames++;
                s_lastCapStep = s_steps;
                s_frameSteps.Add(s_steps);
                string stem = Path.Combine(s_dir, "f" + s_frames.ToString("0000", C));
                Save(s_rgb, GraphicsFormat.R8G8B8_SRGB, 3, stem + ".png");
                if (s_hud)
                {
                    // the panel as the last Step laid it out, at the Game view size, premultiplied (EconomyHud.DebugCapture)
                    var root = HudRoot();
                    RenderTexture.active = s_ui;
                    GL.Clear(true, true, Color.clear);
                    bool ok = root != null && VisualElementCaptureExtensions.TryCaptureIntoRenderTexture(root, s_ui);
                    if (ok)
                    {
                        RenderTexture.active = s_ui;
                        s_rgba.ReadPixels(ReadRect(s_uiCrop, s_uiH), 0, 0, false);
                        Save(s_rgba, GraphicsFormat.R8G8B8A8_SRGB, 4, stem + "_ui.png");
                    }
                    else s_errors.Add("hud capture failed at frame " + s_frames.ToString(C));
                }
            }
            finally
            {
                RenderTexture.active = prevActive;
            }
            s_captureMs += sw.Elapsed.TotalMilliseconds;
        }

        static VisualElement HudRoot()
        {
            var hud = EconomyDebugApi.Hud;
            var doc = hud != null ? hud.GetComponent<UIDocument>() : null;
            var root = doc != null ? doc.rootVisualElement : null;
            return root != null && root.panel != null ? root.panel.visualTree : null;
        }

        /// <summary>
        /// PNG off the main thread: ImageConversion.EncodeArrayToPNG is documented thread safe, so only the raw copy stays in
        /// the eval. At most 2 x cores encodes are in flight (a 3120 x 1302 frame holds 12 MB until written); past that the
        /// eval waits for the oldest. asyncPng = false encodes inline (tens of ms per frame).
        /// </summary>
        static void Save(Texture2D tex, GraphicsFormat fmt, int bpp, string path)
        {
            byte[] raw = tex.GetRawTextureData<byte>().ToArray();   // ReadPixels filled the CPU copy; no Apply needed
            int w = tex.width, h = tex.height;
            if (s_flip) FlipRows(raw, w * bpp, h);
            if (!s_async)
            {
                File.WriteAllBytes(path, ImageConversion.EncodeArrayToPNG(raw, fmt, (uint)w, (uint)h));
                return;
            }
            int maxInFlight = Mathf.Max(4, 2 * System.Environment.ProcessorCount);
            while (Pending() >= maxInFlight)
            {
                try { s_tasks[0].Wait(1000); } catch (System.AggregateException) { }   // PruneTasks records the fault
            }
            s_tasks.Add(Task.Run(() => File.WriteAllBytes(path, ImageConversion.EncodeArrayToPNG(raw, fmt, (uint)w, (uint)h))));
        }

        static void FlipRows(byte[] a, int stride, int h)
        {
            var tmp = new byte[stride];
            for (int y = 0, z = h - 1; y < z; y++, z--)
            {
                System.Buffer.BlockCopy(a, y * stride, tmp, 0, stride);
                System.Buffer.BlockCopy(a, z * stride, a, y * stride, stride);
                System.Buffer.BlockCopy(tmp, 0, a, z * stride, stride);
            }
        }

        /// <summary>Flip raw rows before encoding. Set it only if Probe reports "arrayPng":"flipped".</summary>
        public static void SetFlip(bool on) { s_flip = on; }

        /// <summary>Drops finished PNG tasks (recording failures) and returns how many are still writing.</summary>
        static int Pending()
        {
            for (int i = s_tasks.Count - 1; i >= 0; i--)
            {
                var t = s_tasks[i];
                if (!t.IsCompleted) continue;
                if (t.IsFaulted) s_errors.Add("png: " + (t.Exception != null ? t.Exception.GetBaseException().Message : "failed"));
                s_tasks.RemoveAt(i);
            }
            return s_tasks.Count;
        }

        // ------------------------------------------------------------------ ending
        /// <summary>
        /// Waits up to waitMs for the PNG writers, gives the mouse back, restores the Editor audio, the fps label and
        /// Time.fixedDeltaTime, closes the audio log (audio_clips.json + WAV dumps), frees the targets and writes seq.json (frame -> Step index, sizes, crop, cadence), so the encoder can give every frame
        /// its exact time. Stays paused. Call again while "pendingPng" > 0.
        /// </summary>
        public static string SeqEnd(int waitMs = 4000)
        {
            if (s_dir == null) return "{\"error\":\"no take\"}";
            if (s_aborted) return "{\"error\":\"the take was aborted (SeqAbort, Play stopped or a script reload): no seq.json\"}";
            int pending = WaitPending(waitMs);
            if (s_active) Release();
            bool wrote = pending == 0 && FinishReleased();
            float span = s_frameSteps.Count > 0 ? (s_frameSteps[s_frameSteps.Count - 1] - s_frameSteps[0] + s_every) * StepDt : 0f;
            return "{\"frames\":" + s_frames.ToString(C) + ",\"steps\":" + s_steps.ToString(C) + ",\"seconds\":" + span.ToString("0.00", C)
                 + ",\"pendingPng\":" + pending.ToString(C) + ",\"seqJson\":" + (s_seqWritten ? "true" : "false") + ",\"wroteNow\":" + (wrote ? "true" : "false")
                 + ",\"errors\":" + s_errors.Count.ToString(C) + ",\"dir\":\"" + s_dir.Replace("\\", "/") + "\"}";
        }

        /// <summary>Waits up to waitMs for the PNG writers; returns how many are still writing.</summary>
        static int WaitPending(int waitMs)
        {
            var sw = Stopwatch.StartNew();
            while (Pending() > 0 && sw.ElapsedMilliseconds < waitMs)
            {
                try { s_tasks[0].Wait((int)System.Math.Max(1, waitMs - sw.ElapsedMilliseconds)); } catch (System.AggregateException) { }
            }
            return Pending();
        }

        /// <summary>
        /// Writes seq.json for a take that was released (SeqEnd) but not written yet, once its PNGs are done. Never for a
        /// running or an aborted take. True when it wrote the file now.
        /// </summary>
        static bool FinishReleased()
        {
            if (s_active || s_aborted || s_seqWritten || s_dir == null || Pending() > 0) return false;
            try
            {
                File.WriteAllText(Path.Combine(s_dir, "seq.json"), SeqJson());
                s_seqWritten = true;
                return true;
            }
            catch (System.Exception e) { Debug.LogError("[StoreGifApi] seq.json for take " + s_name + " not written: " + e.Message); return false; }
        }

        /// <summary>A JSON string body: quotes, backslashes and control characters (Mono's "\r\nParameter name: x") escaped.</summary>
        static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", C));
                else sb.Append(c);
            }
            return sb.ToString();
        }

        static string SeqJson()
        {
            var sb = new StringBuilder(4096);
            sb.Append("{\"name\":\"").Append(JsonEscape(s_name)).Append("\",\"stepDt\":").Append(StepDt.ToString("R", C))
              .Append(",\"fps\":").Append((1f / (s_cadence[0].y * StepDt)).ToString("0.###", C))
              .Append(",\"stepFps\":").Append((1f / StepDt).ToString("0.###", C))
              .Append(",\"audioLog\":").Append(File.Exists(Path.Combine(s_dir, "audio.jsonl")) ? "\"audio.jsonl\"" : "null")
              .Append(",\"render\":[").Append(s_w.ToString(C)).Append(',').Append(s_h.ToString(C)).Append(']')
              .Append(",\"crop\":").Append(RectJson(s_crop)).Append(",\"cropAsked\":").Append(RectJson(s_cropAsked))
              .Append(",\"hud\":").Append(s_hud ? "true" : "false");
            if (s_hud)
                sb.Append(",\"ui\":[").Append(s_uiW.ToString(C)).Append(',').Append(s_uiH.ToString(C)).Append("],\"uiCrop\":").Append(RectJson(s_uiCrop));
            sb.Append(",\"png\":\"f%04d.png\"").Append(s_hud ? ",\"uiPng\":\"f%04d_ui.png\"" : "")
              .Append(",\"frames\":").Append(s_frames.ToString(C)).Append(",\"steps\":").Append(s_steps.ToString(C))
              .Append(",\"dtMean\":").Append((s_steps > 0 ? s_dtSum / s_steps : 0).ToString("0.0000", C))
              .Append(",\"udtMean\":").Append((s_steps > 0 ? s_udtSum / s_steps : 0).ToString("0.0000", C))
              .Append(",\"stepMsMean\":").Append((s_steps > 0 ? s_stepMs / s_steps : 0).ToString("0.0", C))
              .Append(",\"captureMsMean\":").Append((s_frames > 0 ? s_captureMs / s_frames : 0).ToString("0.0", C))
              .Append(",\"cadence\":[");
            for (int i = 0; i < s_cadence.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"fromFrame\":").Append(s_cadence[i].x.ToString(C)).Append(",\"every\":").Append(s_cadence[i].y.ToString(C)).Append('}');
            }
            sb.Append("],\"frameSteps\":[");
            for (int i = 0; i < s_frameSteps.Count; i++) { if (i > 0) sb.Append(','); sb.Append(s_frameSteps[i].ToString(C)); }
            sb.Append("],\"errors\":[");
            for (int i = 0; i < s_errors.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(JsonEscape(s_errors[i])).Append('"');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// A running take: drops the queue and the targets without writing seq.json, and puts the pointer, audio and HUD
        /// back. A take already released by SeqEnd (PNGs were still writing) is not lost: this waits for its writers (up to
        /// 3 s) and writes its seq.json, or leaves that to the next SeqEnd. Also run by SeqBegin for a running take, on
        /// leaving Play mode and before a script reload.
        /// </summary>
        public static void SeqAbort()
        {
            s_queue.Clear();
            s_cur = null;
            if (s_active)
            {
                s_aborted = true;
                s_seqWritten = true;
                Release();
            }
            else if (!s_seqWritten && !s_aborted && s_dir != null && WaitPending(3000) == 0) FinishReleased();
        }

        static void Release()
        {
            s_active = false;
            if (s_fpsSet) { Time.fixedDeltaTime = s_prevFixedDt; s_fpsSet = false; }
            if (s_audioLog || s_audioOut != null) EndAudioLog(!s_aborted);
            var t = Tools;
            if (t != null) { t.ScriptedPointer = false; t.ScriptedDown = false; t.ScriptedRight = false; }
            EditorUtility.audioMasterMute = s_prevMute;
            if (s_hud && !s_prevMarketing) StoreShotApi.MarketingHud(false);
            StoreShotApi.FreeCaptureTarget(s_hdr);
            StoreShotApi.FreeCaptureTarget(s_ldr);
            StoreShotApi.FreeCaptureTarget(s_ui);
            DestroyTex(s_rgb);
            DestroyTex(s_rgba);
            s_hdr = s_ldr = s_ui = null; s_rgb = s_rgba = null;
        }

        static void DestroyTex(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        // ------------------------------------------------------------------ audio log (Docs/STORE_CLIPS.md 5)
        // The Editor cannot record the game's audio here (the unfocused Editor's dsp clock stands still), so an audioLog
        // take writes what the mix is doing instead: audio.jsonl, one line per Step with ToolAudio's state, the playing
        // sources and the AudioTap events of that Step; audio_clips.json maps every clip to its asset or a dumped WAV.
        // The take runs the dsp-timed audio (recorded machines, crumb rattle) on a virtual clock that advances with the
        // Steps: dspBase + t, dspBase 1000 s ahead of the real clock, so nothing scheduled on it can sound.
        static bool s_audioLog;
        static StreamWriter s_audioOut;
        static double s_clockBase, s_clock;
        static System.Func<double> s_clockFn;
        static int s_tapEvents, s_mixerGroups;
        static readonly List<AudioTapEvent> s_tapPending = new List<AudioTapEvent>();
        static readonly Dictionary<AudioClip, string> s_clipKey = new Dictionary<AudioClip, string>();
        static readonly Dictionary<string, AudioClip> s_clipByKey = new Dictionary<string, AudioClip>();
        static readonly HashSet<string> s_stateClips = new HashSet<string>();
        static readonly Dictionary<AudioSource, AudioLowPassFilter> s_lowpass = new Dictionary<AudioSource, AudioLowPassFilter>();
        static readonly StringBuilder s_line = new StringBuilder(8192);
        static readonly Regex s_clipField = new Regex("\"(?:motorClip|contactClip|clip)\":\"([^\"]*)\"", RegexOptions.CultureInvariant);

        static string D(double v) { return double.IsNaN(v) || double.IsInfinity(v) ? "null" : v.ToString("0.######", C); }

        /// <summary>Opens audio.jsonl, installs the virtual clock and the tap, writes the header, lists what already plays.</summary>
        static void BeginAudioLog()
        {
            s_tapPending.Clear(); s_clipKey.Clear(); s_clipByKey.Clear(); s_stateClips.Clear(); s_lowpass.Clear();
            s_tapEvents = 0; s_mixerGroups = 0;
            s_audioOut = new StreamWriter(Path.Combine(s_dir, "audio.jsonl"), false, new UTF8Encoding(false)) { NewLine = "\n" };
            s_clockBase = AudioSettings.dspTime + 1000.0;
            s_clock = s_clockBase;
            s_clockFn = () => s_clock;
            AudioTap.ClockOverride = s_clockFn;    // bumps AudioTap.ClockEpoch: crumb captures stamped before it are dropped
            // what staging scheduled on the real (frozen) clock would jump 1000 s forward: recorded machines and rattle
            // voices start the take silent and at rest, before the tap listens (so no "playing" blip at t = 0)
            var ta = Object.FindAnyObjectByType<ToolAudio>();
            if (ta != null) ta.ResetScheduling();
            AudioTap.ClearSources();
            AudioTap.Played -= OnTap;
            AudioTap.Played += OnTap;
            s_audioLog = true;
            AudioTap.Active = true;

            // every source of the scene, the DontSave ones included (ToolAudio, RecordedMachine and UiAudio make all theirs
            // DontSave, and FindObjectsByType skips those); assets (prefab components) left out
            var all = new List<AudioSource>();
            foreach (var s in Resources.FindObjectsOfTypeAll<AudioSource>())
                if (s != null && !EditorUtility.IsPersistent(s)) all.Add(s);
            foreach (var s in all) if (s.outputAudioMixerGroup != null) s_mixerGroups++;
            AudioSettings.GetDSPBufferSize(out int bufLen, out int _);
            s_audioOut.WriteLine("{\"type\":\"header\",\"take\":\"" + JsonEscape(s_name) + "\",\"stepDt\":" + StepDt.ToString("R", C)
                + ",\"fps\":" + (1.0 / StepDt).ToString("0.###", C) + ",\"sampleRate\":" + AudioSettings.outputSampleRate.ToString(C)
                + ",\"dspBuffer\":" + bufLen.ToString(C) + ",\"speakerMode\":\"" + AudioSettings.speakerMode + "\""
                + ",\"clock\":\"virtual\",\"dspBase\":" + s_clockBase.ToString("0.######", C)
                + ",\"startStep\":" + s_steps.ToString(C)
                + ",\"listenerVolume\":" + D(AudioListener.volume) + ",\"listenerPaused\":" + (AudioListener.pause ? "true" : "false")
                + ",\"mixerGroups\":" + s_mixerGroups.ToString(C) + "}");
            // a loop already running when the take starts gets a "playing" event on the first step line
            foreach (var s in all)
            {
                if (s == null) continue;
                AudioTap.Register(s);
                if (s.isPlaying && s.clip != null) AudioTap.Emit("pre/" + s.name, "playing", s, s.clip);
            }
        }

        static void OnTap(AudioTapEvent e)
        {
            if (!s_audioLog) return;
            ClipKey(e.clip);           // keeps the reference: the clip is dumped at the end even if nothing plays it then
            s_tapPending.Add(e);
            s_tapEvents++;
        }

        /// <summary>The clip's name in the log; a second, different clip with the same name becomes "name#2".</summary>
        static string ClipKey(AudioClip c)
        {
            if (ReferenceEquals(c, null)) return "";
            if (s_clipKey.TryGetValue(c, out string k)) return k;
            k = string.IsNullOrEmpty(c.name) ? "clip" : c.name;
            if (s_clipByKey.ContainsKey(k))
            {
                int n = 2;
                while (s_clipByKey.ContainsKey(k + "#" + n.ToString(C))) n++;
                k = k + "#" + n.ToString(C);
            }
            s_clipKey[c] = k;
            s_clipByKey[k] = c;
            return k;
        }

        /// <summary>
        /// One audio.jsonl step line, right after Step s: {"type":"step","s","t","state":AudioJson,"src":[playing
        /// sources],"ev":[events since the previous line]}. Events from a Call item land on the next step line.
        /// </summary>
        static void AudioLogStep()
        {
            if (s_audioOut == null) return;
            string state;
            try { state = CarpetDebugApi.AudioJson(); }
            catch (System.Exception e) { state = null; if (s_errors.Count < 50) s_errors.Add("audio state at step " + s_steps.ToString(C) + ": " + e.Message); }
            bool obj = !string.IsNullOrEmpty(state) && state[0] == '{';
            if (obj) foreach (Match m in s_clipField.Matches(state)) if (m.Groups[1].Value.Length > 0) s_stateClips.Add(m.Groups[1].Value);
            var sb = s_line;
            sb.Length = 0;
            sb.Append("{\"type\":\"step\",\"s\":").Append(s_steps.ToString(C)).Append(",\"t\":").Append(D(s_steps * (double)StepDt))
              .Append(",\"state\":").Append(obj ? state : "null");
            AppendSources(sb);
            AppendEvents(sb);
            sb.Append('}');
            s_audioOut.WriteLine(sb.ToString());
        }

        /// <summary>Every source the tap knows that plays now: id, clip, level, pitch, low-pass cutoff (when it has one), loop.</summary>
        static void AppendSources(StringBuilder sb)
        {
            sb.Append(",\"src\":[");
            bool first = true;
            var list = AudioTap.Sources;
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if (s == null || !s.isPlaying) continue;
                if (!s_lowpass.TryGetValue(s, out var lp)) { lp = s.GetComponent<AudioLowPassFilter>(); s_lowpass[s] = lp; }
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"id\":").Append(AudioTap.IdOf(s).ToString(C)).Append(",\"clip\":\"").Append(JsonEscape(ClipKey(s.clip)))
                  .Append("\",\"vol\":").Append(D(s.volume)).Append(",\"pitch\":").Append(D(s.pitch));
                if (lp != null && lp.enabled) sb.Append(",\"cut\":").Append(D(lp.cutoffFrequency));
                sb.Append(",\"loop\":").Append(s.loop ? "true" : "false").Append('}');
            }
            sb.Append(']');
        }

        /// <summary>The pending AudioTap events; "at" = take seconds the sound starts (or stops) = its call's clock - dspBase + delay.</summary>
        static void AppendEvents(StringBuilder sb)
        {
            sb.Append(",\"ev\":[");
            for (int i = 0; i < s_tapPending.Count; i++)
            {
                var e = s_tapPending[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"src\":\"").Append(JsonEscape(e.src)).Append("\",\"kind\":\"").Append(JsonEscape(e.kind))
                  .Append("\",\"clip\":\"").Append(JsonEscape(ClipKey(e.clip)))
                  .Append("\",\"vol\":").Append(D(e.volume)).Append(",\"scale\":").Append(D(e.scale))
                  .Append(",\"pitch\":").Append(D(e.pitch)).Append(",\"delay\":").Append(D(e.delay))
                  .Append(",\"at\":").Append(D(e.clock - s_clockBase + e.delay)).Append(",\"start\":").Append(D(e.start))
                  .Append(",\"pan\":").Append(D(e.pan)).Append(",\"loop\":").Append(e.loop ? "true" : "false")
                  .Append(",\"id\":").Append(e.sourceId.ToString(C)).Append('}');
            }
            sb.Append(']');
            s_tapPending.Clear();
        }

        /// <summary>
        /// Closes the log (Release): unhooks the tap and the virtual clock, writes the "end" line (events after the last
        /// Step, the id -> source name map), then audio_clips.json unless the take was aborted, and frees ToolAudio's
        /// voices scheduled on the virtual clock.
        /// </summary>
        static void EndAudioLog(bool writeClips)
        {
            s_audioLog = false;
            AudioTap.Active = false;
            AudioTap.Played -= OnTap;
            if (AudioTap.ClockOverride == s_clockFn) AudioTap.ClockOverride = null;
            try
            {
                if (s_audioOut != null)
                {
                    var sb = s_line;
                    sb.Length = 0;
                    sb.Append("{\"type\":\"end\",\"s\":").Append(s_steps.ToString(C)).Append(",\"t\":").Append(D(s_steps * (double)StepDt))
                      .Append(",\"aborted\":").Append(writeClips ? "false" : "true").Append(",\"events\":").Append(s_tapEvents.ToString(C));
                    AppendEvents(sb);
                    sb.Append(",\"sources\":{");
                    var list = AudioTap.Sources;
                    int groups = 0;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var s = list[i];
                        if (i > 0) sb.Append(',');
                        sb.Append('"').Append(AudioTap.IdOf(s).ToString(C)).Append("\":\"").Append(s != null ? JsonEscape(s.name) : "(destroyed)").Append('"');
                        if (s != null && s.outputAudioMixerGroup != null) groups++;
                    }
                    sb.Append("},\"mixerGroups\":").Append(groups.ToString(C)).Append('}');
                    s_audioOut.WriteLine(sb.ToString());
                    s_audioOut.Dispose();
                }
            }
            catch (System.Exception e) { s_errors.Add("audio.jsonl: " + e.Message); }
            s_audioOut = null;
            if (writeClips)
            {
                try { WriteAudioClips(); }
                catch (System.Exception e) { s_errors.Add("audio_clips.json: " + e.Message); }
            }
            var ta = Object.FindAnyObjectByType<ToolAudio>();
            if (ta != null) ta.ResetScheduling();
            AudioTap.ClearSources();
            s_tapPending.Clear();
            s_lowpass.Clear();
        }

        /// <summary>
        /// audio_clips.json: every clip key seen in the events, the playing sources and the state's clip fields ->
        /// {"asset":"Assets/..."} for an imported clip, {"wav":"_clips/&lt;key&gt;.wav"} for one made at runtime
        /// (ProceduralSfx / UiSfxSynth placeholders: their PCM dumped as 16-bit WAV), plus channels, frequency, samples.
        /// A state name no reference was seen for is looked up among the loaded clips (an imported one preferred).
        /// </summary>
        static void WriteAudioClips()
        {
            var missing = new List<string>();
            foreach (var n in s_stateClips) if (!s_clipByKey.ContainsKey(n)) missing.Add(n);
            if (missing.Count > 0)
            {
                foreach (var c in Resources.FindObjectsOfTypeAll<AudioClip>())
                {
                    if (c == null || !missing.Contains(c.name)) continue;
                    if (s_clipByKey.TryGetValue(c.name, out var have))
                    {
                        // an imported clip beats a runtime one of the same name
                        if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(have)) || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(c))) continue;
                        s_clipKey.Remove(have);
                        s_clipByKey.Remove(c.name);
                    }
                    s_clipKey[c] = c.name;
                    s_clipByKey[c.name] = c;
                }
            }
            string wavDir = Path.Combine(s_dir, "_clips");
            var sb = new StringBuilder(4096);
            sb.Append('{');
            bool first = true;
            foreach (var kv in s_clipByKey)
            {
                var clip = kv.Value;
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(JsonEscape(kv.Key)).Append("\":{");
                if (clip == null) { sb.Append("\"error\":\"destroyed\"}"); continue; }
                sb.Append("\"channels\":").Append(clip.channels.ToString(C)).Append(",\"frequency\":").Append(clip.frequency.ToString(C))
                  .Append(",\"samples\":").Append(clip.samples.ToString(C)).Append(",\"length\":").Append(D(clip.length))
                  .Append(",\"loadType\":\"").Append(clip.loadType).Append('"');
                string asset = AssetDatabase.GetAssetPath(clip);
                if (!string.IsNullOrEmpty(asset)) sb.Append(",\"asset\":\"").Append(JsonEscape(asset)).Append('"');
                else
                {
                    string file = SafeFileName(kv.Key) + ".wav";
                    string err = DumpWav(clip, Path.Combine(wavDir, file));
                    if (err == null) sb.Append(",\"wav\":\"_clips/").Append(JsonEscape(file)).Append('"');
                    else sb.Append(",\"error\":\"").Append(JsonEscape(err)).Append('"');
                }
                sb.Append('}');
            }
            foreach (var n in missing)
            {
                if (s_clipByKey.ContainsKey(n)) continue;
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(JsonEscape(n)).Append("\":{\"error\":\"not loaded\"}");
            }
            sb.Append('}');
            File.WriteAllText(Path.Combine(s_dir, "audio_clips.json"), sb.ToString());
        }

        static string SafeFileName(string key)
        {
            var sb = new StringBuilder(key.Length);
            var bad = Path.GetInvalidFileNameChars();
            foreach (char c in key) sb.Append(c == '#' || System.Array.IndexOf(bad, c) >= 0 ? '_' : c);
            return sb.Length > 0 ? sb.ToString() : "clip";
        }

        /// <summary>A runtime clip's PCM as a 16-bit WAV (its own channels and rate). Null when written, else why not.</summary>
        static string DumpWav(AudioClip clip, string path)
        {
            if (clip.samples <= 0 || clip.channels <= 0) return "empty clip";
            var data = new float[clip.samples * clip.channels];
            bool ok;
            try { ok = clip.GetData(data, 0); }
            catch (System.Exception e) { return "GetData: " + e.Message; }
            if (!ok) return "GetData failed (" + clip.loadType + ")";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var w = new BinaryWriter(fs))
            {
                int bytes = data.Length * 2;
                w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes); w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)clip.channels);
                w.Write(clip.frequency); w.Write(clip.frequency * clip.channels * 2); w.Write((short)(clip.channels * 2)); w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                for (int i = 0; i < data.Length; i++)
                    w.Write((short)Mathf.RoundToInt(Mathf.Clamp(data[i], -1f, 1f) * 32767f));
            }
            return null;
        }

        // ------------------------------------------------------------------ helpers for staging
        /// <summary>
        /// Where carpet point (u, v) on the pile top lands in a w x h render of the game camera (top-left origin, pixels),
        /// to place a crop. Pass the render size the take will use (SeqBegin's w, h); v = 0 is the near edge.
        /// </summary>
        public static string UvPixelJson(float u, float v, int w, int h)
        {
            if (!UvPixel(u, v, w, h, out Vector2 px, out bool front)) return "{\"error\":\"scene not ready\"}";
            return "{\"x\":" + px.x.ToString("0.#", C) + ",\"y\":" + px.y.ToString("0.#", C) + ",\"w\":" + w.ToString(C) + ",\"h\":" + h.ToString(C)
                 + ",\"inFront\":" + (front ? "true" : "false") + "}";
        }

        /// <summary>
        /// A cw x ch crop of a w x h render centred on carpet point (u, v) and shifted inside the frame; the numbers to pass
        /// to SeqBegin. {"cx","cy","cw","ch"}.
        /// </summary>
        public static string CropAround(float u, float v, int cw, int ch, int w, int h)
        {
            if (!UvPixel(u, v, w, h, out Vector2 px, out _)) return "{\"error\":\"scene not ready\"}";
            cw = Mathf.Clamp(cw, 2, w); ch = Mathf.Clamp(ch, 2, h);
            int cx = Mathf.Clamp(Mathf.RoundToInt(px.x - cw * 0.5f), 0, w - cw);
            int cy = Mathf.Clamp(Mathf.RoundToInt(px.y - ch * 0.5f), 0, h - ch);
            return "{\"cx\":" + cx.ToString(C) + ",\"cy\":" + cy.ToString(C) + ",\"cw\":" + cw.ToString(C) + ",\"ch\":" + ch.ToString(C) + "}";
        }

        static bool UvPixel(float u, float v, int w, int h, out Vector2 px, out bool inFront)
        {
            px = Vector2.zero; inFront = false;
            var cam = Camera.main; var t = Tools;
            if (cam == null || t == null || w <= 0 || h <= 0) return false;
            Transform root = t.carpetCollider != null ? t.carpetCollider.transform : t.transform;
            var size = CarpetSimulation.CarpetSizeM;
            Vector3 world = root.TransformPoint(new Vector3((u - 0.5f) * size.x, 0.02f, (v - 0.5f) * size.y));
            Vector3 vp = cam.WorldToViewportPoint(world);
            px = new Vector2(vp.x * w, (1f - vp.y) * h);
            inFront = vp.z > 0f;
            return true;
        }

        /// <summary>
        /// A shop click on a POWER (stat 0) / AREA (stat 1) row, through ShopView.OnBuy, so the pip pop, the sparkles and the
        /// wallet roll play as for a real click (EconomyDebugApi.Buy skips them). Buys for real: snapshot the save first.
        /// </summary>
        public static string ShopBuy(int tool, int stat)
        {
            var hud = EconomyDebugApi.Hud;
            if (hud == null) return "{\"error\":\"no EconomyHud\"}";
            const BindingFlags bf = BindingFlags.NonPublic | BindingFlags.Instance;
            var fShop = typeof(EconomyHud).GetField("_shop", bf);
            object shop = fShop != null ? fShop.GetValue(hud) : null;
            if (shop == null) return "{\"error\":\"shop not built\"}";
            var rows = shop.GetType().GetField("_rows", bf)?.GetValue(shop) as System.Collections.IList;
            var onBuy = shop.GetType().GetMethod("OnBuy", bf);
            if (rows == null || onBuy == null) return "{\"error\":\"ShopView internals changed\"}";
            foreach (var r in rows)
            {
                var rt = r.GetType();
                var fTool = rt.GetField("tool"); var fStat = rt.GetField("stat");
                if (fTool == null || fStat == null) return "{\"error\":\"ShopView row internals changed\"}";
                int rTool = (int)(ToolId)fTool.GetValue(r);
                int rStat = (int)(UpgradeStat)fStat.GetValue(r);
                if (rTool != tool || rStat != stat) continue;
                onBuy.Invoke(shop, new[] { r });
                return EconomyDebugApi.ShopJson();
            }
            return "{\"error\":\"no such row\"}";
        }

        /// <summary>
        /// Cost probe before a take (Play mode): `steps` real Steps (ms each, dt, unscaled dt), then one capture at w x h
        /// split into render + blit, ReadPixels, raw copy, EncodeToPNG and EncodeArrayToPNG (+ the HUD panel capture when
        /// hud). Writes probe_sync.png (EncodeToPNG) and probe_array.png (EncodeArrayToPNG) to clips/_probe and decodes both:
        /// "arrayPng" is "upright", "flipped" (then SetFlip(true)) or "differs". fps &gt; 0 = the Steps run at 1/fps s
        /// (Time.fixedDeltaTime, put back before returning), as a SeqBegin(fps: n) take would.
        /// </summary>
        public static string Probe(int steps = 10, int w = 1280, int h = 720, bool hud = false, int uiW = 2560, int uiH = 1440, int fps = 0)
        {
            var cam = Camera.main;
            if (cam == null) return "{\"error\":\"no main camera\"}";
            if (!EditorApplication.isPlaying) return "{\"error\":\"not in Play mode\"}";
            if (s_active) return "{\"error\":\"a take is running\"}";
            if (fps < 0 || fps > 240) return "{\"error\":\"fps must be 0 (keep the step) or 1..240\"}";
            EditorApplication.isPaused = true;
            float prevFixed = Time.fixedDeltaTime;
            if (fps > 0) Time.fixedDeltaTime = 1f / fps;
            float probeStep = Time.fixedDeltaTime;
            var sw = Stopwatch.StartNew();
            double stepMin = 1e9, stepMax = 0, dt = 0, udt = 0;
            try
            {
                for (int i = 0; i < steps; i++)
                {
                    var s = Stopwatch.StartNew();
                    EditorApplication.Step();
                    double ms = s.Elapsed.TotalMilliseconds;
                    stepMin = System.Math.Min(stepMin, ms); stepMax = System.Math.Max(stepMax, ms);
                    dt += Time.deltaTime; udt += Time.unscaledDeltaTime;
                }
            }
            finally { if (fps > 0) Time.fixedDeltaTime = prevFixed; }
            double stepMean = steps > 0 ? sw.Elapsed.TotalMilliseconds / steps : 0;
            string dir = Path.Combine(ClipDir, "_probe");
            Directory.CreateDirectory(dir);
            var hdr = StoreShotApi.NewCaptureTarget(w, h, true);
            var ldr = StoreShotApi.NewCaptureTarget(w, h, false);
            RenderTexture ui = hud ? StoreShotApi.NewCaptureTarget(uiW, uiH, false) : null;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
            var prevActive = RenderTexture.active;
            double tRender, tRead, tRaw, tPng, tArr, tUi = -1;
            string orient;
            try
            {
                var s = Stopwatch.StartNew();
                StoreShotApi.RenderHdr(cam, hdr, ldr);
                tRender = s.Elapsed.TotalMilliseconds; s.Restart();
                RenderTexture.active = ldr;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                tRead = s.Elapsed.TotalMilliseconds; s.Restart();
                byte[] raw = tex.GetRawTextureData<byte>().ToArray();
                tRaw = s.Elapsed.TotalMilliseconds; s.Restart();
                byte[] pngSync = tex.EncodeToPNG();
                tPng = s.Elapsed.TotalMilliseconds; s.Restart();
                byte[] pngArr = ImageConversion.EncodeArrayToPNG(raw, GraphicsFormat.R8G8B8_SRGB, (uint)w, (uint)h);
                tArr = s.Elapsed.TotalMilliseconds;
                File.WriteAllBytes(Path.Combine(dir, "probe_sync.png"), pngSync);
                File.WriteAllBytes(Path.Combine(dir, "probe_array.png"), pngArr);
                orient = CompareOrientation(pngSync, pngArr, h);
                if (ui != null)
                {
                    var root = HudRoot();
                    s.Restart();
                    RenderTexture.active = ui;
                    GL.Clear(true, true, Color.clear);
                    bool ok = root != null && VisualElementCaptureExtensions.TryCaptureIntoRenderTexture(root, ui);
                    tUi = ok ? s.Elapsed.TotalMilliseconds : -2;
                }
            }
            finally
            {
                RenderTexture.active = prevActive;
                StoreShotApi.FreeCaptureTarget(hdr); StoreShotApi.FreeCaptureTarget(ldr); StoreShotApi.FreeCaptureTarget(ui);
                DestroyTex(tex);
            }
            return "{\"steps\":" + steps.ToString(C) + ",\"stepDt\":" + probeStep.ToString("R", C) + ",\"stepMs\":{\"mean\":" + stepMean.ToString("0.0", C) + ",\"min\":" + stepMin.ToString("0.0", C)
                 + ",\"max\":" + stepMax.ToString("0.0", C) + "},\"dtMean\":" + (steps > 0 ? dt / steps : 0).ToString("0.0000", C)
                 + ",\"udtMean\":" + (steps > 0 ? udt / steps : 0).ToString("0.0000", C)
                 + ",\"capture\":{\"w\":" + w.ToString(C) + ",\"h\":" + h.ToString(C) + ",\"renderMs\":" + tRender.ToString("0.0", C)
                 + ",\"readMs\":" + tRead.ToString("0.0", C) + ",\"rawMs\":" + tRaw.ToString("0.0", C) + ",\"encodeToPngMs\":" + tPng.ToString("0.0", C)
                 + ",\"encodeArrayMs\":" + tArr.ToString("0.0", C) + ",\"hudMs\":" + tUi.ToString("0.0", C) + "},\"arrayPng\":\"" + orient
                 + "\",\"flip\":" + (s_flip ? "true" : "false") + "}";
        }

        /// <summary>Decodes both probe PNGs: "upright" when equal, "flipped" when one is the other upside down.</summary>
        static string CompareOrientation(byte[] pngA, byte[] pngB, int h)
        {
            var a = new Texture2D(2, 2, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
            var b = new Texture2D(2, 2, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                if (!a.LoadImage(pngA) || !b.LoadImage(pngB)) return "undecodable";
                byte[] ra = a.GetRawTextureData<byte>().ToArray(), rb = b.GetRawTextureData<byte>().ToArray();
                if (ra.Length != rb.Length || a.height != h || ra.Length % h != 0) return "differs";
                int stride = ra.Length / h;   // LoadImage picks the decoded format (RGB24 or RGBA32)
                if (System.Linq.Enumerable.SequenceEqual(ra, rb)) return "upright";
                FlipRows(rb, stride, h);
                return System.Linq.Enumerable.SequenceEqual(ra, rb) ? "flipped" : "differs";
            }
            finally { DestroyTex(a); DestroyTex(b); }
        }
    }
}
