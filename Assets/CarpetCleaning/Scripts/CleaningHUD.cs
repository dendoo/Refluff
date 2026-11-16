using UnityEngine;
using UnityEngine.UIElements;

namespace CarpetCleaning
{
    /// <summary>
    /// UI Toolkit HUD (Docs/HUD.md). Structure lives in UI/CarpetHud.uxml, every colour and size in
    /// UI/CarpetHud.uss; this file only binds simulation state to named elements and never picks a
    /// pixel value, so a restyle never touches C#.
    ///
    /// Conceptual placement is unchanged from the uGUI version: carpet tools down the left edge,
    /// floor tools down the right edge (Docs/FLOOR_TOOLS.md), meters top left, hint top centre,
    /// Reset + FPS top right, the hand-over / take-new action bottom right (Docs/HANDOVER.md).
    /// The carpet fills the middle of the screen, so nothing that is not a tool may sit over it.
    ///
    /// Two layers, because the HUD must get out of the way while a tool is in use:
    ///   #chrome - strips, hint, Reset, FPS, actions. Fades to 0 and stops picking for as long as
    ///             ToolController.IsStroking, so nothing covers the debris the player is working on.
    ///   #bars   - the meters only. Always visible (that is the feedback the stroke is FOR), never a
    ///             pick target, dimmed while stroking so debris under them stays visible AND reachable.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CleaningHUD : MonoBehaviour
    {
        public CarpetSimulation sim;
        public ToolController tools;
        public CarpetHandover handover;
        /// <summary>The inspection lamp (Docs/HIGHLIGHTS.md): its Z / X chips sit in the meter heads.</summary>
        public SurfaceHighlight highlight;

        /// <summary>Matches the opacity transition in .layer, so a click landing right after the
        /// button is released does not hit a widget that is still half transparent.</summary>
        const float RestoreDelay = 0.07f;
        const float PulseSeconds = 0.55f;

        UIDocument _doc;
        VisualElement _root, _chrome, _bars, _hintRoot, _handWrap, _takeWrap;
        VisualElement _cleanFill, _dryFill;
        Label _cleanValue, _dryValue, _hintLabel, _fpsLabel;
        Label _dirtChip, _wetChip;
        string _toast;
        float _toastUntil;
        Button _handBtn;
        VisualElement[] _toolRows;
        Label[] _toolNames;
        System.Collections.Generic.List<VisualElement> _blockers;

        bool _built, _stroking, _blockersLive = true, _canHand;
        float _restoreIn, _pulseIn;
        bool _pulseLit;
        float _fpsAcc, _fpsSmoothed = 60f;
        int _fpsFrames;

        void Awake()
        {
            if (sim == null) sim = UnityEngine.Object.FindFirstObjectByType<CarpetSimulation>();
            if (tools == null) tools = UnityEngine.Object.FindFirstObjectByType<ToolController>();
            if (handover == null) handover = UnityEngine.Object.FindFirstObjectByType<CarpetHandover>();
            if (highlight == null) highlight = GetComponent<SurfaceHighlight>();
            _doc = GetComponent<UIDocument>();
            _toast = null;
        }

        void OnEnable()
        {
            if (sim != null) sim.StatsUpdated += OnStats;
            if (tools != null) tools.ToolChanged += OnToolChanged;
            if (handover != null) handover.StateChanged += OnHandoverState;
            if (highlight != null) highlight.Changed += RefreshLampChips;
            Loc.Changed += Relocalize;
        }

        void OnDisable()
        {
            if (sim != null) sim.StatsUpdated -= OnStats;
            if (tools != null) tools.ToolChanged -= OnToolChanged;
            if (handover != null) handover.StateChanged -= OnHandoverState;
            if (highlight != null) highlight.Changed -= RefreshLampChips;
            Loc.Changed -= Relocalize;
            if (tools != null && tools.PointerOverUI == PointerOverHud) tools.PointerOverUI = null;
        }

        // UIDocument builds its tree in its own OnEnable, so the query has to wait for Start.
        void Start()
        {
            Build();
            RefreshStrips();
            RefreshBars();
            RefreshHandover();
            RefreshLampChips();
        }

        // ------------------------------------------------------------------ construction
        void Build()
        {
            if (_built) return;
            if (_doc == null) _doc = GetComponent<UIDocument>();
            _root = _doc != null ? _doc.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogError("[CleaningHUD] UIDocument has no root: is panelSettings / sourceAsset assigned? (CarpetCleaning/Build Scene)");
                return;
            }
            _root.pickingMode = PickingMode.Ignore;
            // The instantiated template does not stretch on its own; USS covers this too, but a
            // missing stylesheet must not collapse the whole layout into the top-left corner.
            foreach (var child in _root.Children())
            {
                child.style.position = Position.Absolute;
                child.style.left = 0; child.style.top = 0; child.style.right = 0; child.style.bottom = 0;
                // ...and it must not pick: a full-screen container that answers panel.Pick() would make
                // EventSystem.IsPointerOverGameObject() true over the whole screen and kill every stamp.
                child.pickingMode = PickingMode.Ignore;
            }

            _chrome = _root.Q<VisualElement>("chrome");
            _bars = _root.Q<VisualElement>("bars");
            _hintRoot = _root.Q<VisualElement>("hint");
            _hintLabel = _root.Q<Label>("hint-label");
            _fpsLabel = _root.Q<Label>("fps-label");
            _cleanFill = _root.Q<VisualElement>("clean-fill");
            _dryFill = _root.Q<VisualElement>("dry-fill");
            _cleanValue = _root.Q<Label>("clean-value");
            _dryValue = _root.Q<Label>("dry-value");
            _handWrap = _root.Q<VisualElement>("hand-over");
            _takeWrap = _root.Q<VisualElement>("take-new");
            _handBtn = _root.Q<Button>("hand-over-button");
            _dirtChip = _root.Q<Label>("hl-dirt-chip");
            _wetChip = _root.Q<Label>("hl-wet-chip");

#if UNITY_EDITOR
            var reset = _root.Q<Button>("reset-button");
            // Reset = a brand-new carpet with new dirt, like R (Docs/DIRT_LEVEL.md)
            if (reset != null) reset.clicked += () =>
            {
                if (handover != null) handover.ResetToNewCarpet();
                else if (sim != null) sim.ResetCarpet(sim.Seed + 1);
            };
#else
            // Remove its wrapper too, so player builds have no empty slot or shadow.
            _root.Q<VisualElement>("reset-wrap")?.RemoveFromHierarchy();
#endif
            if (_handBtn != null) _handBtn.clicked += () => { if (handover != null) handover.Begin(); };
            var take = _root.Q<Button>("take-new-button");
            if (take != null) take.clicked += () => { if (handover != null) handover.TakeNewCarpet(); };

            BuildToolRows();

            // every widget allowed to swallow a click carries .ui-blocking in the UXML / in BuildToolRows
            _blockers = _root.Query<VisualElement>(null, "ui-blocking").ToList();
            if (_handWrap != null) _handWrap.style.display = DisplayStyle.None;
            if (_takeWrap != null) _takeWrap.style.display = DisplayStyle.None;

            // UI Toolkit panels are invisible to EventSystem.IsPointerOverGameObject(), so the HUD
            // hands ToolController its own hit test (Docs/HUD.md 3).
            if (tools != null) tools.PointerOverUI = PointerOverHud;
            _built = true;
        }

