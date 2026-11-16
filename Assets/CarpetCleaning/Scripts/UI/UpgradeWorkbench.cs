using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CarpetCleaning
{
    /// <summary>The physical entrance to the upgrade shop. A clear centre-screen ray must reach this table.</summary>
    [DefaultExecutionOrder(40)]
    public sealed class UpgradeWorkbench : MonoBehaviour
    {
        public Camera viewCamera;
        public EconomyHud hud;
        public ToolController tools;
        public Collider interactionCollider;
        [Min(0.25f)] public float interactionDistance = 2.1f;

        public bool Focused { get; private set; }
        public bool CanInteract => isActiveAndEnabled && tools != null && tools.FirstPersonActive &&
            tools.player.CanControl && !tools.UiModal && !tools.Suspended && hud != null && !hud.ShopOpen &&
            IsAimedAt(viewCamera != null ? viewCamera : Camera.main);

        void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (hud == null) hud = FindAnyObjectByType<EconomyHud>();
            if (tools == null) tools = FindAnyObjectByType<ToolController>();
            if (interactionCollider == null) interactionCollider = GetComponentInChildren<Collider>();
            Focused = false;
        }

        /// <summary>Geometry-only check, also usable by unfocused-editor verification.</summary>
        public bool IsAimedAt(Camera camera)
        {
            if (camera == null || interactionCollider == null || !interactionCollider.enabled) return false;
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out var hit, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
            return hit.collider == interactionCollider || hit.collider.transform.IsChildOf(transform);
        }

        public bool TryInteract()
        {
            if (!CanInteract) return false;
            return hud.OpenFromWorkbench(this);
        }

        void Update()
        {
            Focused = CanInteract;
            if (Focused && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryInteract();
                Focused = false;
            }
        }

        void OnDisable() { Focused = false; }

        public string DebugJson()
        {
            return "{\"focused\":" + (Focused ? "true" : "false") +
                ",\"aimed\":" + (IsAimedAt(viewCamera != null ? viewCamera : Camera.main) ? "true" : "false") +
                ",\"canInteract\":" + (CanInteract ? "true" : "false") +
                ",\"reach\":" + interactionDistance.ToString("0.###", CultureInfo.InvariantCulture) + "}";
        }
    }
}
