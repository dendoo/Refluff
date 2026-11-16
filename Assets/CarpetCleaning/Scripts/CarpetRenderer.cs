using UnityEngine;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    /// <summary>Builds the shell, water, foam and cursor meshes at Awake and drives the cursor globals (LateUpdate, order 200).</summary>
    [DefaultExecutionOrder(200)]
    public sealed class CarpetRenderer : MonoBehaviour
    {
        public Material fiberMaterial, waterMaterial, foamMaterial, cursorMaterial;
        public const int DefaultShellCount = 48;
        public int shellCount = DefaultShellCount;
        public int shellSubdivX = CarpetSimulation.SimW / 4, shellSubdivZ = CarpetSimulation.SimH / 4;
        public int waterSubdivX = CarpetSimulation.SimW / 2, waterSubdivZ = CarpetSimulation.SimH / 2;
        public int foamSubdivX = CarpetSimulation.SimW, foamSubdivZ = CarpetSimulation.SimH;   // one vertex per sim cell: the foam mound silhouette follows the F field (Docs/FOAM_LOOK.md)
        public float pileHeightM = 0.02f;

        public ToolModelView toolModels;   // shows the 3D head; the outline below is the fallback where a head is missing

        Mesh _shellMesh, _waterMesh, _foamMesh, _cursorMesh;
        Transform _cursorTf, _stage;
        MeshRenderer _cursorMr;
        Vector4 _cursor = new Vector4(0.5f, 0.5f, 10f, 0f), _cursor2 = Vector4.zero;

        public Transform Shells { get; private set; }
        public Transform Water { get; private set; }
        public Transform Foam { get; private set; }
        public Transform Cursor => _cursorTf;
        public MeshRenderer ShellsRenderer { get; private set; }
        public MeshRenderer WaterRenderer { get; private set; }
        public MeshRenderer FoamRenderer { get; private set; }

        /// <summary>
        /// Visual-only child holding the three carpet surfaces. CarpetHandover animates THIS (Docs/HANDOVER.md), never
        /// CarpetRoot: the collider, the tool cursor, the stamp path and the debris frame (_CarpetToWorld) must stay put.
        /// </summary>
        public Transform Stage => EnsureStage();

        Transform EnsureStage()
        {
            if (_stage == null)
            {
                var t = transform.Find("CarpetStage");
                if (t == null)
                {
                    var go = new GameObject("CarpetStage");
                    t = go.transform;
                    t.SetParent(transform, false);
                }
                _stage = t;
            }
            _stage.localScale = Vector3.one;   // a scale here would scale the mm vertex displacement: the pile would grow
            return _stage;
        }

        void Awake()
        {
            // Existing scenes serialize the previous 24-layer default; upgrade it without a scene rebuild.
            if (shellCount == 24) shellCount = DefaultShellCount;
            var size = CarpetSimulation.CarpetSizeM;
            _shellMesh = ShellMeshBuilder.BuildShells(size.x, size.y, Mathf.Max(1, shellCount), shellSubdivX, shellSubdivZ, pileHeightM);
            _waterMesh = ShellMeshBuilder.BuildGrid(size.x, size.y, waterSubdivX, waterSubdivZ);
            _foamMesh = ShellMeshBuilder.BuildGrid(size.x, size.y, foamSubdivX, foamSubdivZ);   // bounds y -0.10..0.20 m: keep the mound under ~90 mm
            _foamMesh.name = "FoamSurface";
            _cursorMesh = ShellMeshBuilder.BuildQuad();

            var stage = EnsureStage();
            stage.localPosition = Vector3.zero; stage.localRotation = Quaternion.identity;
            Shells = Setup("CarpetShells", _shellMesh, fiberMaterial, out var shellsMr, stage);
            Water = Setup("WaterSurface", _waterMesh, waterMaterial, out var waterMr, stage);
            Foam = Setup("FoamSurface", _foamMesh, foamMaterial, out var foamMr, stage);
            ShellsRenderer = shellsMr; WaterRenderer = waterMr; FoamRenderer = foamMr;
            if (foamMaterial == null)
            {
                Debug.LogWarning("[CarpetRenderer] foamMaterial is not assigned (run CarpetCleaning/Build Scene); foam hidden.", this);
                if (foamMr != null) foamMr.enabled = false;
            }
            _cursorTf = Setup("ToolCursor", _cursorMesh, cursorMaterial, out _cursorMr, transform);
            _cursorTf.localPosition = new Vector3(0f, 0.03f, 0f);
            _cursorTf.localScale = new Vector3(0.02f, 1f, 0.02f);
            if (_cursorMr != null) _cursorMr.enabled = false;
            PushCursorGlobals();
        }

        Transform Setup(string childName, Mesh mesh, Material mat, out MeshRenderer mr, Transform parent)
        {
            if (parent == null) parent = transform;
            var t = parent.Find(childName);
            if (t == null) t = transform.Find(childName);        // adopt a child built before CarpetStage existed
            if (t == null)
            {
                var go = new GameObject(childName);
                t = go.transform;
            }
            if (t.parent != parent) t.SetParent(parent, false);
            t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity; t.localScale = Vector3.one;
            var mf = t.GetComponent<MeshFilter>(); if (mf == null) mf = t.gameObject.AddComponent<MeshFilter>();
            mr = t.GetComponent<MeshRenderer>(); if (mr == null) mr = t.gameObject.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            if (mat != null) mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            return t;
        }

        void OnDestroy()
        {
            if (_shellMesh != null) Destroy(_shellMesh);
            if (_waterMesh != null) Destroy(_waterMesh);
            if (_foamMesh != null) Destroy(_foamMesh);
            if (_cursorMesh != null) Destroy(_cursorMesh);
        }

        /// <summary>Updates _ToolCursor / _ToolCursor2 globals and places the cursor quad on the carpet (local y = 0.03).</summary>
        public void SetCursor(Vector2 uv, Vector2 dirMm, float radiusMm, float lengthMm, ToolId tool, bool active, float heightM = 0.03f)
        {
            if (dirMm.sqrMagnitude < 1e-8f) dirMm = new Vector2(1f, 0f);
            dirMm.Normalize();
            _cursor = new Vector4(uv.x, uv.y, radiusMm, active ? 1f : 0f);
            _cursor2 = new Vector4(dirMm.x, dirMm.y, lengthMm, (int)tool);
            PushCursorGlobals();
            if (_cursorTf == null) return;
            var size = CarpetSimulation.CarpetSizeM;
            float x = (uv.x - 0.5f) * size.x, z = (uv.y - 0.5f) * size.y;
            _cursorTf.localPosition = new Vector3(x, heightM, z);   // 0.03 over the pile, ~0.009 over the tile when the pointer is off the carpet
            // quad local X follows the stroke direction; dir is in mm space (x along u, y along v = local Z)
            float angleDeg = Mathf.Atan2(dirMm.y, dirMm.x) * Mathf.Rad2Deg;
            _cursorTf.localRotation = Quaternion.Euler(0f, -angleDeg, 0f);
            bool blade = lengthMm > 0f;
            float sx = blade ? radiusMm * 2f : radiusMm * 2f;           // along the stroke: blade thickness or disc diameter
            float sz = blade ? lengthMm : radiusMm * 2f;                // across the stroke
            const float pad = 1.15f;
            _cursorTf.localScale = new Vector3(Mathf.Max(sx, 1f) * 0.001f * pad, 1f, Mathf.Max(sz, 1f) * 0.001f * pad);

            // A tool with a generated head shows the model instead of the outline; tools still waiting for
            // their model keep the outline, so no tool is ever left without a cursor (Docs/TOOL_MODELS.md).
            bool hasModel = false;
            if (toolModels != null)
            {
                toolModels.SetTool(uv, dirMm, tool, active, heightM);
                hasModel = toolModels.Has(tool);
            }
            // FPS has a reticle and a held body even for the three tools without a legacy contact head.
            bool firstPerson = toolModels != null && toolModels.tools != null &&
                toolModels.tools.FirstPersonActive && !toolModels.tools.ScriptedPointer;
            if (_cursorMr != null) _cursorMr.enabled = active && !hasModel && !firstPerson;
        }

        void PushCursorGlobals()
        {
            Shader.SetGlobalVector("_ToolCursor", _cursor);
            Shader.SetGlobalVector("_ToolCursor2", _cursor2);
        }

        void LateUpdate()
        {
            PushCursorGlobals();
            Vector3 rim = new Vector3(0.35f, 0.55f, 0.75f).normalized;
            Shader.SetGlobalVector("_RimDir", new Vector4(rim.x, rim.y, rim.z, 0f));
            Shader.SetGlobalVector("_RimColor", new Vector4(0.55f * 0.6f, 0.65f * 0.6f, 0.85f * 0.6f, 1f));
        }
    }
}
