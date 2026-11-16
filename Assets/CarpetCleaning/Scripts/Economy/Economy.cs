using System;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// The wallet, the carpet clock, the pay slip and the upgrade shop's rules (Docs/ECONOMY.md). Lives on the "Economy"
    /// object, which CarpetSceneBuilder keeps across rebuilds (like Steam).
    ///
    /// Pay: CarpetHandover.StateChanged(FlyOut) is the one "carpet handed over" signal (only Begin() raises it). The stats are
    /// re-read synchronously there, the money is credited and saved at once, and PayoutReady hands the slip to the HUD,
    /// which only replays it: quitting mid-animation never loses money.
    /// Clock: sim seconds (CarpetSimulation.SimTime) since the carpet was last entered Idle (landing, R / Reset, first
    /// carpet), so it stops under the Steam overlay and in the shop (both pause the sim) and does not punish a slow frame rate.
    /// </summary>
    [DefaultExecutionOrder(215)]
    public sealed class Economy : MonoBehaviour
    {
        public EconomySettings settings;
        public CarpetSimulation sim;
        public CarpetHandover handover;
        public ToolController tools;

        public static Economy Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        SaveData _save;
        string _loadNote = "";
        float _clockAnchor;
        bool _clockRunning;
        float _frozenSeconds;
        bool _subscribed;
        CarpetSimulation _subSim;
        CarpetHandover _subHandover;

        public event Action<PayoutResult> PayoutReady;
        /// <summary>A carpet was really handed over and paid (raised only by Pay, after the save; DebugShowPayout raises
        /// PayoutReady alone). SteamAchievements unlocks on it (Docs/ACHIEVEMENTS.md).</summary>
        public event Action<PayoutResult> Paid;
        /// <summary>Fired after every change of the wallet: (new balance, delta).</summary>
        public event Action<int, int> MoneyChanged;
        public event Action<ToolId, UpgradeStat, int> Upgraded;
        /// <summary>A one-time item was bought or (debug) granted / taken away: (item, owned now).</summary>
        public event Action<ShopItem, bool> ItemChanged;
        /// <summary>A job was dropped without a hand-over (R / Reset / a new carpet from the debug API): the HUD fast-forwards its card.</summary>
        public event Action JobRestarted;

        public int Money { get { return _save != null ? _save.money : 0; } }
        public SaveData Data { get { return _save; } }
        public string LoadNote { get { return _loadNote; } }
        public PayoutResult LastPayout { get; private set; }
        public int MaxLevel { get { return settings != null ? Mathf.Max(settings.maxLevel, 0) : 6; } }

        /// <summary>Seconds on the clock of the carpet on the sim (frozen from the hand-over until the next carpet lands).</summary>
        public float CarpetSeconds
        {
            get
            {
                if (!_clockRunning || sim == null) return _frozenSeconds;
                if (sim.SimTime < _clockAnchor) _clockAnchor = sim.SimTime;   // a reset without an Idle entry restarted SimTime
                return sim.SimTime - _clockAnchor;
            }
        }

        public bool ClockRunning { get { return _clockRunning; } }

        /// <summary>V of the carpet on the sim: its starting dirt against the fully soiled carpet.</summary>
        public float JobValue
        {
            get
            {
                if (sim == null || settings == null || !sim.InitStats.valid) return 0f;
                return Mathf.Clamp01(CarpetSimulation.DirtScore(sim.InitStats) / Mathf.Max(settings.dirtRef, 1e-4f));
            }
        }

        public float ParSeconds { get { return settings != null ? PayoutResult.Par(settings, JobValue) : 0f; } }

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            if (Instance != null && Instance != this) { Debug.LogWarning("[Economy] second instance ignored"); enabled = false; return; }
            Instance = this;
            if (settings == null) settings = ScriptableObject.CreateInstance<EconomySettings>();
            if (sim == null) sim = UnityEngine.Object.FindAnyObjectByType<CarpetSimulation>();
            if (handover == null) handover = UnityEngine.Object.FindAnyObjectByType<CarpetHandover>();
            if (tools == null) tools = UnityEngine.Object.FindAnyObjectByType<ToolController>();
            _save = SaveStore.Load(out _loadNote);
            _save.Normalize(ToolTable.All.Length, MaxLevel);
            ApplyTuning();
            LastPayout = default;
        }

        void OnEnable() { Subscribe(); }
        void OnDisable() { Unsubscribe(); }

        void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        void Start() { StartClock(); }

        void Subscribe()
        {
            if (_subscribed) return;
            if (handover != null) handover.StateChanged += OnHandoverState;
            if (sim != null) sim.CarpetReset += OnCarpetReset;
            _subHandover = handover; _subSim = sim;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_subHandover != null) _subHandover.StateChanged -= OnHandoverState;
            if (_subSim != null) _subSim.CarpetReset -= OnCarpetReset;
            _subHandover = null; _subSim = null;
            _subscribed = false;
        }

        void OnHandoverState(HandoverState s)
        {
            if (s == HandoverState.Idle) StartClock();
            else if (s == HandoverState.FlyOut) Pay();
        }

        void OnCarpetReset()
        {
            StartClock();
            JobRestarted?.Invoke();
        }

        void StartClock()
        {
            _clockAnchor = sim != null ? sim.SimTime : 0f;
            _clockRunning = true;
            _frozenSeconds = 0f;
        }

        // ------------------------------------------------------------------ pay
        void Pay()
        {
            if (settings == null || sim == null) return;
            float seconds = CarpetSeconds;
            _clockRunning = false;
            _frozenSeconds = seconds;

            sim.RefreshStatsSync();   // the async stats may be up to 10 frames old: the slip uses the exact hand-over state
            var r = PayoutResult.Compute(settings, sim.Cleanliness, 1f - sim.Wetness, seconds, JobValue);
            r.dirtLevel = sim.DirtLevel;
            r.pictureName = handover != null ? handover.PictureName : "";
            r.moneyBefore = _save.money;
            _save.money = SafeAdd(_save.money, r.total);
            r.moneyAfter = _save.money;
            _save.lifetimeEarned += r.total;
            _save.carpetsDelivered++;
            if (r.perfectClean) _save.perfectClean++;
            if (r.perfectDry) _save.perfectDry++;
            if (r.combo) _save.combos++;
            if (r.jobValue >= 0.5f && (_save.bestSeconds <= 0f || seconds < _save.bestSeconds)) _save.bestSeconds = seconds;
            r.carpetNumber = _save.carpetsDelivered;
            LastPayout = r;
            Persist();
            Paid?.Invoke(r);
            MoneyChanged?.Invoke(_save.money, r.total);
            PayoutReady?.Invoke(r);
        }

        static int SafeAdd(int a, int b)
        {
            long x = (long)a + b;
            return (int)Math.Max(0L, Math.Min(int.MaxValue, x));
        }

        void Persist()
        {
            if (!SaveStore.Write(_save, out var err)) Debug.LogWarning("[Economy] progress not saved: " + err);
        }

        /// <summary>Writes the save now. For systems that keep their own fields in SaveData (SteamAchievements: achieved,
        /// toolsUsed) and change them outside the economy's own paths.</summary>
        public void SaveNow() { Persist(); }

        // ------------------------------------------------------------------ upgrades
        public int Level(ToolId t, UpgradeStat stat)
        {
            int i = (int)t;
            var arr = stat == UpgradeStat.Power ? _save.power : _save.area;
            return i >= 0 && i < arr.Length ? arr[i] : 0;
        }

        public bool IsMax(ToolId t, UpgradeStat stat) { return Level(t, stat) >= MaxLevel; }

        /// <summary>Price of the next level, -1 at max.</summary>
        public int Price(ToolId t, UpgradeStat stat)
        {
            int l = Level(t, stat);
            if (l >= MaxLevel || settings == null) return -1;
            float p = settings.priceBase * Mathf.Pow(Mathf.Max(settings.priceGrowth, 1f), l);
            if (stat == UpgradeStat.Area) p *= settings.areaPriceFactor;
            if (ToolTable.IsFloorTool(t)) p *= settings.floorToolPriceFactor;
            int step = Mathf.Max(settings.roundTo, 1);
            return Mathf.Max(step, Mathf.RoundToInt(p / step) * step);
        }

        public bool CanAfford(ToolId t, UpgradeStat stat) { int p = Price(t, stat); return p >= 0 && _save.money >= p; }

        /// <summary>True when any upgrade or unowned one-time item is affordable (the HUD's badge).</summary>
        public bool AnyAffordable()
        {
            for (int i = 0; i < ToolTable.All.Length; i++)
                if (CanAfford((ToolId)i, UpgradeStat.Power) || CanAfford((ToolId)i, UpgradeStat.Area)) return true;
            for (int i = 0; i < ShopItems.Count; i++)
            {
                int p = ItemPrice((ShopItem)i);
                if (p >= 0 && _save.money >= p) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ one-time items (Docs/HIGHLIGHTS.md)
        public bool Owns(ShopItem item)
        {
            int i = (int)item;
            return _save != null && i >= 0 && i < _save.items.Length && _save.items[i] > 0;
        }

        /// <summary>Price of an item not owned yet, -1 once it is.</summary>
        public int ItemPrice(ShopItem item)
        {
            if (Owns(item)) return -1;
            if (settings == null) return 0;
            int p = item == ShopItem.DirtHighlight ? settings.dirtHighlightPrice : settings.moistureHighlightPrice;
            return Mathf.Max(p, 0);
        }

        public bool TryBuyItem(ShopItem item)
        {
            int price = ItemPrice(item);
            if (price < 0 || _save.money < price) return false;
            _save.money -= price;
            _save.items[(int)item] = 1;
            Persist();
            MoneyChanged?.Invoke(_save.money, -price);
            ItemChanged?.Invoke(item, true);
            return true;
        }

        public bool TryBuy(ToolId t, UpgradeStat stat)
        {
            int price = Price(t, stat);
            if (price < 0 || _save.money < price) return false;
            _save.money -= price;
            var arr = stat == UpgradeStat.Power ? _save.power : _save.area;
            arr[(int)t] = Mathf.Min(arr[(int)t] + 1, MaxLevel);
            ApplyTuning();
            Persist();
            MoneyChanged?.Invoke(_save.money, -price);
            Upgraded?.Invoke(t, stat, arr[(int)t]);
            return true;
        }

        /// <summary>Multiplier against the factory tuning at `level` (what the simulation uses).</summary>
        public float Multiplier(ToolId t, UpgradeStat stat, int level)
        {
            if (settings == null) return 1f;
            int i = (int)t;
            if (stat == UpgradeStat.Power)
            {
                int a = settings.powerAnchor != null && i < settings.powerAnchor.Length ? settings.powerAnchor[i] : 0;
                return Mathf.Pow(settings.powerStep, level - a);
            }
            else
            {
                int a = settings.areaAnchor != null && i < settings.areaAnchor.Length ? settings.areaAnchor[i] : 0;
                return Mathf.Pow(AreaStep, level - a);
            }
        }

        /// <summary>AREA ratio from one level to the next: areaMax spread evenly (in ratio) over the levels.</summary>
        public float AreaStep
        {
            get
            {
                if (settings == null) return 1f;
                return Mathf.Pow(Mathf.Max(settings.areaMax, 0.01f), 1f / Mathf.Max(settings.maxLevel, 1));
            }
        }

        /// <summary>What the shop shows: the gain over the player's own level 0, e.g. 0.32 for x1.32.</summary>
        public float GainOverStart(UpgradeStat stat, int level)
        {
            if (settings == null) return 0f;
            float step = stat == UpgradeStat.Power ? settings.powerStep : AreaStep;
            return Mathf.Pow(step, level) - 1f;
        }

        public void ApplyTuning()
        {
            for (int i = 0; i < ToolTable.All.Length; i++)
            {
                var t = (ToolId)i;
                ToolTuning.Set(t, Multiplier(t, UpgradeStat.Power, Level(t, UpgradeStat.Power)), Multiplier(t, UpgradeStat.Area, Level(t, UpgradeStat.Area)));
            }
        }

        // ------------------------------------------------------------------ debug (EconomyDebugApi)
        public void DebugGrant(int amount)
        {
            int before = _save.money;
            _save.money = SafeAdd(_save.money, amount);
            Persist();
            MoneyChanged?.Invoke(_save.money, _save.money - before);
        }

        public void DebugSetLevel(ToolId t, UpgradeStat stat, int level)
        {
            var arr = stat == UpgradeStat.Power ? _save.power : _save.area;
            int i = (int)t;
            if (i < 0 || i >= arr.Length) return;
            arr[i] = Mathf.Clamp(level, 0, MaxLevel);
            ApplyTuning();
            Persist();
            Upgraded?.Invoke(t, stat, arr[i]);
        }

        /// <summary>Grants or takes away a one-time item without money (tests, the Economy menu).</summary>
        public void DebugSetOwned(ShopItem item, bool owned)
        {
            int i = (int)item;
            if (i < 0 || i >= _save.items.Length) return;
            _save.items[i] = owned ? 1 : 0;
            Persist();
            ItemChanged?.Invoke(item, owned);
        }

        public bool DebugResetSave()
        {
            var fresh = new SaveData();
            fresh.Normalize(ToolTable.All.Length, MaxLevel);
            // Commit the reset before changing live progress; a failed write must leave the old state intact.
            if (!SaveStore.Write(fresh, out _)) return false;
            _save = fresh;
            _loadNote = "reset";
            LastPayout = default;
            ApplyTuning();
            MoneyChanged?.Invoke(0, 0);
            RaiseItems();
            return true;
        }

        /// <summary>Re-reads the file (after a test wrote to it).</summary>
        public void DebugReload()
        {
            _save = SaveStore.Load(out _loadNote);
            _save.Normalize(ToolTable.All.Length, MaxLevel);
            ApplyTuning();
            MoneyChanged?.Invoke(_save.money, 0);
            RaiseItems();
        }

        void RaiseItems()
        {
            for (int i = 0; i < ShopItems.Count; i++) ItemChanged?.Invoke((ShopItem)i, Owns((ShopItem)i));
        }

        /// <summary>Replays a slip through the normal path without touching the wallet (card tests).</summary>
        public void DebugShowPayout(PayoutResult r)
        {
            r.valid = true;
            r.moneyBefore = _save.money;
            r.moneyAfter = _save.money;
            LastPayout = r;
            PayoutReady?.Invoke(r);
        }
    }
}