        void BuildToolRows()
        {
            _toolRows = new VisualElement[ToolTable.All.Length];
            _toolNames = new Label[ToolTable.All.Length];
            var carpetList = _root.Q<VisualElement>("carpet-tools");
            var floorList = _root.Q<VisualElement>("floor-tools");
            for (int i = 0; i < ToolTable.All.Length; i++)
            {
                var def = ToolTable.All[i];
                var list = def.floorTool ? floorList : carpetList;
                if (list == null) continue;

                var row = new Button();
                row.AddToClassList("tool");
                row.AddToClassList("ui-blocking");
                if (def.HasHotkey)
                {
                    var key = new Label(def.HotkeyLabel);   // "1".."9", "0" (the mop, slot 10)
                    key.AddToClassList("kbd");
                    key.pickingMode = PickingMode.Ignore;
                    row.Add(key);
                }
                else row.AddToClassList("tool--plain");   // mouse-only tool: the name centres instead
                var name = new Label(Loc.T(def.name));   // ToolTable names are keys of Resources/Loc (Docs/LOCALIZATION.md)
                name.AddToClassList("tool__name");
                name.pickingMode = PickingMode.Ignore;
                row.Add(name);
                _toolNames[i] = name;

                var captured = def.id;
                row.clicked += () => { if (tools != null) tools.SetTool(captured); };
                list.Add(row);
                _toolRows[i] = row;
            }
        }

