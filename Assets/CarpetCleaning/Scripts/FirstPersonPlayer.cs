using UnityEngine;
using UnityEngine.InputSystem;

namespace CarpetCleaning
{
    /// <summary>
    /// The player's body in the cleaning workshop. Movement runs before ToolController, so the
    /// centre-screen contact and the held tool use this frame's camera pose. The simulation still
    /// receives ordinary ToolStamps; this component only changes how the player reaches them.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonPlayer : MonoBehaviour
    {
        public Camera viewCamera;
        public ToolController tools;
        public CarpetSimulation sim;

        [Header("Movement (metres / seconds)")]
        public float walkSpeed = 1.65f;
        public float fastWalkSpeed = 2.5f;
        public float eyeHeight = 1.62f;
        public float floorY = -0.003f;
        public Rect movementBounds = new Rect(-2.95f, -2.95f, 5.9f, 5.9f);

        [Header("View")]
        [Tooltip("Degrees per mouse pixel. Deliberately independent of delta time.")]
        public float mouseSensitivity = 0.085f;
        public float initialPitch = 48f;
        public float minPitch = -80f;
        public float maxPitch = 85f;
        public const float DefaultToolReach = 2f;
        [Tooltip("Maximum camera-ray distance for contact tools and short-range sprays. The hose has unlimited reach.")]
        public float toolReach = DefaultToolReach;

        CharacterController _body;
        float _yaw, _pitch, _verticalSpeed;
        bool _focused, _waitForButtonsUp, _wasBlocked, _ownsCursor;

        public bool Active => isActiveAndEnabled && viewCamera != null;
        /// <summary>Modal ownership is independent of hand-over InputEnabled: walking remains possible during a carpet swap.</summary>
        public bool CanControl => Active && _focused && !_waitForButtonsUp && !Blocked && Cursor.lockState == CursorLockMode.Locked;
        public Ray InteractionRay => viewCamera != null
            ? viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(transform.position + Vector3.up * eyeHeight, transform.forward);
        public float Yaw => _yaw;
        public float Pitch => _pitch;
        public Vector3 MovementVelocity { get; private set; }

        bool Blocked => (tools != null && (tools.UiModal || tools.Suspended || tools.ScriptedPointer))
                        || (sim != null && sim.IsPaused);

        void Awake()
        {
            _body = GetComponent<CharacterController>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            if (tools == null) tools = UnityEngine.Object.FindAnyObjectByType<ToolController>();
            if (sim == null) sim = UnityEngine.Object.FindAnyObjectByType<CarpetSimulation>();
            if (tools != null) tools.player = this;
            _yaw = transform.eulerAngles.y;
            _pitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
            _verticalSpeed = 0f;
            _focused = Application.isFocused;
            _waitForButtonsUp = false;
            _wasBlocked = false;
            _ownsCursor = false;
            MovementVelocity = Vector3.zero;
            ApplyView();
        }

        void OnEnable()
        {
            _focused = Application.isFocused;
            _waitForButtonsUp = ButtonsHeld();
        }

        void OnDisable()
        {
            MovementVelocity = Vector3.zero;
            ReleaseCursor();
        }

        void OnApplicationFocus(bool focused)
        {
            _focused = focused;
            _waitForButtonsUp = true;
            if (!focused) ReleaseCursor();
        }

        void Update()
        {
            SyncCursor();
            if (!CanControl)
            {
                MovementVelocity = Vector3.zero;
                return;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _yaw = Mathf.Repeat(_yaw + delta.x * mouseSensitivity, 360f);
                _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, minPitch, maxPitch);
            }
            ApplyView();

            Vector2 move = Vector2.zero;
            var kb = Keyboard.current;
            bool fast = false;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                fast = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            }
            StepMovement(move, Mathf.Min(Time.unscaledDeltaTime, 0.05f), fast);
        }

        // PauseMenu and EconomyHud can claim the cursor later in Update. Release it in this same
        // frame, before rendering their modal, rather than waiting for the next player Update.
        void LateUpdate() { SyncCursor(); }

        void SyncCursor()
        {
            bool blocked = !Active || !_focused || Blocked;
            if (blocked)
            {
                _wasBlocked = true;
                ReleaseCursor();
                return;
            }
            if (_wasBlocked)
            {
                _wasBlocked = false;
                _waitForButtonsUp = true;
            }
            if (_waitForButtonsUp)
            {
                if (ButtonsHeld()) return;
                _waitForButtonsUp = false;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _ownsCursor = true;
        }

        void ReleaseCursor()
        {
            if (!_ownsCursor) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _ownsCursor = false;
        }

        static bool ButtonsHeld()
        {
            var mouse = Mouse.current;
            return mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed);
        }

        void ApplyView()
        {
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (viewCamera == null) return;
            viewCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
            viewCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        /// <summary>Deterministic movement hook for CLI collision checks; real input calls the same path.
        /// Ignores OS focus for testing, but always respects gameplay modals and overlay pause.</summary>
        public void StepMovement(Vector2 input, float dt, bool fast = false)
        {
            if (!Active || Blocked)
            {
                MovementVelocity = Vector3.zero;
                return;
            }
            if (_body == null) _body = GetComponent<CharacterController>();
            if (_body == null || !_body.enabled || dt <= 0f) return;
            input = Vector2.ClampMagnitude(input, 1f);
            float speed = Mathf.Max(0f, fast ? fastWalkSpeed : walkSpeed);
            Vector3 horizontal = (transform.right * input.x + transform.forward * input.y) * speed;
            Vector3 before = transform.position;
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / 0.04f), 1, 300);
            float stepDt = Mathf.Min(dt, 12f) / steps;
            for (int i = 0; i < steps; i++)
            {
                if (_body.isGrounded && _verticalSpeed < 0f) _verticalSpeed = -1f;
                else _verticalSpeed = Mathf.Max(_verticalSpeed - 14f * stepDt, -10f);
                Vector3 displacement = (horizontal + Vector3.up * _verticalSpeed) * stepDt;
                // Clamp the intended move before CharacterController resolves table/wall collision.
                Vector3 p = transform.position;
                displacement.x = Mathf.Clamp(p.x + displacement.x, movementBounds.xMin, movementBounds.xMax) - p.x;
                displacement.z = Mathf.Clamp(p.z + displacement.z, movementBounds.yMin, movementBounds.yMax) - p.z;
                if (p.y + displacement.y < floorY) displacement.y = floorY - p.y;
                _body.Move(displacement);
                if (transform.position.y <= floorY + 0.002f) _verticalSpeed = -1f;
            }
            MovementVelocity = (transform.position - before) / dt;
        }

        /// <summary>Place the camera deterministically without synthesizing keyboard or mouse input.</summary>
        public void DebugPose(float x, float z, float yaw, float pitch)
        {
            if (_body == null) _body = GetComponent<CharacterController>();
            bool wasEnabled = _body != null && _body.enabled;
            if (wasEnabled) _body.enabled = false;
            transform.position = new Vector3(Mathf.Clamp(x, movementBounds.xMin, movementBounds.xMax), floorY,
                Mathf.Clamp(z, movementBounds.yMin, movementBounds.yMax));
            _yaw = Mathf.Repeat(yaw, 360f);
            _pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            _verticalSpeed = 0f;
            MovementVelocity = Vector3.zero;
            ApplyView();
            if (wasEnabled) _body.enabled = true;
            Physics.SyncTransforms();
            if (tools != null) tools.ResetPointerContinuity();
        }

        [System.Serializable]
        struct Diagnostics
        {
            public bool active, canControl, focused, modal, suspended, cursorLocked, hasAim;
            public Vector3 position, cameraPosition, aim;
            public float yaw, pitch, reach;
        }

        public string DiagnosticsJson()
        {
            return JsonUtility.ToJson(new Diagnostics
            {
                active = Active, canControl = CanControl, focused = _focused,
                modal = tools != null && tools.UiModal, suspended = tools != null && tools.Suspended,
                cursorLocked = Cursor.lockState == CursorLockMode.Locked,
                hasAim = tools != null && tools.HasSurfaceAim,
                position = transform.position, cameraPosition = viewCamera != null ? viewCamera.transform.position : Vector3.zero,
                aim = tools != null ? tools.SurfaceAimWorld : Vector3.zero, yaw = _yaw, pitch = _pitch, reach = toolReach
            });
        }
    }
}
