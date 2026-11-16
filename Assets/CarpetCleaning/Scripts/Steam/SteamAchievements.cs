#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || UNITY_ANDROID || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace CarpetCleaning
{
    /// <summary>
    /// Steam achievements and stats (Docs/ACHIEVEMENTS.md), on the "Steam" object. The save is the local truth
    /// (SaveData.achieved, toolsUsed, carpetsDelivered, upgrade levels, items; Auto-Cloud carries it between machines): the
    /// rules of Achievements.cs are applied and saved with or without Steam, and reach Steam only while SteamActive.
    /// Nothing is ever revoked.
    /// Steam: Start syncs both ways (earned in the save -> SetAchievement, unlocked on Steam -> earned in the save; the
    /// stats only grow). An unlock stores at once (that shows the toast); a stat-only change waits statsStoreInterval real
    /// seconds, and OnApplicationQuit stores what is left before SteamService shuts Steam down. A name the PUBLISHED
    /// Steamworks schema lacks makes Get/Set return false: it is logged once, listed by DebugJson and skipped from then on.
    /// Order 222 = after CarpetHandover (210), Economy (215) and SteamRichPresence (220).
    /// </summary>
    [DefaultExecutionOrder(222)]
    [DisallowMultipleComponent]
    public sealed class SteamAchievements : MonoBehaviour
    {
        public CarpetSimulation sim;
        public CarpetHandover handover;
        public ToolController tools;
        public Economy economy;
        [Tooltip("Push unlocks and stats to Steam from Play mode in the Editor. Off by default: Editor Play inits Steam with the real App ID, so tests would unlock achievements on the developer's own account.")]
        public bool steamInEditor = false;
        [Tooltip("Peak carpet-mean wetness (0..1) during the job that counts as soaked for Bone Dry (Docs/ACHIEVEMENTS.md).")]
        public float boneDryPeakWetness = Achievements.DefaultBoneDryPeakWetness;
        [Tooltip("Real seconds between two StoreStats for stat-only changes. An unlock stores at once.")]
        public float statsStoreInterval = 60f;

        public static SteamAchievements Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        /// <summary>Highest carpet-mean Wetness of the job while the carpet sat at home (Bone Dry's "soaked" test). Back to 0
        /// on a full reset and when the next carpet flies in.</summary>
        public float PeakWetness { get; private set; }
        public int StoreCount { get; private set; }
        public string LastStoreResult { get; private set; } = "";
        /// <summary>Time.realtimeSinceStartup of the last StoreStats, -1 = none yet.</summary>
        public float LastStoreTime { get; private set; } = -1f;
        /// <summary>Steam may be used: always in a player, in the Editor only with steamInEditor.</summary>
        public bool SteamAllowed { get { return !Application.isEditor || steamInEditor; } }
        /// <summary>Unlocks and stats go to Steam. False = nothing touches Steamworks; the save still works.</summary>
        public bool SteamActive { get { return SteamService.IsReady && SteamAllowed; } }

        // never cached: EconomyDebugApi's reload / reset replace the SaveData object
        SaveData Data { get { return economy != null ? economy.Data : null; } }

        readonly List<AchievementId> _found = new List<AchievementId>(Achievements.Count);
        readonly HashSet<string> _schemaMissing = new HashSet<string>();
        readonly bool?[] _steamAchieved = new bool?[Achievements.Count];   // last read from / sent to Steam, null = unknown
        int? _steamCarpets, _steamToolsUsed;
        bool _saveDirty, _storeNow, _statsDirty;
        float _lastStoreReal;
        bool _subscribed;
        CarpetSimulation _subSim;
        CarpetHandover _subHandover;
        ToolController _subTools;
        Economy _subEconomy;
#if !DISABLESTEAMWORKS
        Callback<UserStatsReceived_t> _cbReceived;
        Callback<UserStatsStored_t> _cbStored;
        Callback<UserAchievementStored_t> _cbAchievementStored;
#endif

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            if (Instance != null && Instance != this) { Debug.LogWarning("[Achievements] second instance ignored"); enabled = false; return; }
            Instance = this;
            if (sim == null) sim = FindAnyObjectByType<CarpetSimulation>();
            if (handover == null) handover = FindAnyObjectByType<CarpetHandover>();
            if (tools == null) tools = FindAnyObjectByType<ToolController>();
            if (economy == null) economy = Economy.Instance != null ? Economy.Instance : FindAnyObjectByType<Economy>();
        }

        void OnEnable() { Subscribe(); }
        void OnDisable() { Unsubscribe(); }

        void Start()
        {
            _lastStoreReal = Time.realtimeSinceStartup;
            // Steam first: the sync reads what Steam holds, so the retro-grants below push (and store) only what is new to it
            if (SteamActive) SyncWithSteam();
            EvaluateState("save");   // retro-grants: carpets delivered, upgrades and items from before the achievements existed
            FlushSave();
        }

        void OnDestroy()
        {
            Unsubscribe();
            DisposeCallbacks();
            if (Instance == this) Instance = null;
        }

        void LateUpdate()
        {
            FlushSave();   // once per frame, however many unlocks it had
            if (!SteamActive) return;
            if (_storeNow || (_statsDirty && Time.realtimeSinceStartup - _lastStoreReal >= Mathf.Max(statsStoreInterval, 0f))) StoreNow();
        }

        // before SteamService.OnDestroy shuts Steam down
        void OnApplicationQuit()
        {
            FlushSave();
            if (SteamActive && (_storeNow || _statsDirty)) StoreNow();
        }

        void Subscribe()
        {
            if (_subscribed) return;
            if (economy != null) { economy.Paid += OnPaid; economy.Upgraded += OnUpgraded; economy.ItemChanged += OnItemChanged; }
            if (tools != null) tools.Stamped += OnStamped;
            if (sim != null) { sim.StatsUpdated += OnStatsUpdated; sim.CarpetReset += OnCarpetReset; }
            if (handover != null) handover.StateChanged += OnHandoverState;
            _subEconomy = economy; _subTools = tools; _subSim = sim; _subHandover = handover;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_subEconomy != null) { _subEconomy.Paid -= OnPaid; _subEconomy.Upgraded -= OnUpgraded; _subEconomy.ItemChanged -= OnItemChanged; }
            if (_subTools != null) _subTools.Stamped -= OnStamped;
            if (_subSim != null) { _subSim.StatsUpdated -= OnStatsUpdated; _subSim.CarpetReset -= OnCarpetReset; }
            if (_subHandover != null) _subHandover.StateChanged -= OnHandoverState;
            _subEconomy = null; _subTools = null; _subSim = null; _subHandover = null;
            _subscribed = false;
        }

        void FlushSave()
        {
            if (!_saveDirty) return;
            _saveDirty = false;
            if (economy != null) economy.SaveNow();
        }

        // ------------------------------------------------------------------ game events
        void OnPaid(PayoutResult r)
        {
            var d = Data;
            if (d == null) return;
            _found.Clear();
            Achievements.PayoutUnlocks(r, PeakWetness, boneDryPeakWetness, _found);
            if (_found.Count > 0)
            {
                string why = PayoutReason(r);
                for (int i = 0; i < _found.Count; i++) Unlock(_found[i], why);
            }
            EvaluateState("hand-over");
            int n = d.carpetsDelivered;
            PushStat(Achievements.StatCarpets, n);
            if (n > 0 && n % 5 == 0 && n < Achievements.RegularsTarget) IndicateProgress(AchievementId.Regulars, n, Achievements.RegularsTarget);
            FlushSave();
        }

        string PayoutReason(PayoutResult r)
        {
            var c = CultureInfo.InvariantCulture;
            return "carpet #" + r.carpetNumber.ToString(c) + ": V " + r.jobValue.ToString("0.###", c)
                 + ", clean " + r.clean.ToString("0.####", c) + ", dry " + r.dry.ToString("0.####", c)
                 + ", peak wetness " + PeakWetness.ToString("0.###", c)
                 + ", " + r.seconds.ToString("0.#", c) + " s / par " + r.parSeconds.ToString("0.#", c) + " s";
        }

        void OnUpgraded(ToolId t, UpgradeStat stat, int level) { EvaluateState("upgrade"); }

        void OnItemChanged(ShopItem item, bool owned) { if (owned) EvaluateState("item"); }

        void OnStamped(ToolId t, Vector2 uv)
        {
            var d = Data;
            int i = (int)t;
            if (d == null || i < 0 || i >= ToolTable.All.Length || i >= 31) return;   // ToolTable.Get clamps a bad debug id: never count it
            int bit = 1 << i;
            if ((d.toolsUsed & bit) != 0) return;                     // every stroke frame lands here: the usual case leaves at once
            if (ToolTable.IsFloorTool(t) && OnCarpet(uv)) return;     // the floor tools do nothing on the carpet
            d.toolsUsed |= bit;
            _saveDirty = true;
            int n = Achievements.ToolsUsedCount(d.toolsUsed);
            Debug.Log("[Achievements] first use of " + t + " (" + n.ToString(CultureInfo.InvariantCulture) + "/"
                      + ToolTable.All.Length.ToString(CultureInfo.InvariantCulture) + " tools)");
            PushStat(Achievements.StatToolsUsed, n);
            if (n == 5) IndicateProgress(AchievementId.FullKit, n, Achievements.FullKitTarget);
            EvaluateState("tools");
        }

        bool OnCarpet(Vector2 uv)
        {
            bool inRect = uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
            return inRect && (sim == null || !sim.CarpetAway);
        }

        void OnStatsUpdated(SimStats s)
        {
            if (sim == null) return;
            if (handover != null && handover.State != HandoverState.Idle) return;   // the flight belongs to no job
            float w = sim.Wetness;
            if (w > PeakWetness) PeakWetness = w;
        }

        void OnCarpetReset() { ClearPeak(); }

        void OnHandoverState(HandoverState s) { if (s == HandoverState.FlyIn) ClearPeak(); }

        /// <summary>
        /// A new job starts at 0. CarpetSimulation's async stats readback (every 10 frames) carries no reset generation: one
        /// still in flight would land after the reset with the OLD carpet's Wetness and, the state being Idle, count for
        /// the new job (Bone Dry on a carpet that never got wet). WaitAllRequests delivers those callbacks here, before
        /// the zero; ResetCarpet has just stalled on its own readback, so it costs nothing extra.
        /// </summary>
        void ClearPeak()
        {
            UnityEngine.Rendering.AsyncGPUReadback.WaitAllRequests();
            PeakWetness = 0f;
        }

        // ------------------------------------------------------------------ unlocks
        /// <summary>Applies Achievements.StateUnlocks to the save as it is now.</summary>
        void EvaluateState(string reason)
        {
            var d = Data;
            if (d == null) return;
            _found.Clear();
            Achievements.StateUnlocks(d, ToolTable.All.Length, economy.MaxLevel, _found);
            for (int i = 0; i < _found.Count; i++) Unlock(_found[i], reason);
        }

        /// <summary>
        /// Earns `id` in the save (logged the first time) and, while SteamActive, on Steam with a store in this frame's
        /// LateUpdate (the toast). True when it was new to the save.
        /// </summary>
        public bool Unlock(AchievementId id, string reason)
        {
            var d = Data;
            int i = (int)id;
            if (d == null || d.achieved == null || i < 0 || i >= d.achieved.Length) return false;
            bool fresh = d.achieved[i] <= 0;
            if (fresh)
            {
                d.achieved[i] = 1;
                _saveDirty = true;
                Debug.Log("[Achievements] unlocked " + Achievements.Api(id) + " (" + reason + ")");
            }
            if (SteamActive && _steamAchieved[i] != true) PushAchievement(i);
            return fresh;
        }

        int? StatCache(string stat) { return stat == Achievements.StatCarpets ? _steamCarpets : _steamToolsUsed; }

        void SetStatCache(string stat, int? v)
        {
            if (stat == Achievements.StatCarpets) _steamCarpets = v;
            else _steamToolsUsed = v;
        }

        // ------------------------------------------------------------------ Steam
        void PushAchievement(int i)
        {
#if !DISABLESTEAMWORKS
            string api = Achievements.Defs[i].api;
            if (_schemaMissing.Contains(api)) return;
            if (SteamUserStats.SetAchievement(api)) { _steamAchieved[i] = true; _storeNow = true; }
            else SchemaMissing(api, "SetAchievement");
#endif
        }

        /// <summary>Raises a stat on Steam to `value`; never lowers it (both stats are increment-only in Steamworks).</summary>
        void PushStat(string stat, int value)
        {
#if !DISABLESTEAMWORKS
            if (!SteamActive || _schemaMissing.Contains(stat)) return;
            int? known = StatCache(stat);
            if (!known.HasValue)
            {
                if (!SteamUserStats.GetStat(stat, out int cur)) { SchemaMissing(stat, "GetStat"); return; }
                known = cur;
                SetStatCache(stat, cur);
            }
            if (value <= known.Value) return;
            if (!SteamUserStats.SetStat(stat, value)) { SchemaMissing(stat, "SetStat"); return; }
            SetStatCache(stat, value);
            _statsDirty = true;
#endif
        }

        /// <summary>Steam's progress popup ("5/25"). Only a popup: skipped once earned or when the name is not published.</summary>
        void IndicateProgress(AchievementId id, int cur, int max)
        {
#if !DISABLESTEAMWORKS
            string api = Achievements.Api(id);
            if (!SteamActive || cur <= 0 || cur >= max || Achievements.Earned(Data, id) || _schemaMissing.Contains(api)) return;
            SteamUserStats.IndicateAchievementProgress(api, (uint)cur, (uint)max);
#endif
        }

        void SchemaMissing(string name, string call)
        {
            if (!_schemaMissing.Add(name)) return;
            Debug.LogWarning("[Achievements] " + name + " is not in the published Steamworks schema (" + call
                             + " returned false): it stays in the save until the schema is published (Docs/ACHIEVEMENTS.md)");
        }

        /// <summary>
        /// Both ways, never revoking: earned in the save but not on Steam -> SetAchievement; unlocked on Steam but not in the
        /// save -> earned in the save (another machine, an older save); a stat below the save's value -> raised. One store
        /// when anything went to Steam. False when Steam is not active. Safe to call again (CarpetDebugApi.AchievementsSteamSync).
        /// </summary>
        public bool SyncWithSteam()
        {
#if DISABLESTEAMWORKS
            return false;
#else
            if (!SteamActive) return false;
            RegisterCallbacks();
            var d = Data;
            bool pushed = false;
            for (int i = 0; i < Achievements.Count; i++)
            {
                var def = Achievements.Defs[i];
                if (!SteamUserStats.GetAchievement(def.api, out bool onSteam))
                {
                    _steamAchieved[i] = null;
                    SchemaMissing(def.api, "GetAchievement");
                    continue;
                }
                _schemaMissing.Remove(def.api);
                _steamAchieved[i] = onSteam;
                bool local = Achievements.Earned(d, def.id);
                if (local && !onSteam)
                {
                    if (SteamUserStats.SetAchievement(def.api))
                    {
                        _steamAchieved[i] = true;
                        pushed = true;
                        Debug.Log("[Achievements] " + def.api + " is earned in the save: set on Steam");
                    }
                    else SchemaMissing(def.api, "SetAchievement");
                }
                else if (onSteam && !local && d != null && d.achieved != null && i < d.achieved.Length)
                {
                    d.achieved[i] = 1;
                    _saveDirty = true;
                    Debug.Log("[Achievements] " + def.api + " is unlocked on Steam: earned in the save too");
                }
            }
            for (int s = 0; s < Achievements.Stats.Length; s++)
                pushed |= SyncStat(Achievements.Stats[s], Achievements.LocalStat(d, Achievements.Stats[s]));
            if (pushed) StoreNow();
            return true;
#endif
        }