        // ------------------------------------------------------------------ input blocking
        /// <summary>
        /// "Is the pointer over a HUD widget that must swallow the click?" Only elements carrying
        /// .ui-blocking count, so the meters (and anything else parented into #bars) can never eat a
        /// stamp - there can be debris under them. Returns false the moment the chrome goes away.
        /// </summary>
        bool PointerOverHud(Vector2 screenPos)
        {
            // A locked first-person mouse aims the tool. Peripheral HUD badges are keyboard hints, not hit targets.
            if (tools != null && tools.FirstPersonActive && !tools.UiModal) return false;
            if (!_built || !_blockersLive || _root == null) return false;
            var panel = _root.panel;
            if (panel == null) return false;
            var p = RuntimePanelUtils.ScreenToPanel(panel, screenPos);
            for (var e = panel.Pick(p); e != null; e = e.parent)
                if (e.ClassListContains("ui-blocking")) return true;
            return false;
        }

        void SetBlockersLive(bool live)
        {
            if (_blockersLive == live || _blockers == null) return;
            _blockersLive = live;
            var mode = live ? PickingMode.Position : PickingMode.Ignore;
            for (int i = 0; i < _blockers.Count; i++)
                if (_blockers[i] != null) _blockers[i].pickingMode = mode;
        }

        /// <summary>
        /// Chrome out while the tool is working, back in on release. Picking drops on the FIRST
        /// stroking frame (not when the fade finishes), otherwise the fade would still swallow
        /// stamps at the screen edges - which is the bug this whole mechanism exists to fix.
        /// </summary>
        void UpdateWorkingFade()
        {
            bool stroking = tools != null && tools.IsStroking;
            if (stroking != _stroking)
            {
                _stroking = stroking;
                if (_chrome != null) _chrome.EnableInClassList("layer--hidden", stroking);
                if (_bars != null) _bars.EnableInClassList("layer--dim", stroking);
                if (stroking) { SetBlockersLive(false); _restoreIn = 0f; }
                else _restoreIn = RestoreDelay;
            }
            if (_restoreIn > 0f)
            {
                _restoreIn -= Time.unscaledDeltaTime;
                if (_restoreIn <= 0f) SetBlockersLive(true);
            }
        }

        // ------------------------------------------------------------------ updates
        void OnToolChanged(ToolId t) { RefreshStrips(); }

        /// <summary>Loc.Changed: the tool names and the hint in the new language (the static labels are HudLocalizer's).</summary>
        void Relocalize()
        {
            if (!_built) return;
            if (_toolNames != null)
                for (int i = 0; i < _toolNames.Length; i++)
                    if (_toolNames[i] != null) _toolNames[i].text = Loc.T(ToolTable.All[i].name);
            RefreshBars();   // the percentages and the hint
            SetHint(HintText());
        }

        void OnHandoverState(HandoverState s)
        {
            RefreshStrips();
            RefreshHandover();
            RefreshBars();
        }

        void OnStats(SimStats s)
        {
            RefreshBars();
            RefreshHandover();
        }

        /// <summary>Active-tool highlight. Every tool is usable in every state: while the carpet is
        /// away the carpet tools work on the bare tile it leaves behind (Docs/HANDOVER.md 6), so
        /// nothing is ever greyed out.</summary>
        void RefreshStrips()
        {
            if (!_built || _toolRows == null) return;
            int cur = tools != null ? (int)tools.Current : -1;
            for (int i = 0; i < _toolRows.Length; i++)
                if (_toolRows[i] != null) _toolRows[i].EnableInClassList("is-active", i == cur);
        }

        void RefreshHandover()
        {
            if (!_built) return;
            _canHand = handover != null && handover.CanHandOver;
            bool canTake = handover != null && handover.CanTakeNew;
            if (_handWrap != null) _handWrap.style.display = _canHand ? DisplayStyle.Flex : DisplayStyle.None;
            if (_takeWrap != null) _takeWrap.style.display = canTake ? DisplayStyle.Flex : DisplayStyle.None;
            if (!_canHand) { _pulseLit = false; _pulseIn = 0f; if (_handBtn != null) _handBtn.RemoveFromClassList("is-lit"); }
        }

