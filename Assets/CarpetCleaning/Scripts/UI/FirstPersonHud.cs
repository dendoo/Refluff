using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CarpetCleaning
{
    /// <summary>Non-interactive centre reticle, held-tool belt and physical-table prompt.</summary>
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(235)]
    public sealed class FirstPersonHud : MonoBehaviour
    {
        public ToolController tools;
        public UpgradeWorkbench workbench;

        VisualElement _root, _layer, _reticle, _prompt, _belt;
        Label _toolName, _toolKey, _interactKey, _moveKeys;
        VisualElement[] _slots;
        System.Collections.Generic.List<VisualElement> _chromeBlockers;
        bool _built;
        bool _pickingSuppressed;
        int _lastTool = -1;

        void Awake()
        {
            if (tools == null) tools = FindAnyObjectByType<ToolController>();
            if (workbench == null) workbench = FindAnyObjectByType<UpgradeWorkbench>();
        }

        void OnEnable() { Loc.Changed += Relocalize; }
        void OnDisable()
        {
            Loc.Changed -= Relocalize;
            SuppressChromePicking(false);
            if (_layer != null) _layer.style.display = DisplayStyle.None;
            _root?.RemoveFromClassList("first-person");
        }

        void Start() { Build(); }

        void Build()
        {
            if (_built) return;
            var tree = GetComponent<UIDocument>().rootVisualElement;
            _root = tree?.Q<VisualElement>("hud");
            _layer = tree?.Q<VisualElement>("fp-overlay");
            if (_root == null || _layer == null) return;
            _reticle = tree.Q<VisualElement>("fp-reticle");
            _prompt = tree.Q<VisualElement>("fp-workbench");
            _belt = tree.Q<VisualElement>("fp-belt");
            _toolName = tree.Q<Label>("fp-tool-name");
            _toolKey = tree.Q<Label>("fp-tool-key");
            _interactKey = tree.Q<Label>("fp-interact-key");
            _moveKeys = tree.Q<Label>("fp-move-keys");
            _chromeBlockers = tree.Q<VisualElement>("chrome")?.Query<VisualElement>(null, "ui-blocking").ToList();
            var slots = tree.Q<VisualElement>("fp-slots");
            _slots = new VisualElement[ToolTable.All.Length];
            if (slots != null)
                for (int i = 0; i < _slots.Length; i++)
                {
                    var slot = new Label(SlotKey(i)) { pickingMode = PickingMode.Ignore, languageDirection = LanguageDirection.LTR };
                    slot.AddToClassList("fp-slot");
                    slots.Add(slot);
                    _slots[i] = slot;
                }
            _built = true;
            if (_toolKey != null) _toolKey.languageDirection = LanguageDirection.LTR;
            if (_interactKey != null) _interactKey.languageDirection = LanguageDirection.LTR;
            if (_moveKeys != null) _moveKeys.languageDirection = LanguageDirection.LTR;
            var toolKeys = tree.Q<Label>("fp-tool-keys");
            if (toolKeys != null) toolKeys.languageDirection = LanguageDirection.LTR;
            Relocalize();
        }

        // Floor tools have no number in the old pointer-mode ToolTable; first-person input maps every slot.
        static string SlotKey(int index) => index == 9 ? "0" : (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

        void Relocalize()
        {
            _lastTool = -1;
            if (!_built) return;
            RefreshTool();
            RefreshKeys();
        }

        void RefreshKeys()
        {
            if (_interactKey != null) _interactKey.text = KeyNames.Label(Key.E);
            if (_moveKeys != null)
                _moveKeys.text = KeyNames.Label(Key.W) + KeyNames.Label(Key.A) + KeyNames.Label(Key.S) + KeyNames.Label(Key.D);
        }

        void RefreshTool()
        {
            int selected = tools != null ? (int)tools.Current : 0;
            if (selected == _lastTool || selected < 0 || selected >= ToolTable.All.Length) return;
            _lastTool = selected;
            var def = ToolTable.All[selected];
            if (_toolName != null) _toolName.text = Loc.T(def.name);
            if (_toolKey != null) _toolKey.text = SlotKey(selected);
            if (_slots != null)
                for (int i = 0; i < _slots.Length; i++)
                {
                    _slots[i]?.EnableInClassList("is-active", i == selected);
                    if (_slots[i] != null) _slots[i].tooltip = Loc.T(ToolTable.All[i].name);
                }
        }

        void Update()
        {
            if (!_built) { Build(); if (!_built) return; }
            bool firstPerson = tools != null && tools.FirstPersonActive;
            _root.EnableInClassList("first-person", firstPerson);
            bool modal = tools != null && (tools.UiModal || tools.Suspended);
            // H / N / Esc remain readable keyboard prompts. A captured mouse must not also
            // activate an ordinary HUD button while the same click works the cleaning tool.
            // Modal controls live outside #chrome and always keep their normal input behavior.
            SuppressChromePicking(firstPerson && !modal && UnityEngine.Cursor.lockState == CursorLockMode.Locked);
            _layer.style.display = firstPerson && !modal ? DisplayStyle.Flex : DisplayStyle.None;
            if (!firstPerson || modal) return;
            bool focused = workbench != null && workbench.CanInteract;
            _prompt.style.display = focused ? DisplayStyle.Flex : DisplayStyle.None;
            _reticle.EnableInClassList("is-interactive", focused);
            _reticle.EnableInClassList("is-working", tools.IsStroking);
            _belt.EnableInClassList("is-working", tools.IsStroking);
            RefreshTool();
            RefreshKeys();
        }

        void SuppressChromePicking(bool suppress)
        {
            // CleaningHUD may restore .ui-blocking after a working fade, so reapply while locked.
            // When returning to the legacy pointer mode, restore once and let its fade own picking.
            if (_chromeBlockers != null && (suppress || _pickingSuppressed))
                foreach (var element in _chromeBlockers)
                    if (element != null) element.pickingMode = suppress ? PickingMode.Ignore : PickingMode.Position;
            _pickingSuppressed = suppress;
        }
    }
}
