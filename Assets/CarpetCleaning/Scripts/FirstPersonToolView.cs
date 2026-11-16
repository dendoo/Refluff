using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    /// <summary>
    /// A visible grip and shaft connect the real contact head to the first-person camera. Sprayers and blowers
    /// stay in the player's grip and point at the same surface aim used by ToolController. No hands are needed.
    /// Runtime geometry is limited to mechanical connectors; the working tools use the generated model assets.
    /// </summary>
    [DefaultExecutionOrder(207)]
    public sealed class FirstPersonToolView : MonoBehaviour
    {
        public Camera viewCamera;
        public ToolController tools;
        public ToolModelView surfaceTools;
        public FirstPersonToolSet generatedTools;
        public Vector3 gripPosition = new Vector3(0.23f, -0.31f, 0.43f);

        readonly Dictionary<ToolId, Transform> _held = new Dictionary<ToolId, Transform>();
        readonly List<Material> _materials = new List<Material>();
        Transform _runtime, _shaft, _sleeve, _grip, _coupling, _vacuumFoot;
        LineRenderer _hose, _spray;
        ToolId _shownTool;
        Transform _shown;
        bool _built;

        public bool Visible { get { return _runtime != null && _runtime.gameObject.activeSelf; } }
        public bool HasGenerated(ToolId id) { return generatedTools != null && generatedTools.TryGet(id, out _); }

        public static bool IsHandheld(ToolId id)
        {
            return id == ToolId.Hose || id == ToolId.FoamSprayer || id == ToolId.FloorCleaner ||
                   id == ToolId.Dryer || id == ToolId.DryVacuum;
        }

        void Awake()
        {
            // Domain/scene reload is disabled. A previous session must not leave live material/mesh references.
            _held.Clear(); _materials.Clear(); _built = false; _shown = null;
            if (viewCamera == null) viewCamera = GetComponent<Camera>();
            if (viewCamera == null) viewCamera = Camera.main;
            if (tools == null) tools = FindFirstObjectByType<ToolController>();
            if (surfaceTools == null) surfaceTools = FindFirstObjectByType<ToolModelView>();
            var old = transform.Find("FirstPersonTools_Runtime");
            if (old != null) Destroy(old.gameObject);
        }

        void OnDisable()
        {
            if (_runtime != null) _runtime.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            for (int i = 0; i < _materials.Count; i++) if (_materials[i] != null) Destroy(_materials[i]);
            if (_runtime != null) Destroy(_runtime.gameObject);
        }

        void EnsureBuilt()
        {
            if (_built || !Application.isPlaying || viewCamera == null) return;
            _built = true;
            _runtime = new GameObject("FirstPersonTools_Runtime").transform;
            _runtime.SetParent(transform, false);
            _runtime.gameObject.hideFlags = HideFlags.DontSave;
            var metal = Material("Tool shaft • satin aluminium", new Color(0.46f, 0.51f, 0.55f), 0.76f, 0.55f);
            var rubber = Material("Tool grip • graphite", new Color(0.035f, 0.045f, 0.052f), 0f, 0.24f);
            var accent = Material("Tool collar • teal", new Color(0.035f, 0.31f, 0.33f), 0.25f, 0.43f);
            _shaft = Cylinder("Telescopic aluminium shaft", metal);
            _sleeve = Cylinder("Handle sleeve", accent);
            _grip = Cylinder("Rubber grip", rubber);
            _coupling = Cylinder("Swivel collar", rubber);
            _hose = Line("Flexible hose", rubber, 0.018f, 13);
            var sprayMat = Material("Tool spray", new Color(0.72f, 0.91f, 0.98f, 0.4f), 0f, 0.7f, true);
            var sprayShader = generatedTools != null && generatedTools.sprayMaterial != null
                ? generatedTools.sprayMaterial.shader : Shader.Find("CarpetCleaning/FirstPersonSpray");
            if (sprayShader != null) sprayMat.shader = sprayShader;
            _spray = Line("Visible spray", sprayMat, 0.006f, 9);
            _spray.widthMultiplier = 1f;
            _spray.numCapVertices = 2;
            _spray.enabled = false;

            foreach (var def in ToolTable.All)
            {
                GameObject prefab = null;
                if (generatedTools != null && generatedTools.TryGet(def.id, out var full)) prefab = full.prefab;
                else if (surfaceTools != null && surfaceTools.set != null && surfaceTools.set.TryGet(def.id, out var head)) prefab = head.prefab;
                if (prefab == null) continue;
                var clone = Instantiate(prefab, _runtime);
                clone.name = "Held_" + def.id;
                Strip(clone);
                clone.SetActive(false);
                _held[def.id] = clone.transform;
            }
            // Reuse the existing generated extraction head as the dry vacuum's suction foot. The new Tripo model is
            // its complete motor/grip body; a physical tube joins it to this foot instead of vacuuming at a distance.
            if (surfaceTools != null && surfaceTools.set != null && surfaceTools.set.TryGet(ToolId.WetVacuum, out var suction))
            {
                var foot = Instantiate(suction.prefab, _runtime);
                foot.name = "DryVacuum_SuctionFoot"; Strip(foot); foot.SetActive(false);
                _vacuumFoot = foot.transform;
            }
        }

        void LateUpdate()
        {
            bool active = tools != null && tools.FirstPersonActive && !tools.ScriptedPointer && viewCamera != null;
            if (!active) { if (_runtime != null) _runtime.gameObject.SetActive(false); return; }
            EnsureBuilt();
            if (_runtime == null) return;
            _runtime.gameObject.SetActive(!tools.UiModal && !tools.Suspended);
            if (!_runtime.gameObject.activeSelf) return;

            ToolId id = tools.Current;
            if (_shown != null && _shownTool != id) { _shown.gameObject.SetActive(false); _shown = null; }
            _shownTool = id;
            bool handheld = IsHandheld(id);
            bool contact = tools.HasSurfaceAim && !handheld && surfaceTools != null && surfaceTools.ShownTransform != null;
            Vector3 grip = viewCamera.transform.TransformPoint(gripPosition);
            Vector3 aim = tools.HasSurfaceAim ? tools.SurfaceAimWorld : viewCamera.transform.position + viewCamera.transform.forward * 2f;
            Vector3 surfaceTarget = tools.HasSurfaceAim ? aim - Vector3.up * 0.012f : aim;
            float suctionScale = ToolTuning.Effective(ToolId.DryVacuum).radiusMm * 2f / 160f;
            Vector3 groundAxis = tools.SurfaceAimDirection;
            Quaternion suctionYaw = Quaternion.Euler(0f, -Mathf.Atan2(groundAxis.z, groundAxis.x) * Mathf.Rad2Deg, 0f);
            // The vacuum tube terminates at its floor foot's upper socket, while a spray terminates at the surface.
            Vector3 barrelTarget = surfaceTarget + (id == ToolId.DryVacuum && tools.HasSurfaceAim
                ? suctionYaw * (ContactSocket(_vacuumFoot, ToolId.WetVacuum) * suctionScale) : Vector3.zero);
            Vector3 direction = (barrelTarget - grip).normalized;
            if (direction.sqrMagnitude < 0.5f) direction = viewCamera.transform.forward;
            Quaternion facing = Quaternion.LookRotation(direction, viewCamera.transform.up);
            Vector3 nozzle = grip + direction * 0.18f;

            if (_held.TryGetValue(id, out var held))
            {
                bool showHeld = handheld || !contact;
                held.gameObject.SetActive(showHeld);
                _shown = showHeld ? held : null;
                if (showHeld)
                {
                    if (generatedTools != null && generatedTools.TryGet(id, out var entry))
                    {
                        Vector3 offset = entry.cameraPosition.sqrMagnitude > 0.0001f ? entry.cameraPosition : gripPosition;
                        held.position = viewCamera.transform.TransformPoint(offset);
                        // Upgrade area affects the simulation/head, not a vacuum motor or a bottle's physical size.
                        held.localScale = Vector3.one * (entry.scale > 0f ? entry.scale : 1f);
                        held.rotation = Quaternion.LookRotation((barrelTarget - held.position).normalized, viewCamera.transform.up)
                                        * Quaternion.Euler(entry.eulerOffset);
                        // The nozzle is usually above the motor/bottle centre. Aim from its inspected marker, not
                        // the generic camera-side grip, so the jet and vacuum tube leave along the visible barrel.
                        for (int solve = 0; solve < 3; solve++)
                        {
                            Vector3 outlet = held.TransformPoint(entry.nozzlePosition);
                            Vector3 toSurface = barrelTarget - outlet;
                            if (toSurface.sqrMagnitude > 0.001f)
                                held.rotation = Quaternion.FromToRotation(held.forward, toSurface.normalized) * held.rotation;
                        }
                        nozzle = held.TransformPoint(entry.nozzlePosition);
                    }
                    else
                    {
                        // The generated hose/detergent heads' central outlet is +Y in their surface-up prefab.
                        held.position = handheld ? grip + direction * 0.10f : grip + direction * 0.60f;
                        held.rotation = handheld ? facing * Quaternion.Euler(90f, 0f, 0f) : facing * Quaternion.Euler(0f, 90f, 0f);
                        held.localScale = ToolTuning.HeadScale(id);
                        nozzle = held.position + direction * (id == ToolId.FloorCleaner ? 0.090f : 0.070f) * held.localScale.y;
                    }
                }
            }

            bool hasTool = contact || _shown != null;
            bool vacuumFoot = id == ToolId.DryVacuum && hasTool && tools.HasSurfaceAim && _vacuumFoot != null;
            if (_vacuumFoot != null)
            {
                _vacuumFoot.gameObject.SetActive(vacuumFoot);
                if (vacuumFoot)
                {
                    _vacuumFoot.position = surfaceTarget;
                    _vacuumFoot.rotation = suctionYaw;
                    _vacuumFoot.localScale = Vector3.one * suctionScale;
                }
            }
            bool shaft = hasTool && (!handheld || vacuumFoot);
            _shaft.gameObject.SetActive(shaft);
            _sleeve.gameObject.SetActive(shaft);
            _coupling.gameObject.SetActive(shaft);
            // Complete generated tools already include their grip; existing nozzle assets receive a small grip.
            bool gripNeeded = hasTool && !HasGenerated(id);
            _grip.gameObject.SetActive(gripNeeded);
            if (shaft)
            {
                var head = vacuumFoot ? _vacuumFoot : contact ? surfaceTools.ShownTransform : _shown;
                Vector3 socket = head.TransformPoint(ContactSocket(head, vacuumFoot ? ToolId.WetVacuum : id));
                Vector3 shaftStart = vacuumFoot ? nozzle : grip;
                // The regenerated collars have different bores. A generic 46mm cuff obscured the small brush
                // and scraper sockets; use the inspected physical tube diameter, independent of AREA upgrades.
                float shaftRadius = vacuumFoot ? 0.009f : ShaftRadius(id);
                Segment(_shaft, shaftStart, socket, shaftRadius);
                Vector3 axis = (socket - shaftStart).normalized;
                Segment(_sleeve, shaftStart + axis * 0.14f, shaftStart + axis * 0.27f, 0.019f);
                Segment(_coupling, socket, socket - axis * (id == ToolId.Brush ? 0.018f : 0.026f), shaftRadius + 0.002f);
                Segment(_grip, grip - axis * 0.10f, grip + axis * 0.12f, 0.021f);
            }
            else if (gripNeeded)
                Segment(_grip, grip + Vector3.down * 0.09f, grip + direction * 0.10f, 0.025f);

            bool hose = hasTool && (id == ToolId.Hose || id == ToolId.FloorCleaner || id == ToolId.WetVacuum);
            _hose.enabled = hose;
            if (hose)
            {
                Vector3 start = grip - direction * 0.06f;
                var inlet = _shown != null ? _shown.Find("HoseSocket") : null;
                if (inlet != null) start = inlet.position;
                Vector3 end = viewCamera.transform.TransformPoint(new Vector3(0.43f, -0.78f, 0.06f));
                for (int i = 0; i < _hose.positionCount; i++)
                {
                    float t = i / (float)(_hose.positionCount - 1);
                    _hose.SetPosition(i, Vector3.Lerp(start, end, t) + viewCamera.transform.right * (0.10f * Mathf.Sin(t * Mathf.PI)));
                }
            }
            UpdateSpray(id, nozzle, surfaceTarget, hasTool && tools.HasSurfaceAim && tools.IsStroking);
        }

        void UpdateSpray(ToolId id, Vector3 nozzle, Vector3 aim, bool working)
        {
            bool spray = working && (id == ToolId.Hose || id == ToolId.FoamSprayer || id == ToolId.FloorCleaner);
            _spray.enabled = spray;
            if (!spray) return;
            Color c = id == ToolId.FoamSprayer ? new Color(0.91f, 0.84f, 0.96f, 0.63f) :
                      id == ToolId.FloorCleaner ? new Color(0.61f, 0.92f, 0.81f, 0.45f) : new Color(0.70f, 0.90f, 1f, 0.48f);
            _spray.startColor = c;
            _spray.endColor = new Color(c.r, c.g, c.b, c.a * 0.24f);
            _spray.sharedMaterial.SetFloat("_Foam", id == ToolId.FoamSprayer ? 1f : 0f);
            _spray.startWidth = id == ToolId.Hose ? 0.006f : 0.010f;
            _spray.endWidth = ToolTuning.Effective(id).radiusMm * 0.001f * (id == ToolId.Hose ? 0.55f : 1.2f);
            for (int i = 0; i < _spray.positionCount; i++)
            {
                float t = i / (float)(_spray.positionCount - 1);
                _spray.SetPosition(i, Vector3.Lerp(nozzle, aim, t));
            }
        }

        Material Material(string label, Color color, float metallic, float smoothness, bool transparent = false)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = label;
            mat.hideFlags = HideFlags.DontSave | HideFlags.DontUnloadUnusedAsset;
            mat.SetColor("_BaseColor", color); mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", smoothness);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            _materials.Add(mat);
            return mat;
        }

        Transform Cylinder(string label, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = label; go.transform.SetParent(_runtime, false);
            var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Strip(go);
            return go.transform;
        }

        LineRenderer Line(string label, Material mat, float width, int points)
        {
            var go = new GameObject(label); go.transform.SetParent(_runtime, false); go.layer = 2;
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = mat; line.useWorldSpace = true; line.positionCount = points;
            line.widthMultiplier = width; line.numCornerVertices = 3; line.numCapVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            line.textureMode = LineTextureMode.Stretch;
            return line;
        }

        static void Segment(Transform segment, Vector3 from, Vector3 to, float radius)
        {
            Vector3 delta = to - from;
            segment.position = (from + to) * 0.5f;
            if (delta.sqrMagnitude > 1e-6f) segment.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            segment.localScale = new Vector3(radius * 2f, delta.magnitude * 0.5f, radius * 2f);
        }

        static Vector3 ContactSocket(Transform head, ToolId id)
        {
            var marker = head != null ? head.Find(ToolModelView.SocketNode) : null;
            if (marker != null) return marker.localPosition;
            // Measured in the normalized prefab's local coordinates; these models' collars are not at the footprint centre.
            // Legacy fallback only. New imported heads carry HandleSocket; TransformPoint handles idle pose and AREA scale.
            switch (id)
            {
                case ToolId.Squeegee: return new Vector3(0.012f, 0.116f, -0.008f);
                case ToolId.Mop: return new Vector3(0.130f, 0.058f, -0.001f);
                case ToolId.WetVacuum: return new Vector3(0f, 0.074f, -0.057f);
                case ToolId.Brush: return new Vector3(0f, 0.035f, 0f);
                default: return new Vector3(0f, 0.020f, 0f);
            }
        }

        static float ShaftRadius(ToolId id)
        {
            // Metres, fitted to the normalized 2026-11-15 collar openings (source meshes remain untouched).
            switch (id)
            {
                case ToolId.Scraper: return 0.009f;
                case ToolId.Brush: return 0.006f;
                case ToolId.Squeegee: return 0.011f;
                case ToolId.WetVacuum: return 0.014f;
                case ToolId.Mop: return 0.008f;
                default: return 0.014f;
            }
        }

        static void Strip(GameObject go)
        {
            foreach (var child in go.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }
        }
    }
}