        void RefreshBars()
        {
            if (!_built || sim == null) return;
            float clean = Mathf.Clamp01(sim.Cleanliness), dry = Mathf.Clamp01(1f - sim.Wetness);
            if (_cleanFill != null) _cleanFill.style.width = Length.Percent(clean * 100f);
            if (_dryFill != null) _dryFill.style.width = Length.Percent(dry * 100f);
            // CarpetSimulation.ShownPercent: the same rounding decides when the inspection lamp goes dark (Docs/HIGHLIGHTS.md 4)
            if (_cleanValue != null) _cleanValue.text = Loc.F("{0}%", CarpetSimulation.ShownPercent(clean));   // "85 %" in French, "%85" in Turkish
            if (_dryValue != null) _dryValue.text = Loc.F("{0}%", CarpetSimulation.ShownPercent(dry));
            SetHint(HintText());
        }

        /// <summary>Z / X chips beside CLEAN / DRY: shown once the item is owned, lit while its lamp is on.</summary>
        void RefreshLampChips()
        {
            if (!_built) return;
            LampChip(_dirtChip, ShopItem.DirtHighlight);
            LampChip(_wetChip, ShopItem.MoistureHighlight);
        }

        void LampChip(Label chip, ShopItem item)
        {
            if (chip == null) return;
            bool owned = highlight != null && highlight.Owned(item);
            var want = owned ? DisplayStyle.Flex : DisplayStyle.None;
            if (chip.style.display != want) chip.style.display = want;
            chip.EnableInClassList("is-on", owned && highlight.IsOn(item));
        }

        /// <summary>A short message in the hint pill that wins over the sim's hint for `seconds` (unscaled). The caller passes
        /// it already translated (Loc.F).</summary>
        public void Toast(string text, float seconds)
        {
            _toast = text;
            _toastUntil = Time.unscaledTime + Mathf.Max(seconds, 0f);
            SetHint(HintText());
        }

        void SetHint(string text)
        {
            if (_hintLabel == null) return;
            if (_hintLabel.text != text) _hintLabel.text = text;
            if (_hintRoot != null)
            {
                var want = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
                if (_hintRoot.style.display != want) _hintRoot.style.display = want;
            }
        }

        string HintText()
        {
            if (_toast != null && Time.unscaledTime < _toastUntil) return _toast;
            if (handover != null)
            {
                switch (handover.State)
                {
                    case HandoverState.FlyOut: return Loc.T("Handed over to the client!");
                    case HandoverState.Away: return Loc.T("Carpet away - clean the bare tile, or take a new one");
                    case HandoverState.FlyIn: return Loc.T("Fresh carpet coming in...");
                    default: break;
                }
                if (handover.CanHandOver) return Loc.T("Sparkling clean - hand it over!");
            }
            if (sim == null) return "";
            // sim.Hint stays English (tests and the recipes read it); it is a key, translated here
            return sim.IsWon ? Loc.T("Sparkling clean!") : Loc.T(sim.Hint);
        }

        void Update()
        {
            if (!_built) { Build(); if (!_built) return; }
            UpdateWorkingFade();
            if (_toast != null && Time.unscaledTime >= _toastUntil) { _toast = null; SetHint(HintText()); }

            _fpsAcc += Time.unscaledDeltaTime; _fpsFrames++;
            if (_fpsAcc >= 0.5f)
            {
                _fpsSmoothed = _fpsFrames / _fpsAcc; _fpsAcc = 0f; _fpsFrames = 0;
                if (_fpsLabel != null) _fpsLabel.text = Mathf.RoundToInt(_fpsSmoothed) + " fps";
            }

            // A slow breathe on the reward button: two classes and the USS transition between them,
            // so even the pulse colour stays in the stylesheet. The flight itself is the celebration.
            if (_canHand && _handBtn != null)
            {
                _pulseIn -= Time.unscaledDeltaTime;
                if (_pulseIn <= 0f)
                {
                    _pulseIn = PulseSeconds;
                    _pulseLit = !_pulseLit;
                    _handBtn.EnableInClassList("is-lit", _pulseLit);
                }
            }
        }
    }
}