#if !DISABLESTEAMWORKS
        bool SyncStat(string stat, int local)
        {
            if (!SteamUserStats.GetStat(stat, out int onSteam))
            {
                SetStatCache(stat, null);
                SchemaMissing(stat, "GetStat");
                return false;
            }
            _schemaMissing.Remove(stat);
            SetStatCache(stat, onSteam);
            if (local <= onSteam) return false;
            if (!SteamUserStats.SetStat(stat, local)) { SchemaMissing(stat, "SetStat"); return false; }
            SetStatCache(stat, local);
            return true;
        }

        /// <summary>Re-reads what Steam holds without sending anything (after Steam refused a store).</summary>
        void ReadSteamState()
        {
            for (int i = 0; i < Achievements.Count; i++)
                _steamAchieved[i] = SteamUserStats.GetAchievement(Achievements.Defs[i].api, out bool a) ? a : (bool?)null;
            for (int s = 0; s < Achievements.Stats.Length; s++)
                SetStatCache(Achievements.Stats[s], SteamUserStats.GetStat(Achievements.Stats[s], out int v) ? v : (int?)null);
        }

        void RegisterCallbacks()
        {
            if (_cbReceived != null) return;
            _cbReceived = Callback<UserStatsReceived_t>.Create(OnStatsReceived);
            _cbStored = Callback<UserStatsStored_t>.Create(OnStatsStored);
            _cbAchievementStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);
        }

        static bool IsThisGame(ulong gameId) { return SteamService.IsReady && gameId == (ulong)SteamUtils.GetAppID().m_AppId; }

        void OnStatsReceived(UserStatsReceived_t e)
        {
            if (!IsThisGame(e.m_nGameID) || e.m_steamIDUser != SteamUser.GetSteamID()) return;
            if (e.m_eResult != EResult.k_EResultOK) { Debug.LogWarning("[Achievements] UserStatsReceived: " + e.m_eResult); return; }
            // SDK 1.61+ has the stats before init returns and Start already synced; a later reload is synced again
            SyncWithSteam();
        }

        void OnStatsStored(UserStatsStored_t e)
        {
            if (!IsThisGame(e.m_nGameID)) return;
            LastStoreResult = e.m_eResult.ToString();
            if (e.m_eResult == EResult.k_EResultOK) return;
            if (e.m_eResult == EResult.k_EResultInvalidParam)
            {
                // a stat broke a Steamworks constraint (increment-only, max) and Steam rolled it back: take Steam's values
                Debug.LogWarning("[Achievements] StoreStats: invalid param, Steam reverted a stat; re-reading");
                ReadSteamState();
                return;
            }
            Debug.LogWarning("[Achievements] StoreStats failed: " + e.m_eResult);
        }

        void OnAchievementStored(UserAchievementStored_t e)
        {
            if (!IsThisGame(e.m_nGameID)) return;
            if (e.m_nMaxProgress > 0)
                Debug.Log("[Achievements] Steam showed progress " + e.m_rgchAchievementName + " " + e.m_nCurProgress + "/" + e.m_nMaxProgress);
            else Debug.Log("[Achievements] Steam stored " + e.m_rgchAchievementName);
        }
