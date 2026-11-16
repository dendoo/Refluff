using UnityEngine;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    /// <summary>
    /// Builds the floor meshes at Awake (Docs/FLOOR_SYSTEM.md section 5): "FloorSurface" = 12 x 12 m opaque plane (4x4 quads),
    /// "FloorWater" = 12 x 12 m grid (320x320) covering the floor simulation domain and "FloorFoam" = 12 m grid (640x640)
    /// displaced by the floor foam (Docs/FOAM_LOOK.md). All children sit at local y = -0.003 (FLOOR_Y), so the shaders displace
    /// relative to the floor top. Children are found by name or created. Order 200.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class FloorRenderer : MonoBehaviour
    {
        public Material surfaceMaterial, waterMaterial, foamMaterial;
        public float surfaceSizeM = 12f;
        public int surfaceQuads = 4;
        public int waterSubdiv = FloorConsts.FloorW / 4;
        public int foamSubdiv = FloorConsts.FloorW / 2;   // multiple of 80 (grid lines on the carpet rim, Docs/CARPET_EDGE.md)

        Mesh _surfaceMesh, _waterMesh, _foamMesh;

        public Transform Surface { get; private set; }
        public Transform Water { get; private set; }
        public Transform Foam { get; private set; }

        void Awake()
        {
            // sane tile globals even before FloorSimulation.SetGlobals runs (or when it is missing), otherwise the floor renders as solid grout
            Shader.SetGlobalFloat("_F_TILE", 300f);
            Shader.SetGlobalFloat("_F_GROUT_W", 6f);
            Shader.SetGlobalFloat("_F_GROUT_DEPTH", 1f);
            Shader.SetGlobalVector("_FloorRect", new Vector4(FloorConsts.FloorMinMm.x, FloorConsts.FloorMinMm.y, 1f / FloorConsts.FloorSizeMm, FloorConsts.FloorYMm));
            Shader.SetGlobalVector("_FloorTexel", new Vector4(1f / FloorConsts.FloorW, 1f / FloorConsts.FloorW, FloorConsts.FloorW, FloorConsts.FloorW));
            Shader.SetGlobalVector("_FloorConsts", new Vector4(12f, 12f, FloorConsts.FloorDxMm, 0f));
            float floorY = FloorConsts.FloorYMm * 0.001f;             // -0.003 m
            float domainM = FloorConsts.FloorSizeMm * 0.001f;         // 12 m, centred on the carpet centre = local origin
            int q = Mathf.Max(1, surfaceQuads);
            int w = Mathf.Clamp(waterSubdiv, 1, 1023);

            _surfaceMesh = ShellMeshBuilder.BuildGrid(surfaceSizeM, surfaceSizeM, q, q);
            _surfaceMesh.name = "FloorSurface";
            _surfaceMesh.bounds = new Bounds(new Vector3(0f, 0.05f, 0f), new Vector3(surfaceSizeM, 0.3f, surfaceSizeM));

            _waterMesh = ShellMeshBuilder.BuildGrid(domainM, domainM, w, w);
            _waterMesh.name = "FloorWater";
            var b = _waterMesh.bounds;
            _waterMesh.bounds = new Bounds(new Vector3(0f, 0.05f, 0f), new Vector3(domainM, b.size.y + 0.1f, domainM));

            int fq = Mathf.Clamp(foamSubdiv, 1, 1023);
            // the carpet foam's rim vertices copy the height this coarser grid rasterizes along the rim, so the two foam meshes meet without a step (Docs/CARPET_EDGE.md);
            // the 3.6 x 4.5 m rug edges sit at floor UV .35/.65 and .3125/.6875: fq must be a multiple of 80.
            if (fq % 80 != 0) Debug.LogWarning("[FloorRenderer] foamSubdiv " + fq + " is not a multiple of 80: the floor foam grid lines miss the carpet rim, the two foam meshes will show a seam.", this);
            Shader.SetGlobalFloat("_FloorFoamPitchMm", FloorConsts.FloorSizeMm / fq);
            _foamMesh = ShellMeshBuilder.BuildGrid(domainM, domainM, fq, fq);
            _foamMesh.name = "FloorFoam";
            _foamMesh.bounds = new Bounds(new Vector3(0f, 0.05f, 0f), new Vector3(domainM, 0.4f, domainM));   // mound up to ~60 mm over the tile

            Surface = Setup("FloorSurface", _surfaceMesh, surfaceMaterial, floorY);
            Water = Setup("FloorWater", _waterMesh, waterMaterial, floorY);
            Foam = Setup("FloorFoam", _foamMesh, foamMaterial, floorY);
            if (foamMaterial == null)
            {
                Debug.LogWarning("[FloorRenderer] foamMaterial is not assigned (run CarpetCleaning/Build Scene); floor foam hidden.", this);
                var mr = Foam.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }
        }

        Transform Setup(string childName, Mesh mesh, Material mat, float localY)
        {
            var t = transform.Find(childName);
            if (t == null)
            {
                var go = new GameObject(childName);
                t = go.transform;
                t.SetParent(transform, false);
            }
            t.localPosition = new Vector3(0f, localY, 0f); t.localRotation = Quaternion.identity; t.localScale = Vector3.one;
            var mf = t.GetComponent<MeshFilter>(); if (mf == null) mf = t.gameObject.AddComponent<MeshFilter>();
            var mr = t.GetComponent<MeshRenderer>(); if (mr == null) mr = t.gameObject.AddComponent<MeshRenderer>();
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
            if (_surfaceMesh != null) Destroy(_surfaceMesh);
            if (_waterMesh != null) Destroy(_waterMesh);
            if (_foamMesh != null) Destroy(_foamMesh);
        }
    }
}
