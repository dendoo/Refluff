// Filming-only input recorder. Load with run_script, never copy into Assets.
// Records genuine ToolController input after Update. No synthetic input is sent while armed.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using CarpetCleaning;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScraperUpgradeFilm
{
    public static class HumanRecorder
    {
        [Serializable] public sealed class Sample
        {
            public double t;
            public float u, v;
            public bool present, down, right, onCarpet;
            public int frame;
        }

        [Serializable] public sealed class Take
        {
            public int version = 1;
            public bool fixture;
            public string kind, recordedUtc, endReason;
            public double duration;
            public float requestedMaxSeconds;
            public int tool;
            public List<Sample> samples = new List<Sample>();
        }

        [Serializable] public sealed class State
        {
            public string phase, kind, path, reason;
            public int samples;
            public double seconds;
            public bool complete, gameFocused, releaseSeen, serializationVerified;
        }

        static readonly string Work = Path.GetFullPath(Path.Combine(Application.dataPath,
            "../SteamStore/_work/tiktok_scraper/human"));
        static readonly FieldInfo HoverUv = typeof(ToolController).GetField("_hoverUv", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo HasPointer = typeof(ToolController).GetField("_hasPointer", BindingFlags.NonPublic | BindingFlags.Instance);
        static ToolController tools;
        static Take take;
        static string phase = "idle", reason = "", path = "";
        static bool releaseSeen, focusGate, previousInputEnabled;
        static int sampledFrame = -1;
        static double started, lastStatusWrite;
        static bool serializationVerified;
        static readonly Dictionary<string, Take> completedTakes = new Dictionary<string, Take>();
        static Label badge;

        public static string Main()
        {
            var domain = AppDomain.CurrentDomain;
            var previous = domain.GetData("scu.human.uninstall") as Func<string>;
            if (previous != null) previous();
            SelfTest();
            domain.SetData("scu.human.armweak", (Func<string>)(() => Arm("weak", 7f)));
            domain.SetData("scu.human.armstrong", (Func<string>)(() => Arm("strong", 8f)));
            domain.SetData("scu.human.status", (Func<string>)Status);
            domain.SetData("scu.human.stop", (Func<string>)(() => Stop("manual")));
            domain.SetData("scu.human.replay", (Func<string, string>)Replay);
            domain.SetData("scu.human.uninstall", (Func<string>)Uninstall);
            domain.SetData("scu.human.selftest", (Func<string>)SelfTest);
            domain.SetData("scu.human.activejson", (Func<string>)(() => take == null ? "null" : SerializeChecked(take)));
            RenderPipelineManager.beginContextRendering += AfterToolFrame;
            EditorApplication.update += WatchEditor;
            EditorApplication.playModeStateChanged += PlayState;
            Directory.CreateDirectory(Work);
            return Status();
        }

        static bool FocusedGameView()
        {
            if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) return false;
            var w = EditorWindow.focusedWindow;
            if (w == null) return false;
            string name = w.GetType().Name;
            return name == "GameView" || name == "PlayModeView";
        }

        static string Arm(string kind, float maxSeconds)
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            if (phase == "recording") Finish("rearmed");
            if (take != null && take.samples.Count > 0 && phase == "done") completedTakes[take.kind] = take;
            RestoreFocusGate();
            tools = CarpetDebugApi.Tools;
            if (tools == null) throw new InvalidOperationException("No ToolController.");
            if (!tools.InputEnabled) throw new InvalidOperationException("Enable ToolController before arming; handover must be idle.");
            if (HoverUv == null || HasPointer == null) throw new InvalidOperationException("ToolController pointer fields changed.");
            take = new Take { kind = kind, recordedUtc = DateTime.UtcNow.ToString("o"),
                requestedMaxSeconds = maxSeconds, tool = (int)tools.Current };
            path = Path.Combine(Work, kind + ".json");
            ArchiveExisting(path, kind);
            phase = "armed";
            reason = "";
            releaseSeen = false;
            previousInputEnabled = tools.InputEnabled;
            focusGate = true;
            tools.InputEnabled = false;
            sampledFrame = -1;
            started = 0;
            tools.ScriptedPointer = false;
            tools.ScriptedDown = false;
            tools.ScriptedRight = false;
            EditorApplication.isPaused = false;
            ShowBadge();
            WriteStatus();
            return Status();
        }

        static void AfterToolFrame(ScriptableRenderContext context, List<Camera> cameras)
        {
            if ((phase != "armed" && phase != "recording") || tools == null
                || !EditorApplication.isPlaying || EditorApplication.isPaused || tools.ScriptedPointer
                || cameras == null || !cameras.Contains(Camera.main) || sampledFrame == Time.frameCount) return;
            sampledFrame = Time.frameCount;
            bool focused = FocusedGameView();
            var mouse = Mouse.current;
            if (!focused || mouse == null) return;
            bool left = mouse.leftButton.isPressed, right = mouse.rightButton.isPressed;
            bool present = tools.InputEnabled && !tools.Suspended && !tools.UiModal && (bool)HasPointer.GetValue(tools);
            Vector2 uv = (Vector2)HoverUv.GetValue(tools);

            if (phase == "armed")
            {
                // First focus click is never accepted: a released frame in Game view must come first.
                if (!left && !right)
                {
                    releaseSeen = true;
                    // Suppress the initial click used to focus Game view, including its actual tool stamps.
                    if (focusGate) { tools.InputEnabled = previousInputEnabled; focusGate = false; }
                }
                if (!releaseSeen || !left || !present
                    || !tools.PointerOnCarpet || !tools.IsStroking) return;
                phase = "recording";
                started = Time.unscaledTimeAsDouble;
            }

            double elapsed = Math.Max(0, Time.unscaledTimeAsDouble - started);
            take.samples.Add(new Sample { t = elapsed, u = uv.x, v = uv.y, present = present,
                down = (left || right) && tools.IsStroking, right = !left && right,
                onCarpet = present && tools.PointerOnCarpet, frame = Time.frameCount });
            take.duration = elapsed;
            UpdateBadge();
            if (take.samples.Count == 1 || EditorApplication.timeSinceStartup - lastStatusWrite > .25)
            {
                Persist();
                WriteStatus();
            }
            if (elapsed >= take.requestedMaxSeconds) Finish("duration");
        }

        static void WatchEditor()
        {
            if (phase != "recording") return;
            // Editor updates keep running when an unfocused Game view stops rendering.
            if (!FocusedGameView() && take.samples.Count > 1) Finish("game_view_left");
        }

        static void PlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                if (phase == "recording") Finish("play_stopped");
                else if (phase == "armed") { RestoreFocusGate(); HideBadge(); phase = "cancelled"; reason = "play_stopped"; WriteStatus(); }
            }
        }

        static void Finish(string why)
        {
            if (phase != "recording") return;
            phase = "saving";
            reason = why;
            take.endReason = why;
            if (take.samples.Count > 0) take.duration = take.samples[take.samples.Count - 1].t;
            HideBadge();
            if (EditorApplication.isPlaying) EditorApplication.isPaused = true;
            try
            {
                Persist();
                var saved = DeserializeChecked(File.ReadAllText(path));
                if (saved.samples.Count != take.samples.Count) throw new IOException("Saved take sample count differs.");
                completedTakes[take.kind] = take;
                phase = "done";
                ShowBadge();
            }
            catch (Exception e)
            {
                phase = "error";
                reason = "save failed: " + e.Message;
                ShowBadge();
                WriteStatus();
                Debug.LogError("[HumanRecorder] " + reason);
                return;
            }
            WriteStatus();
            Debug.Log("[HumanRecorder] " + take.kind + " captured " + take.samples.Count + " samples, "
                + take.duration.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + " s; " + why);
        }

        static string Stop(string why)
        {
            if (phase == "recording") Finish(why);
            else if (phase == "armed") { RestoreFocusGate(); HideBadge(); phase = "cancelled"; reason = why; EditorApplication.isPaused = true; WriteStatus(); }
            return Status();
        }

        static State CurrentState()
        {
            return new State { phase = phase, kind = take == null ? "" : take.kind, path = path, reason = reason,
                samples = take == null ? 0 : take.samples.Count, seconds = take == null ? 0 : take.duration,
                complete = phase == "done", gameFocused = FocusedGameView(), releaseSeen = releaseSeen,
                serializationVerified = serializationVerified };
        }

        static string Status() { return JsonConvert.SerializeObject(CurrentState()); }
        static void WriteStatus()
        {
            lastStatusWrite = EditorApplication.timeSinceStartup;
            Directory.CreateDirectory(Work);
            File.WriteAllText(Path.Combine(Work, "status.json"), Status());
        }

        static string Replay(string kind)
        {
            if (kind != "weak" && kind != "strong") throw new ArgumentException("Expected weak or strong.");
            if (phase == "recording" || phase == "armed") throw new InvalidOperationException("Stop the live recorder first.");
            string source = Path.Combine(Work, kind + ".json");
            Take recorded;
            if (!completedTakes.TryGetValue(kind, out recorded))
            {
                var saved = AppDomain.CurrentDomain.GetData("scu.human.take." + kind) as string;
                recorded = DeserializeChecked(saved ?? File.ReadAllText(source));
            }
            if (recorded == null || recorded.samples == null || recorded.samples.Count < 2 || recorded.duration <= 0)
                throw new InvalidOperationException("The recorded path is empty or too short.");
            HideBadge();
            var target = CarpetDebugApi.Tools;
            int cursor = 0;
            StoreGifApi.Call(() => { target.ScriptedPointer = true; target.ScriptedDown = false;
                target.ScriptedRight = false; InitializeReplayHeading(target, kind); });
            StoreGifApi.Each((float)recorded.duration + 1f / 60f, t =>
            {
                while (cursor + 1 < recorded.samples.Count && recorded.samples[cursor + 1].t <= t) cursor++;
                var a = recorded.samples[cursor];
                var b = recorded.samples[Math.Min(cursor + 1, recorded.samples.Count - 1)];
                float f = b.t > a.t ? Mathf.Clamp01((float)((t - a.t) / (b.t - a.t))) : 0f;
                // Time interpolation only: retain every hand wobble, reversal, pause and button transition.
                Vector2 uv = new Vector2(a.u, a.v);
                if (a.present && b.present) uv = Vector2.LerpUnclamped(uv, new Vector2(b.u, b.v), f);
                target.ScriptedPointer = true;
                target.ScriptedUv = a.present ? uv : new Vector2(-5f, -5f);
                target.ScriptedDown = a.present && a.down;
                target.ScriptedRight = a.right;
            });
            StoreGifApi.Call(() => { target.ScriptedDown = false; target.ScriptedRight = false; });
            return JsonConvert.SerializeObject(new State { phase = "queued", kind = kind, path = source,
                samples = recorded.samples.Count, seconds = recorded.duration });
        }

        static string Uninstall()
        {
            if (phase == "recording") Finish("uninstalled");
            RestoreFocusGate();
            HideBadge();
            RenderPipelineManager.beginContextRendering -= AfterToolFrame;
            EditorApplication.update -= WatchEditor;
            EditorApplication.playModeStateChanged -= PlayState;
            phase = "uninstalled";
            return "human recorder removed";
        }

        static void InitializeReplayHeading(ToolController target, string kind)
        {
            // Optional filming correction for an already-recorded take lacking pre-press hover state.
            // It changes only the initial tool orientation, never the saved human pointer path.
            object value = AppDomain.CurrentDomain.GetData("scu.human.initialheading." + kind);
            if (value == null) { target.SnapHeadingOnNextMove(); return; }
            float angle = Convert.ToSingle(value, System.Globalization.CultureInfo.InvariantCulture);
            SetPrivate(target, "_headAngle", angle);
            SetPrivate(target, "_headTarget", angle);
            SetPrivate(target, "_headSeeded", true);
            SetPrivate(target, "_headAccum", Vector2.zero);
            SetPrivate(target, "_travelSign", 1f);
            SetPrivate(target, "_hasLast", false);
            SetPrivate(target, "_hasPointer", false);
            SetPrivate(target, "_carryMm", 0f);
            SetPrivate(target, "_plowHasPrev", false);
        }

        static void SetPrivate(ToolController target, string name, object value)
        {
            var field = typeof(ToolController).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new MissingFieldException(typeof(ToolController).FullName, name);
            field.SetValue(target, value);
        }

        static void RestoreFocusGate()
        {
            if (focusGate && tools != null) tools.InputEnabled = previousInputEnabled;
            focusGate = false;
        }

        static string SerializeChecked(Take value)
        {
            string json = JsonConvert.SerializeObject(value, Formatting.Indented);
            var parsed = JObject.Parse(json);
            var rows = parsed["samples"] as JArray;
            if (rows == null || rows.Count != value.samples.Count) throw new InvalidOperationException("Take serialization omitted samples.");
            var roundtrip = JsonConvert.DeserializeObject<Take>(json);
            if (roundtrip == null || roundtrip.samples == null || roundtrip.samples.Count != value.samples.Count)
                throw new InvalidOperationException("Take serialization roundtrip failed.");
            return json;
        }

        static Take DeserializeChecked(string json)
        {
            var rows = JObject.Parse(json)["samples"] as JArray;
            if (rows == null || rows.Count == 0) throw new InvalidOperationException("Saved take has no samples array.");
            var value = JsonConvert.DeserializeObject<Take>(json);
            if (value == null || value.samples == null || value.samples.Count != rows.Count)
                throw new InvalidOperationException("Saved take sample count did not roundtrip.");
            return value;
        }

        static string SelfTest()
        {
            var probe = new Take { kind = "selftest", fixture = true, duration = .5, requestedMaxSeconds = 1, tool = 0 };
            probe.samples.Add(new Sample { t = 0, u = .23f, v = .61f, present = true, down = true, onCarpet = true, frame = 14 });
            probe.samples.Add(new Sample { t = .5, u = .75f, v = .12f, present = false, down = false, right = true, frame = 27 });
            string json = SerializeChecked(probe);
            Directory.CreateDirectory(Work);
            string fixturePath = Path.Combine(Work, "_selftest.json");
            File.WriteAllText(fixturePath, json);
            var saved = DeserializeChecked(File.ReadAllText(fixturePath));
            if (!saved.fixture || saved.samples.Count != 2 || saved.samples[0].u != .23f || !saved.samples[0].down
                || saved.samples[1].frame != 27 || !saved.samples[1].right || saved.samples[1].present)
                throw new InvalidOperationException("Sample fields failed serialization selftest.");
            serializationVerified = true;
            return "{\"success\":true,\"samples\":2,\"serializer\":\"Newtonsoft.Json\"}";
        }

        static void ArchiveExisting(string source, string kind)
        {
            if (!File.Exists(source)) return;
            string archive = Path.Combine(Work, "archive");
            Directory.CreateDirectory(archive);
            File.Copy(source, Path.Combine(archive, kind + "_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".json"));
        }

        static void Persist()
        {
            string json = SerializeChecked(take);
            AppDomain.CurrentDomain.SetData("scu.human.take." + take.kind, json);
            Directory.CreateDirectory(Work);
            File.WriteAllText(path, json);
        }

        static void ShowBadge()
        {
            HideBadge();
            var doc = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
            if (doc == null || doc.rootVisualElement == null) return;
            badge = new Label();
            badge.pickingMode = PickingMode.Ignore;
            badge.style.position = Position.Absolute;
            badge.style.left = 22;
            badge.style.top = 22;
            badge.style.fontSize = 30;
            badge.style.color = Color.white;
            badge.style.backgroundColor = new Color(.12f, .12f, .12f, .92f);
            badge.style.paddingLeft = 15;
            badge.style.paddingRight = 15;
            badge.style.paddingTop = 8;
            badge.style.paddingBottom = 8;
            doc.rootVisualElement.Add(badge);
            UpdateBadge();
        }

        static void UpdateBadge()
        {
            if (badge == null) return;
            if (phase == "done") badge.text = "SAVED: " + take.samples.Count + " samples / "
                + take.duration.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s";
            else if (phase == "error") badge.text = "SAVE ERROR — return to chat";
            else if (phase == "recording") badge.text = "REC "
                + take.duration.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " / " + take.requestedMaxSeconds + " s";
            else badge.text = "ARMED — click Game, release, then scrape";
            badge.style.backgroundColor = (phase == "recording" || phase == "error")
                ? new Color(.75f, .04f, .04f, .94f) : new Color(.10f, .30f, .12f, .94f);
        }

        static void HideBadge()
        {
            if (badge != null) badge.RemoveFromHierarchy();
            badge = null;
        }
    }
}
