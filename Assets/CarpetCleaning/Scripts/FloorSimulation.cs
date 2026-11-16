using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    /// <summary>Floor stats from K_FReduce: per-cell means over the 1280x1280 domain (mm for water), pooledCells = fraction with h > 0.3 mm.</summary>
    public struct FloorStats
    {
        public bool valid;
        public float water, slurry, foam, foamDirt, dust, residue, wetFilm, detergent;
        public float pooledCells, dirtyCells;
    }

    /// <summary>
    /// Owns the floor simulation textures (Docs/FLOOR_SYSTEM.md section 4) and runs
    /// K_FInject -> nSub x (K_FFlux, K_FIntegrate) -> K_FChem every LateUpdate (order 110, after CarpetSimulation 100 and DebrisSystem 105).
    /// Reads the carpet's EdgeOut/EdgeOut2 runoff textures and its stamp buffer in the same frame; shares InjectBuffer with DebrisSystem.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public sealed class FloorSimulation : MonoBehaviour
    {
        public const int FW = FloorConsts.FloorW, FH = FloorConsts.FloorW;
        public const int CellCount = FW * FH;
        public const float DtSim = 1f / 240f;
        public const int MaxSubsteps = 8;
        const int kStatsCount = 16;
        const int kReduceScale = 128; // Match FREDUCE_SCALE: 1280^2 * 12 mm * 128 = 2516582400, below uint32.

        public ComputeShader compute;
        public FloorSettings settings;
        public CarpetSimulation carpet;

        readonly RenderTexture[] _fluid = new RenderTexture[2];
        readonly RenderTexture[] _soap = new RenderTexture[2];
        RenderTexture _deposit, _flux, _bed, _toolFlow, _agit;
        int _curFluid, _curSoap;
        ComputeBuffer _inject, _statsBuf, _dummyStamps;
        readonly uint[] _statsRaw = new uint[kStatsCount];

        int kInit = -1, kClearInject = -1, kInject = -1, kFlux = -1, kIntegrate = -1, kChem = -1, kClearStats = -1, kReduce = -1;
        bool _allocated, _loggedNoCompute, _subscribed;
        float _simTime;
        CarpetSimulation _subscribedCarpet;

        public RenderTexture Fluid => _fluid[_curFluid];
        public RenderTexture Soap => _soap[_curSoap];
        public RenderTexture Deposit => _deposit;
        public RenderTexture Bed => _bed;
        /// <summary>FW*FH*4 uints, fixed point x FloorConsts.InjectScale (ch0 water mm, ch1 slurry, ch2 foam F, ch3 dust). Written by DebrisSystem, consumed + zeroed by K_FInject.</summary>
        public ComputeBuffer InjectBuffer => _inject;
        public bool Ready => _allocated && compute != null;
        public float SimTime => _simTime;

        static readonly int ID_FFluidIn = Shader.PropertyToID("_FFluidIn"), ID_FFluidOut = Shader.PropertyToID("_FFluidOut");
        static readonly int ID_FSoapIn = Shader.PropertyToID("_FSoapIn"), ID_FSoapOut = Shader.PropertyToID("_FSoapOut");
        static readonly int ID_FDepositIn = Shader.PropertyToID("_FDepositIn"), ID_FDeposit = Shader.PropertyToID("_FDeposit");
        static readonly int ID_FFluxIn = Shader.PropertyToID("_FFluxIn"), ID_FFlux = Shader.PropertyToID("_FFlux");
        static readonly int ID_FBedIn = Shader.PropertyToID("_FBedIn"), ID_FBed = Shader.PropertyToID("_FBed");
        static readonly int ID_FToolFlowIn = Shader.PropertyToID("_FToolFlowIn"), ID_FToolFlow = Shader.PropertyToID("_FToolFlow");
        static readonly int ID_FAgitIn = Shader.PropertyToID("_FAgitIn"), ID_FAgit = Shader.PropertyToID("_FAgit");
        static readonly int ID_FInject = Shader.PropertyToID("_FInject"), ID_FStatsBuf = Shader.PropertyToID("_FStatsBuf");
        static readonly int ID_EdgeOut = Shader.PropertyToID("_EdgeOut"), ID_EdgeOut2 = Shader.PropertyToID("_EdgeOut2");
        static readonly int ID_Stamps = Shader.PropertyToID("_Stamps");

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            if (settings == null) settings = ScriptableObject.CreateInstance<FloorSettings>();
            if (carpet == null) carpet = FindAnyObjectByType<CarpetSimulation>();
            Allocate();
        }

        void OnEnable() { Subscribe(); }
        void OnDisable() { Unsubscribe(); }

        void Start()
        {
            Subscribe();
            if (Ready) ResetFloor();
        }

        void Subscribe()
        {
            if (_subscribed || carpet == null) return;
            carpet.CarpetReset += OnCarpetReset;
            _subscribedCarpet = carpet;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_subscribedCarpet != null) _subscribedCarpet.CarpetReset -= OnCarpetReset;
            _subscribedCarpet = null;
            _subscribed = false;
        }

        void OnCarpetReset() { if (Ready) ResetFloor(); }

        void OnDestroy()
        {
            Unsubscribe();
            AsyncGPUReadback.WaitAllRequests();
            for (int i = 0; i < 2; i++) { Release(ref _fluid[i]); Release(ref _soap[i]); }
            Release(ref _deposit); Release(ref _flux); Release(ref _bed); Release(ref _toolFlow); Release(ref _agit);
            if (_inject != null) { _inject.Release(); _inject = null; }
            if (_statsBuf != null) { _statsBuf.Release(); _statsBuf = null; }
            if (_dummyStamps != null) { _dummyStamps.Release(); _dummyStamps = null; }
            _allocated = false;
        }

        static void Release(ref RenderTexture rt)
        {
            if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
        }

        static RenderTexture MakeRT(string name, GraphicsFormat fmt)
        {
            var rt = new RenderTexture(FW, FH, 0, fmt)
            {
                name = name, enableRandomWrite = true, filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp, useMipMap = false, autoGenerateMips = false,
                hideFlags = HideFlags.DontUnloadUnusedAsset   // private-field-only RTs must survive editor asset refreshes
            };
            rt.Create();
            return rt;
        }

        void Allocate()
        {
            if (compute == null)
            {
                if (!_loggedNoCompute) { Debug.LogError("[FloorSimulation] compute shader not assigned; floor simulation disabled.", this); _loggedNoCompute = true; }
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                _fluid[i] = MakeRT("FFluid" + i, GraphicsFormat.R32G32B32A32_SFloat);
                _soap[i] = MakeRT("FSoap" + i, GraphicsFormat.R32G32B32A32_SFloat);
            }
            _deposit = MakeRT("FDeposit", GraphicsFormat.R32G32B32A32_SFloat);
            _flux = MakeRT("FFlux", GraphicsFormat.R32G32B32A32_SFloat);
            _bed = MakeRT("FBed", GraphicsFormat.R32_SFloat);
            _toolFlow = MakeRT("FToolFlow", GraphicsFormat.R16G16B16A16_SFloat);
            _agit = MakeRT("FAgit", GraphicsFormat.R16G16B16A16_SFloat);

            _inject = new ComputeBuffer(CellCount * 4, sizeof(uint), ComputeBufferType.Structured);
            _statsBuf = new ComputeBuffer(kStatsCount, sizeof(uint), ComputeBufferType.Structured);
            _dummyStamps = new ComputeBuffer(1, ToolStamp.Size, ComputeBufferType.Structured);
            _dummyStamps.SetData(new ToolStamp[1]);

            kInit = compute.FindKernel("K_FInit");
            kClearInject = compute.FindKernel("K_FClearInject");
            kInject = compute.FindKernel("K_FInject");
            kFlux = compute.FindKernel("K_FFlux");
            kIntegrate = compute.FindKernel("K_FIntegrate");
            kChem = compute.FindKernel("K_FChem");
            kClearStats = compute.FindKernel("K_FClearStats");
            kReduce = compute.FindKernel("K_FReduce");
            _allocated = true;
            SetGlobals();
        }

        // ------------------------------------------------------------------ public API
        /// <summary>K_FInit on both ping-pong halves (tile bed + cleared state) and K_FClearInject.</summary>
        public void ResetFloor()
        {
            if (!Ready) return;
            _simTime = 0f;
            UploadUniforms(DtSim * 4f);
            for (int b = 0; b < 2; b++)
            {
                compute.SetTexture(kInit, ID_FBed, _bed);
                compute.SetTexture(kInit, ID_FFluidOut, _fluid[b]);
                compute.SetTexture(kInit, ID_FSoapOut, _soap[b]);
                compute.SetTexture(kInit, ID_FDeposit, _deposit);
                compute.SetTexture(kInit, ID_FFlux, _flux);
                compute.SetTexture(kInit, ID_FToolFlow, _toolFlow);
                compute.SetTexture(kInit, ID_FAgit, _agit);
                compute.Dispatch(kInit, FW / 8, FH / 8, 1);
            }
            _curFluid = _curSoap = 0;
            compute.SetBuffer(kClearInject, ID_FInject, _inject);
            compute.Dispatch(kClearInject, (CellCount * 4 + 255) / 256, 1, 1);
            SetGlobals();
        }

        /// <summary>One simulated frame: K_FInject (edge runoff + debris deposits + stamps) -> nSub x (K_FFlux, K_FIntegrate) -> K_FChem -> globals.</summary>
        public void StepFrame(float dtFrame)
        {
            if (!Ready) return;
            dtFrame = Mathf.Clamp(dtFrame, 1e-4f, 1f / 30f * 4f + 1e-3f);
            int nSub = Mathf.Clamp(Mathf.RoundToInt(dtFrame / DtSim), 1, MaxSubsteps);
            _simTime += dtFrame;
            UploadUniforms(dtFrame);

            // carpet coupling: this frame's runoff textures and stamps (CarpetSimulation ran first, order 100)
            bool haveCarpet = carpet != null && carpet.Ready && carpet.EdgeOut != null && carpet.StampBuffer != null;
            Texture edge = haveCarpet ? (Texture)carpet.EdgeOut : Texture2D.blackTexture;
            Texture edge2 = haveCarpet ? (Texture)carpet.EdgeOut2 : Texture2D.blackTexture;
            ComputeBuffer stamps = haveCarpet ? carpet.StampBuffer : _dummyStamps;
            int stampCount = haveCarpet ? Mathf.Clamp(carpet.StampCount, 0, CarpetSimulation.MaxStamps) : 0;
            compute.SetInt("_StampCount", stampCount);

            // 1. inject (in place on the current buffers; rewrites _FAgit / _FToolFlow)
            compute.SetTexture(kInject, ID_FFluidOut, _fluid[_curFluid]);
            compute.SetTexture(kInject, ID_FSoapOut, _soap[_curSoap]);
            compute.SetTexture(kInject, ID_FDeposit, _deposit);
            compute.SetTexture(kInject, ID_FAgit, _agit);
            compute.SetTexture(kInject, ID_FToolFlow, _toolFlow);
            compute.SetBuffer(kInject, ID_FInject, _inject);
            compute.SetTexture(kInject, ID_EdgeOut, edge);
            compute.SetTexture(kInject, ID_EdgeOut2, edge2);
            compute.SetBuffer(kInject, ID_Stamps, stamps);
            compute.Dispatch(kInject, FW / 8, FH / 8, 1);

            // 2. substepped shallow water + transport
            for (int s = 0; s < nSub; s++)
            {
                int fc = _curFluid, fn = 1 - _curFluid, sc = _curSoap, sn = 1 - _curSoap;
                compute.SetTexture(kFlux, ID_FFluidIn, _fluid[fc]);
                compute.SetTexture(kFlux, ID_FBedIn, _bed);
                compute.SetTexture(kFlux, ID_FToolFlowIn, _toolFlow);
                compute.SetTexture(kFlux, ID_FFlux, _flux);
                compute.Dispatch(kFlux, FW / 8, FH / 8, 1);

                compute.SetTexture(kIntegrate, ID_FFluidIn, _fluid[fc]);
                compute.SetTexture(kIntegrate, ID_FSoapIn, _soap[sc]);
                compute.SetTexture(kIntegrate, ID_FFluxIn, _flux);
                compute.SetTexture(kIntegrate, ID_FToolFlowIn, _toolFlow);
                compute.SetTexture(kIntegrate, ID_FFluidOut, _fluid[fn]);
                compute.SetTexture(kIntegrate, ID_FSoapOut, _soap[sn]);
                compute.Dispatch(kIntegrate, FW / 8, FH / 8, 1);
                _curFluid = fn; _curSoap = sn;
            }

            // 3. chemistry (in place)
            compute.SetTexture(kChem, ID_FFluidOut, _fluid[_curFluid]);
            compute.SetTexture(kChem, ID_FSoapOut, _soap[_curSoap]);
            compute.SetTexture(kChem, ID_FDeposit, _deposit);
            compute.SetTexture(kChem, ID_FAgitIn, _agit);
            compute.Dispatch(kChem, FW / 8, FH / 8, 1);

            // 4. globals
            SetGlobals();
        }

        /// <summary>K_FClearStats + K_FReduce, blocking readback. Means per cell; pooledCells is a fraction.</summary>
        public FloorStats FloorStatsSync()
        {
            if (!Ready) return default;
            compute.SetInt("_FW", FW); compute.SetInt("_FH", FH);
            if (settings != null) settings.Apply(compute);
            compute.SetBuffer(kClearStats, ID_FStatsBuf, _statsBuf);
            compute.Dispatch(kClearStats, 1, 1, 1);
            compute.SetTexture(kReduce, ID_FFluidIn, _fluid[_curFluid]);
            compute.SetTexture(kReduce, ID_FSoapIn, _soap[_curSoap]);
            compute.SetTexture(kReduce, ID_FDepositIn, _deposit);
            compute.SetBuffer(kReduce, ID_FStatsBuf, _statsBuf);
            compute.Dispatch(kReduce, FW / 8, FH / 8, 1);
            var req = AsyncGPUReadback.Request(_statsBuf);
            req.WaitForCompletion();
            if (req.hasError) return default;
            var data = req.GetData<uint>();
            for (int i = 0; i < kStatsCount && i < data.Length; i++) _statsRaw[i] = data[i];
            const float inv = 1f / (kReduceScale * (float)CellCount);
            return new FloorStats
            {
                valid = true,
                water = _statsRaw[0] * inv, slurry = _statsRaw[1] * inv, foam = _statsRaw[2] * inv, foamDirt = _statsRaw[3] * inv,
                dust = _statsRaw[4] * inv, residue = _statsRaw[5] * inv, wetFilm = _statsRaw[6] * inv, pooledCells = _statsRaw[7] * inv,
                detergent = _statsRaw[8] * inv, dirtyCells = _statsRaw[9] * inv
            };
        }

        void LateUpdate()
        {
            if (!Ready) return;
            if (carpet != null && carpet.IsPaused) return;
            float scale = carpet != null ? carpet.TimeScale : 1f;
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f) * scale;
            if (dt <= 0f) return;
            StepFrame(dt);
        }

        // ------------------------------------------------------------------ internals
        void UploadUniforms(float dtFrame)
        {
            compute.SetInt("_FW", FW);
            compute.SetInt("_FH", FH);
            compute.SetFloat("_FDX", FloorConsts.FloorDxMm);
            compute.SetVector("_FloorMin", new Vector4(FloorConsts.FloorMinMm.x, FloorConsts.FloorMinMm.y, 0f, 0f));
            compute.SetFloat("_FloorSize", FloorConsts.FloorSizeMm);
            compute.SetFloat("_DtSim", DtSim);
            compute.SetFloat("_DtFrame", dtFrame);
            compute.SetFloat("_Time", _simTime);
            // 1 = the carpet has been handed over: the rect stops being a wall and becomes ordinary tile (HANDOVER.md 6)
            compute.SetInt("_CarpetAway", CarpetAway ? 1 : 0);
            if (settings != null) settings.Apply(compute);
            // the rotary brush is one machine on pile and tile alike: its numbers live in the carpet's SimSettings
            var brush = carpet != null ? carpet.settings : null;
            compute.SetFloat("_BRUSH_RPM", brush != null ? brush.BRUSH_RPM : 0f);
            compute.SetFloat("_BRUSH_SWIRL", brush != null ? brush.BRUSH_SWIRL : 0f);
            compute.SetFloat("_BRUSH_FLING", brush != null ? brush.BRUSH_FLING : 0f);
            // tool POWER upgrades (Docs/ECONOMY.md 2): the floor feels the same upgraded tool as the pile
            ToolTuning.FillPower(_toolPower);
            compute.SetVectorArray("_ToolPower", _toolPower);
        }

        readonly Vector4[] _toolPower = new Vector4[4];

        /// <summary>True while the carpet is away and the rect it covers must simulate and render as plain tile.</summary>
        public bool CarpetAway { get { return carpet != null && carpet.CarpetAway; } }

        /// <summary>Shader globals: _FFluidTex _FSoapTex _FDepositTex _FBedTex _FloorRect _FloorTexel _FloorConsts _CarpetAway (+ _F_TILE/_F_GROUT_W/_F_GROUT_DEPTH for FloorSurface).</summary>
        void SetGlobals()
        {
            if (!_allocated) return;
            // FloorSurface / FloorWater / CarpetFoam(_FLOOR_DOMAIN) all cut the carpet rect out of the floor; this lets them back in
            Shader.SetGlobalFloat("_CarpetAway", CarpetAway ? 1f : 0f);
            Shader.SetGlobalTexture("_FFluidTex", _fluid[_curFluid]);
            Shader.SetGlobalTexture("_FSoapTex", _soap[_curSoap]);
            Shader.SetGlobalTexture("_FDepositTex", _deposit);
            Shader.SetGlobalTexture("_FBedTex", _bed);
            Shader.SetGlobalVector("_FloorRect", new Vector4(FloorConsts.FloorMinMm.x, FloorConsts.FloorMinMm.y, 1f / FloorConsts.FloorSizeMm, FloorConsts.FloorYMm));
            Shader.SetGlobalVector("_FloorTexel", new Vector4(1f / FW, 1f / FH, FW, FH));
            float hmax = 12f, fmax = 12f, tile = 300f, groutW = 6f, groutD = 1f;
            if (settings != null) { hmax = settings.F_H_MAX; fmax = settings.F_F_MAX; tile = settings.F_TILE; groutW = settings.F_GROUT_W; groutD = settings.F_GROUT_DEPTH; }
            Shader.SetGlobalVector("_FloorConsts", new Vector4(hmax, fmax, FloorConsts.FloorDxMm, 0f));
            Shader.SetGlobalFloat("_F_TILE", tile);
            Shader.SetGlobalFloat("_F_GROUT_W", groutW);
            Shader.SetGlobalFloat("_F_GROUT_DEPTH", groutD);
            Shader.SetGlobalFloat("_F_DET_REF", settings != null ? settings.F_DET_REF : 0.35f);   // FloorSurface reads the film against it
        }
    }
}
