using System.Collections.Generic;

namespace CarpetCleaning
{
    /// <summary>
    /// The Steam achievements (Docs/ACHIEVEMENTS.md). Append only, never reorder: the index is the SaveData.achieved index,
    /// the Steamworks creation order and the localization token NEW_ACHIEVEMENT_1_&lt;index&gt;; the English source is
    /// SteamStore/achievements/source_en.json (same order).
    /// </summary>
    public enum AchievementId
    {
        FirstCarpet = 0,
        Spotless = 1,
        BoneDry = 2,
        Combo = 3,
        Filthy = 4,
        Express = 5,
        Regulars = 6,
        FullKit = 7,
        MaxedOut = 8,
        Inspector = 9,
    }

    /// <summary>
    /// Achievement rules as pure functions of a pay slip and the save (Docs/ACHIEVEMENTS.md): no Steam, no scene, so tests
    /// and CarpetDebugApi.AchievementsPreview can call them anywhere. SteamAchievements applies them and talks to Steam.
    /// </summary>
    public static class Achievements
    {
        public const int Count = 10;

        public const string StatCarpets = "STAT_CARPETS";        // SaveData.carpetsDelivered, increment-only
        public const string StatToolsUsed = "STAT_TOOLS_USED";   // bits in SaveData.toolsUsed, increment-only

        public const int RegularsTarget = 25;
        /// <summary>Tools that make the full kit: ToolTable.All.Length, and the max of STAT_TOOLS_USED in Steamworks.</summary>
        public const int FullKitTarget = 10;
        /// <summary>A carpet is dirty when V is above this (the pay slip's "Already spotless" class ends here).</summary>
        public const float DirtyMinValue = PayoutResult.SpotlessMaxValue;
        /// <summary>The pay slip's "Filthy" class starts here.</summary>
        public const float FilthyMinValue = PayoutResult.HeavyMaxValue;
        /// <summary>Express Service needs "Heavy soil" or "Filthy": V from the end of "Medium soil" on.</summary>
        public const float ExpressMinValue = PayoutResult.MediumMaxValue;
        /// <summary>Peak carpet-mean Wetness during the job that counts as "soaked" for Bone Dry (tuned in Play, Docs/ACHIEVEMENTS.md).</summary>
        public const float DefaultBoneDryPeakWetness = 0.2f;

        /// <summary>One row of the Steamworks schema: API name, and the stat Steam draws its progress bar from (null if none).</summary>
        public readonly struct Def
        {
            public readonly AchievementId id;
            public readonly string api;
            public readonly string progressStat;
            public readonly int progressMax;

            public Def(AchievementId id, string api, string progressStat = null, int progressMax = 0)
            {
                this.id = id; this.api = api; this.progressStat = progressStat; this.progressMax = progressMax;
            }
        }

        /// <summary>Indexed by AchievementId.</summary>
        public static readonly Def[] Defs =
        {
            new Def(AchievementId.FirstCarpet, "ACH_FIRST_CARPET"),
            new Def(AchievementId.Spotless,    "ACH_SPOTLESS"),
            new Def(AchievementId.BoneDry,     "ACH_BONE_DRY"),
            new Def(AchievementId.Combo,       "ACH_COMBO"),
            new Def(AchievementId.Filthy,      "ACH_FILTHY"),
            new Def(AchievementId.Express,     "ACH_EXPRESS"),
            new Def(AchievementId.Regulars,    "ACH_REGULARS", StatCarpets, RegularsTarget),
            new Def(AchievementId.FullKit,     "ACH_FULL_KIT", StatToolsUsed, FullKitTarget),
            new Def(AchievementId.MaxedOut,    "ACH_MAXED_OUT"),
            new Def(AchievementId.Inspector,   "ACH_INSPECTOR"),
        };

        public static readonly string[] Stats = { StatCarpets, StatToolsUsed };

        /// <summary>Steamworks API name, "" for an id outside the table.</summary>
        public static string Api(AchievementId id)
        {
            int i = (int)id;
            return i >= 0 && i < Defs.Length ? Defs[i].api : "";
        }

        public static bool TryParse(string api, out AchievementId id)
        {
            for (int i = 0; i < Defs.Length; i++)
            {
                if (Defs[i].api != api) continue;
                id = Defs[i].id;
                return true;
            }
            id = AchievementId.FirstCarpet;
            return false;
        }

        /// <summary>Earned in the save (the local truth).</summary>
        public static bool Earned(SaveData s, AchievementId id)
        {
            int i = (int)id;
            return s != null && s.achieved != null && i >= 0 && i < s.achieved.Length && s.achieved[i] > 0;
        }

        public static int ToolsUsedCount(int mask)
        {
            int n = 0;
            for (uint m = (uint)mask; m != 0; m &= m - 1) n++;
            return n;
        }

        /// <summary>The save's value of a progress stat (what Steam should hold at least); 0 for an unknown name.</summary>
        public static int LocalStat(SaveData s, string stat)
        {
            if (s == null) return 0;
            if (stat == StatCarpets) return s.carpetsDelivered;
            if (stat == StatToolsUsed) return ToolsUsedCount(s.toolsUsed);
            return 0;
        }

        /// <summary>
        /// What one paid hand-over earns (the slip's own classes and bonuses). peakWetness = highest carpet-mean Wetness seen
        /// during the job (SteamAchievements.PeakWetness), boneDryPeak = the level that counts as soaked. Appends to `into`,
        /// earned or not before: the caller skips what the save already has.
        /// </summary>
        public static void PayoutUnlocks(PayoutResult r, float peakWetness, float boneDryPeak, List<AchievementId> into)
        {
            if (into == null || !r.valid) return;
            float v = r.jobValue;
            bool dirty = v > DirtyMinValue;
            if (r.perfectClean && dirty) into.Add(AchievementId.Spotless);
            if (r.perfectDry && peakWetness >= boneDryPeak) into.Add(AchievementId.BoneDry);
            if (r.combo && dirty) into.Add(AchievementId.Combo);
            if (r.perfectClean && v >= FilthyMinValue) into.Add(AchievementId.Filthy);
            if (v >= ExpressMinValue && r.seconds <= r.parSeconds) into.Add(AchievementId.Express);   // the full speed bonus
        }

        /// <summary>
        /// What the save alone earns: counters, tools used, upgrade levels, items. Safe to run any time (Start re-grants
        /// from an old save); appends to `into` whatever holds now, earned before or not.
        /// </summary>
        public static void StateUnlocks(SaveData s, int toolCount, int maxLevel, List<AchievementId> into)
        {
            if (s == null || into == null) return;
            if (s.carpetsDelivered >= 1) into.Add(AchievementId.FirstCarpet);
            if (s.carpetsDelivered >= RegularsTarget) into.Add(AchievementId.Regulars);
            if (toolCount > 0 && ToolsUsedCount(s.toolsUsed) >= toolCount) into.Add(AchievementId.FullKit);
            if (maxLevel > 0 && s.power != null && s.area != null)
            {
                int n = System.Math.Min(s.power.Length, s.area.Length);
                for (int i = 0; i < n; i++)
                {
                    if (s.power[i] < maxLevel || s.area[i] < maxLevel) continue;
                    into.Add(AchievementId.MaxedOut);
                    break;
                }
            }
            if (s.items != null && s.items.Length >= ShopItems.Count)
            {
                bool all = true;
                for (int i = 0; i < ShopItems.Count; i++) if (s.items[i] <= 0) { all = false; break; }
                if (all) into.Add(AchievementId.Inspector);
            }
        }
    }
}