#endif

        void DisposeCallbacks()
        {
#if !DISABLESTEAMWORKS
            if (_cbReceived != null) { _cbReceived.Dispose(); _cbReceived = null; }
            if (_cbStored != null) { _cbStored.Dispose(); _cbStored = null; }
            if (_cbAchievementStored != null) { _cbAchievementStored.Dispose(); _cbAchievementStored = null; }
#endif
        }

        bool StoreNow()
        {
            _storeNow = false;
            _statsDirty = false;
#if DISABLESTEAMWORKS
            return false;
#else
            if (!SteamActive) return false;
            bool ok = SteamUserStats.StoreStats();
            StoreCount++;
            LastStoreTime = _lastStoreReal = Time.realtimeSinceStartup;
            LastStoreResult = ok ? "sent" : "StoreStats returned false";
            if (!ok) Debug.LogWarning("[Achievements] StoreStats returned false");
            return ok;
#endif
        }

        // ------------------------------------------------------------------ debug (CarpetDebugApi.Achievements*)
        /// <summary>For CarpetDebugApi.AchievementsJson: flags, the save's side, what Steam holds (null = not read or Steam
        /// not active), the names the published schema lacks.</summary>
        public string DebugJson()
        {
            var c = CultureInfo.InvariantCulture;
            var d = Data;
            bool active = SteamActive;
            int mask = d != null ? d.toolsUsed : 0;
            var sb = new StringBuilder(1600);
            sb.Append("{\"component\":true");
            sb.Append(",\"steamAllowed\":").Append(B(SteamAllowed));
            sb.Append(",\"steamActive\":").Append(B(active));
            sb.Append(",\"steamReady\":").Append(B(SteamService.IsReady));
            sb.Append(",\"steamInEditor\":").Append(B(steamInEditor));
            sb.Append(",\"save\":").Append(B(d != null));
            sb.Append(",\"peakWetness\":").Append(N(PeakWetness));
            sb.Append(",\"boneDryPeak\":").Append(N(boneDryPeakWetness));
            sb.Append(",\"toolsUsed\":").Append(mask.ToString(c));
            sb.Append(",\"toolsUsedCount\":").Append(Achievements.ToolsUsedCount(mask).ToString(c));
            sb.Append(",\"carpets\":").Append((d != null ? d.carpetsDelivered : 0).ToString(c));
            sb.Append(",\"stores\":").Append(StoreCount.ToString(c));
            sb.Append(",\"lastStore\":\"").Append(LastStoreResult).Append('"');
            sb.Append(",\"lastStoreAgo\":").Append(LastStoreTime < 0f ? "null" : N(Time.realtimeSinceStartup - LastStoreTime));
            sb.Append(",\"pending\":{\"save\":").Append(B(_saveDirty)).Append(",\"store\":").Append(B(_storeNow))
              .Append(",\"stats\":").Append(B(_statsDirty)).Append('}');
            sb.Append(",\"achievements\":[");
            for (int i = 0; i < Achievements.Count; i++)
            {
                var def = Achievements.Defs[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"api\":\"").Append(def.api).Append('"');
                sb.Append(",\"earned\":").Append(B(Achievements.Earned(d, def.id)));
                sb.Append(",\"steam\":").Append(active ? NB(_steamAchieved[i]) : "null");
                sb.Append(",\"inSchema\":").Append(_schemaMissing.Contains(def.api) ? "false" : _steamAchieved[i].HasValue ? "true" : "null");
                if (def.progressStat != null)
                {
                    int cur = Mathf.Min(Achievements.LocalStat(d, def.progressStat), def.progressMax);
                    sb.Append(",\"progress\":\"").Append(cur.ToString(c)).Append('/').Append(def.progressMax.ToString(c)).Append('"');
                }
                sb.Append('}');
            }
            sb.Append("],\"stats\":{");
            for (int s = 0; s < Achievements.Stats.Length; s++)
            {
                int? v = StatCache(Achievements.Stats[s]);
                if (s > 0) sb.Append(',');
                sb.Append('"').Append(Achievements.Stats[s]).Append("\":").Append(active && v.HasValue ? v.Value.ToString(c) : "null");
            }
            sb.Append("},\"schemaMissing\":[");
            int k = 0;
            foreach (var name in _schemaMissing)
            {
                if (k++ > 0) sb.Append(',');
                sb.Append('"').Append(name).Append('"');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// ClearAchievement + StoreStats on the account Steam runs as, whatever steamInEditor says. Developer account only:
        /// it undoes a test unlock. The save keeps its copy (CarpetDebugApi.AchievementsForgetLocal clears that), so the
        /// next sync with Steam active sets it again unless both are cleared.
        /// </summary>
        public string DebugClearOnSteam(string api)
        {
#if DISABLESTEAMWORKS
            return "Steamworks is not supported on this platform";
#else
            if (!SteamService.IsReady) return "Steam is not running";
            if (!Achievements.TryParse(api, out var id)) return "unknown achievement " + api;
            if (!SteamUserStats.ClearAchievement(api))
            {
                SchemaMissing(api, "ClearAchievement");
                return "ClearAchievement returned false (not in the published schema)";
            }
            _steamAchieved[(int)id] = false;
            bool ok = SteamUserStats.StoreStats();
            StoreCount++;
            LastStoreTime = _lastStoreReal = Time.realtimeSinceStartup;
            LastStoreResult = ok ? "sent" : "StoreStats returned false";
            Debug.Log("[Achievements] cleared " + api + " on Steam (debug)");
            return ok ? "cleared" : "cleared, but StoreStats returned false";
#endif
        }

        /// <summary>Steam Cloud state for CarpetDebugApi.SteamJson (Docs/STEAM.md 11); every value null without Steam.</summary>
        public static string CloudJson()
        {
#if !DISABLESTEAMWORKS
            if (SteamService.IsReady)
            {
                var c = CultureInfo.InvariantCulture;
                bool account = SteamRemoteStorage.IsCloudEnabledForAccount();
                bool app = SteamRemoteStorage.IsCloudEnabledForApp();
                bool quota = SteamRemoteStorage.GetQuota(out ulong total, out ulong avail);
                int files = SteamRemoteStorage.GetFileCount();
                return "{\"account\":" + B(account) + ",\"app\":" + B(app)
                     + ",\"quotaTotal\":" + (quota ? total.ToString(c) : "null")
                     + ",\"quotaAvail\":" + (quota ? avail.ToString(c) : "null")
                     + ",\"files\":" + files.ToString(c) + "}";
            }
#endif
            return "{\"account\":null,\"app\":null,\"quotaTotal\":null,\"quotaAvail\":null,\"files\":null}";
        }

        static string B(bool b) { return b ? "true" : "false"; }
        static string NB(bool? b) { return b.HasValue ? B(b.Value) : "null"; }
        static string N(float v) { return float.IsNaN(v) || float.IsInfinity(v) ? "null" : v.ToString("0.####", CultureInfo.InvariantCulture); }
    }
}
