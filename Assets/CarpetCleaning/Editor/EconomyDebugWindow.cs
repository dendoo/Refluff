using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CarpetCleaning
{
    /// <summary>
    /// Test money for the economy (Docs/ECONOMY.md 7). Menu CarpetCleaning > Economy: quick grants and a small window with the
    /// balance, a custom amount and a wallet reset. Works in both modes:
    ///   - Play mode with an Economy in the scene: Economy.DebugGrant, so the HUD wallet rolls up, the shop refreshes and the
    ///     save is written by the game itself;
    ///   - otherwise: the Editor save file (SaveStore.FilePath, carpet_save_editor.json) is edited directly and the next Play
    ///     loads it. A save that does not parse is moved aside by SaveStore.Load exactly as the game would.
    /// Money is clamped to [0, int.MaxValue] like every other wallet change.
    /// </summary>
    public sealed class EconomyDebugWindow : EditorWindow
    {
        const string Menu = "CarpetCleaning/Economy/";

        [MenuItem(Menu + "Add $10,000 (test)", false, 1)]
        static void Add10K() { Report(Grant(10_000)); }

        [MenuItem(Menu + "Add $100,000 (test)", false, 2)]
        static void Add100K() { Report(Grant(100_000)); }

        [MenuItem(Menu + "Add $1,000,000 (test)", false, 3)]
        static void Add1M() { Report(Grant(1_000_000)); }

        [MenuItem(Menu + "Unlock highlights (test)", false, 10)]
        static void UnlockHighlights() { Report(SetHighlightsOwned(true)); }

        [MenuItem(Menu + "Remove highlights (test)", false, 11)]
        static void RemoveHighlights() { Report(SetHighlightsOwned(false)); }

        [MenuItem(Menu + "Test Money Window...", false, 20)]
        static void Open()
        {
            var w = GetWindow<EconomyDebugWindow>();
            w.titleContent = new GUIContent("Test Money");
            w.minSize = new Vector2(300f, 225f);
            w.Show();
        }

        static void Report(string message) { Debug.Log("[EconomyDebug] " + message); }

        [MenuItem("Tools/CarpetCleaning/Reset Progress...", false, 100)]
        [MenuItem(Menu + "Reset Progress...", false, 40)]
        static void ResetProgressMenu()
        {
            if (ConfirmResetProgress()) Report(ResetProgress());
        }

        static bool ConfirmResetProgress()
        {
            return EditorUtility.DisplayDialog("Reset Editor progress?",
                "Reset money, all tool upgrades, highlights, statistics and local achievements?\n\n" +
                "Only the Editor save is reset. Game/Steam progress, language and volume are kept.\n\n" + SaveStore.FilePath,
                "Reset progress", "Cancel");
        }

        // ------------------------------------------------------------------ the actual work (also used by the window)
        // Instance is a static: a script hot reload during Play clears it while the component lives on, so look it up then
        static Economy LiveEconomy
        {
            get
            {
                if (!EditorApplication.isPlaying) return null;
                return Economy.Instance != null ? Economy.Instance : UnityEngine.Object.FindAnyObjectByType<Economy>();
            }
        }

        /// <summary>Adds `amount` (may be negative) and says where it went. Public for CLI tests.</summary>
        public static string Grant(int amount)
        {
            var eco = LiveEconomy;
            if (eco != null && eco.Data != null)
            {
                int before = eco.Money;
                eco.DebugGrant(amount);
                return "Play mode: " + Money(before) + " -> " + Money(eco.Money) + " (saved to " + SaveStore.FilePath + ")";
            }
            var data = SaveStore.Load(out string note);
            data.Normalize(ToolTable.All.Length, MaxLevel());
            int was = data.money;
            data.money = Clamp((long)data.money + amount);
            if (!SaveStore.Write(data, out string err)) return "save NOT written: " + err;
            return "save file: " + Money(was) + " -> " + Money(data.money) + " (" + note + ", " + SaveStore.FilePath + ")";
        }

        /// <summary>Sets the wallet to exactly `amount`; everything else in the save is kept.</summary>
        public static string SetMoney(int amount)
        {
            var eco = LiveEconomy;
            if (eco != null && eco.Data != null) return Grant(Clamp(amount) - eco.Money);
            var data = SaveStore.Load(out string note);
            data.Normalize(ToolTable.All.Length, MaxLevel());
            int was = data.money;
            data.money = Clamp(amount);
            if (!SaveStore.Write(data, out string err)) return "save NOT written: " + err;
            return "save file: " + Money(was) + " -> " + Money(data.money) + " (" + note + ")";
        }

        /// <summary>Resets all saved Editor progress. Confirmation belongs to the menu/window, not CLI callers.</summary>
        public static string ResetProgress()
        {
            var eco = LiveEconomy;
            if (eco != null && eco.Data != null)
            {
                if (!eco.DebugResetSave()) return "progress NOT reset: could not write the save (see Console)";
                ToolTuning.SetNeutral(false);
                return "Play mode: progress reset (saved to " + SaveStore.FilePath + ")";
            }
            var data = new SaveData();
            data.Normalize(ToolTable.All.Length, MaxLevel());
            if (!SaveStore.Write(data, out string err)) return "progress NOT reset: " + err;
            return "save file: progress reset (" + SaveStore.FilePath + ")";
        }

        /// <summary>Owns / drops both one-time highlight items (Docs/HIGHLIGHTS.md) without touching the wallet.</summary>
        public static string SetHighlightsOwned(bool owned)
        {
            var eco = LiveEconomy;
            if (eco != null && eco.Data != null)
            {
                for (int i = 0; i < ShopItems.Count; i++) eco.DebugSetOwned((ShopItem)i, owned);
                return "Play mode: highlights " + (owned ? "unlocked" : "removed") + " (saved to " + SaveStore.FilePath + ")";
            }
            var data = SaveStore.Load(out string note);
            data.Normalize(ToolTable.All.Length, MaxLevel());
            for (int i = 0; i < data.items.Length; i++) data.items[i] = owned ? 1 : 0;
            if (!SaveStore.Write(data, out string err)) return "save NOT written: " + err;
            return "save file: highlights " + (owned ? "unlocked" : "removed") + " (" + note + ", " + SaveStore.FilePath + ")";
        }

        /// <summary>The balance without side effects: the live wallet in Play, else a read-only peek at the save file.</summary>
        public static string Balance()
        {
            var eco = LiveEconomy;
            if (eco != null && eco.Data != null) return Money(eco.Money) + "  (Play mode, live)";
            string path = SaveStore.FilePath;
            if (!File.Exists(path)) return Money(0) + "  (no save file yet)";
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                return data != null ? Money(data.money) + "  (save file)" : "unreadable save file";
            }
            catch (System.Exception e) { return "unreadable save file: " + e.Message; }
        }

        static int MaxLevel()
        {
            var s = AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/CarpetCleaning/Settings/EconomySettings.asset");
            return s != null ? Mathf.Max(s.maxLevel, 0) : 6;
        }

        static int Clamp(long v) { return (int)System.Math.Max(0L, System.Math.Min(int.MaxValue, v)); }
        static string Money(int v) { return PayoutResult.Money(v); }

        // ------------------------------------------------------------------ window (UI Toolkit, default editor look)
        Label _balance, _last;
        IntegerField _custom;

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10; root.style.paddingRight = 10; root.style.paddingTop = 8; root.style.paddingBottom = 8;

            _balance = new Label();
            _balance.style.unityFontStyleAndWeight = FontStyle.Bold;
            _balance.style.fontSize = 16;
            _balance.style.marginBottom = 8;
            root.Add(_balance);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            foreach (int amount in new[] { 1_000, 10_000, 100_000, 1_000_000 })
            {
                int a = amount;
                var b = new Button(() => Do(Grant(a))) { text = "+" + Money(a) };
                b.style.minWidth = 64;
                row.Add(b);
            }
            root.Add(row);

            var customRow = new VisualElement();
            customRow.style.flexDirection = FlexDirection.Row;
            customRow.style.marginTop = 6;
            _custom = new IntegerField("Amount") { value = 50_000 };
            _custom.style.flexGrow = 1;
            customRow.Add(_custom);
            customRow.Add(new Button(() => Do(Grant(_custom.value))) { text = "Add" });
            customRow.Add(new Button(() => Do(SetMoney(_custom.value))) { text = "Set" });
            root.Add(customRow);

            var reset = new Button(() => Do(SetMoney(0))) { text = "Wallet to $0 (keeps upgrades)" };
            reset.style.marginTop = 6;
            root.Add(reset);

            var resetProgress = new Button(() =>
            {
                if (ConfirmResetProgress()) Do(ResetProgress());
            }) { text = "Reset all progress..." };
            resetProgress.style.marginTop = 6;
            root.Add(resetProgress);

            _last = new Label("In Play the HUD wallet rolls up at once; otherwise the save file is edited and the next Play loads it.");
            _last.style.whiteSpace = WhiteSpace.Normal;
            _last.style.marginTop = 8;
            _last.style.opacity = 0.7f;
            root.Add(_last);

            RefreshBalance();
            root.schedule.Execute(RefreshBalance).Every(300);
        }

        void Do(string result)
        {
            Report(result);
            if (_last != null) _last.text = result;
            RefreshBalance();
        }

        void RefreshBalance()
        {
            if (_balance == null) return;
            string s = "Wallet: " + Balance();
            if (_balance.text != s) _balance.text = s;
        }
    }
}
