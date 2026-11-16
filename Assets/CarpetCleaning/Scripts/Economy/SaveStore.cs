using System;
using System.IO;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// Everything the economy keeps between launches (Docs/ECONOMY.md 3). JsonUtility, so public fields only.
    /// The whole file is synced between machines by Steam Auto-Cloud (pattern carpet_save.json, Docs/STEAM.md 11), so
    /// what the achievements keep here (Docs/ACHIEVEMENTS.md) travels with the money.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int money;
        public long lifetimeEarned;
        public int carpetsDelivered;
        public int perfectClean;
        public int perfectDry;
        public int combos;
        public float bestSeconds;          // fastest hand-over of a carpet worth V >= 0.5, 0 = none yet
        public int[] power = new int[0];   // upgrade level per ToolId
        public int[] area = new int[0];
        public int[] items = new int[0];   // one-time purchases per ShopItem: 1 = owned (a file from before they existed reads 0)
        public int[] achieved = new int[0];   // 1 = earned locally, index = AchievementId (SteamAchievements; Steam gets it when it is up)
        public int toolsUsed;              // bit per ToolId ever used (Every Tool in the Van)

        /// <summary>Pads / trims the level arrays to the tool count and clamps every level, so an old or edited file is always safe to read.</summary>
        public void Normalize(int tools, int maxLevel)
        {
            power = Fit(power, tools, maxLevel);
            area = Fit(area, tools, maxLevel);
            items = Fit(items, ShopItems.Count, 1);
            achieved = Fit(achieved, Achievements.Count, 1);
            toolsUsed &= tools >= 31 ? int.MaxValue : (1 << Mathf.Max(tools, 0)) - 1;
            if (money < 0) money = 0;
            if (lifetimeEarned < 0) lifetimeEarned = 0;
            version = CurrentVersion;
        }

        static int[] Fit(int[] src, int n, int maxLevel)
        {
            var dst = new int[n];
            for (int i = 0; i < n; i++) dst[i] = src != null && i < src.Length ? Mathf.Clamp(src[i], 0, maxLevel) : 0;
            return dst;
        }
    }

    /// <summary>
    /// JSON save in Application.persistentDataPath: carpet_save.json in a player, carpet_save_editor.json in the Editor so
    /// testing never touches the Steam build's progress. Writes are atomic (temp file, then replace); a file that does not
    /// parse is moved aside as .bak and the game starts fresh, with a warning.
    /// </summary>
    public static class SaveStore
    {
        public static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, Application.isEditor ? "carpet_save_editor.json" : "carpet_save.json"); }
        }

        /// <summary>Never returns null. `note` says what happened (for the log and EconomyDebugApi).</summary>
        public static SaveData Load(out string note)
        {
            string path = FilePath;
            try
            {
                if (!File.Exists(path)) { note = "no save, fresh start"; return new SaveData(); }
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) throw new FormatException("empty document");
                note = "loaded";
                return data;
            }
            catch (Exception e)
            {
                string bak = path + ".bak";
                try
                {
                    if (File.Exists(bak)) File.Delete(bak);
                    if (File.Exists(path)) File.Move(path, bak);
                }
                catch (Exception moveError) { Debug.LogWarning("[SaveStore] could not move the broken save aside: " + moveError.Message); }
                note = "broken save moved to .bak (" + e.Message + "), fresh start";
                Debug.LogWarning("[SaveStore] " + note + ": " + path);
                return new SaveData();
            }
        }

        public static bool Write(SaveData data, out string error)
        {
            error = null;
            if (data == null) { error = "null data"; return false; }
            string path = FilePath;
            string tmp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
                // File.Replace needs an existing destination; the very first save is a plain move
                if (File.Exists(path)) File.Replace(tmp, path, null);
                else File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                Debug.LogWarning("[SaveStore] save failed: " + e.Message + " (" + path + ")");
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch (Exception) { }
                return false;
            }
        }

        public static void Delete()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch (Exception e) { Debug.LogWarning("[SaveStore] delete failed: " + e.Message); }
        }
    }
}
