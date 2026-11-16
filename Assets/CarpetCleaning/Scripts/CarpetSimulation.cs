using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    public struct SimStats
    {
        public bool valid;
        public float crust, mud, stain, grit, slurry, foamDirt, water, pileWater, soap, foam; // per-cell means
        public float wetCells, pooledCells;                                                   // fractions
        public float dustCells, crustCells;                                                   // visible coverage: grit > 0.04, crust > 0.1
    }

    /// <summary>
    /// One sweep of the squeegee blade for the plow kernels (Docs/SQUEEGEE.md): blade centre and axis (the drawn head
    /// direction, unsigned) before and after, in carpet mm; sign = which way along the axis the blade travels.
    /// </summary>
    public struct PlowSweep
    {
        public Vector2 p0, p1, a0, a1;
        public float sign, radiusMm, halfLengthMm;
    }

    /// <summary>
    /// What a new carpet's dirt is made of (Docs/DIRT_LEVEL.md), rolled from its seed by CarpetSimulation.RollDirt:
    /// how soiled overall, how unevenly, and what the soil is spent on. K_Init reads it as _DirtMask / _DirtMix.
    /// </summary>
    public struct DirtProfile
    {
        public float level;     // 0 spotless .. 1 fully soiled
        public float spread;    // soil ramp width in noise units: wide = one carpet runs from clean to filthy, narrow = patches
        public float dust;      // loose dust (grit channel) on lightly soiled ground
        public float mud;       // ground-in grime: 1 hides the print where the soil is full
        public float crust;     // share of the carpet in dried-mud cake patches (0 = no crust at all)
        public float spill;     // share of the 12 spill stains that happened
        public Vector2 offset;  // where the carpet reads the soil noise, in noise cells

        /// <summary>The carpet every carpet was before 2026-09-24: crust and grime everywhere, every spill, no dust.</summary>
        public static DirtProfile FullySoiled => new DirtProfile { level = 1f, spread = 0.5f, dust = 0f, mud = 1f, crust = 1f, spill = 1f };
    }

    /// <summary>Owns every simulation render texture and runs the section 4.9 dispatch schedule (LateUpdate, order 100).</summary>
    [DefaultExecutionOrder(100)]
    public sealed class CarpetSimulation : MonoBehaviour
    {
        // Keep in step with Compute/SimulationDomain.hlsl. Physical size is independent of RT resolution.
        public const int SimW = 512, SimH = 640;
        public const int CellCount = SimW * SimH;
        public const float CarpetWidthMm = 3600f, CarpetHeightMm = 4500f;
        public const float CellSizeMm = CarpetWidthMm / SimW;
        public const float DtSim = 1f / 240f;
        public const int MaxStamps = 32;
        public static readonly Vector2 CarpetSizeMm = new Vector2(CarpetWidthMm, CarpetHeightMm);
        public static readonly Vector2 CarpetSizeM = CarpetSizeMm * 0.001f;

        // Stats 0..11 = means/wet coverage, 12..13 = visible dust/crust coverage (256 fixed point), 14..15 reserved.
        // Inspection lamp histograms appended to the stats buffer by K_Reduce (keep in step with CarpetSim.compute HIST_*):
        // per-cell DirtScore and wetness, amount and cell count per log bin (Docs/HIGHLIGHTS.md 4)
        public const int HistBins = 60;
        const float HistLo = -16f, HistPerOct = 3f;
        const double HistScale = 512.0;    // 327680 cells * cap16 *512 =2684354560 fits uint32; match HLSL
        const int StatsHist0 = 16;         // amounts: dirt, wet; then cell counts: dirt, wet (HistBins each)
        public const int StatsLen = StatsHist0 + 4 * HistBins;
        /// <summary>The HUD rounds its bars to whole percent, so it reads 100 % from 0.995 up (Mathf.RoundToInt(99.5f) == 100).</summary>
        public const float ShownFull = 0.995f;
        /// <summary>A lamp cut no cell reaches: the bar shows 100 %, so nothing on the carpet lights up.</summary>
        public const float NoCut = 1e30f;

        /// <summary>A bar value as the HUD prints it (whole percent). Every "is it 100 %" test uses this.</summary>
        public static int ShownPercent(float v) { return Mathf.RoundToInt(Mathf.Clamp01(v) * 100f); }

        public ComputeShader compute;
        public SimSettings settings;
        public FloorSettings floorSettings;   // D_* debris constants read by K_Stamp / K_Emit (Docs/FLOOR_SYSTEM.md 1.4)
        public int seed = 1;
        public bool randomFirstSeed = true;   // the first carpet of a play session rolls its seed, so its dirt level too (Docs/DIRT_LEVEL.md)
        public float TimeScale = 1f;
        public bool Paused;
        /// <summary>
        /// Set by the upgrade shop while it is open (Docs/ECONOMY.md 5). A flag of its own, not Paused: SteamService owns
        /// Paused for the overlay and only lifts a pause it set itself, so the two never clear each other.
        /// </summary>
        [System.NonSerialized] public bool UiPaused;
        /// <summary>True while anything holds the simulation still (Steam overlay, upgrade shop).</summary>
        public bool IsPaused => Paused || UiPaused;

        // ping-pong pairs + single buffers
        readonly RenderTexture[] _fluid = new RenderTexture[2];
        readonly RenderTexture[] _soap = new RenderTexture[2];
        readonly RenderTexture[] _fiber = new RenderTexture[2];
        RenderTexture _flux, _dirt, _crust, _fiberVel, _agit, _toolFlow, _bed;
        RenderTexture _fiberSet;              // (bSet.xy, fSet, 0): wet-pressed pile memory, K_Fiber only (Docs/FIBER_SET.md)
        RenderTexture _edgeOut, _edgeOut2;    // this frame's runoff over the carpet edge, consumed by FloorSimulation
        int _curFluid, _curSoap, _curFiber;

        ComputeBuffer _stampBuf, _statsBuf, _emitBuf;   // _emitBuf: AppendStructuredBuffer<DebrisEmit>, consumed by DebrisSystem
        int _stampCount;
        readonly ToolStamp[] _stampArr = new ToolStamp[MaxStamps];
        readonly Queue<ToolStamp> _pending = new Queue<ToolStamp>();
        readonly uint[] _statsRaw = new uint[StatsLen];

        int kInit = -1, kStamp = -1, kFlux = -1, kIntegrate = -1, kChemistry = -1, kFiber = -1, kClearStats = -1, kReduce = -1;
        int kClearEdge = -1, kEmit = -1, kClearFiberSet = -1;
        int kPlowScatter = -1, kPlowApply = -1, kPlowDrain = -1, kPlowClear = -1;
        // squeegee plow (Docs/SQUEEGEE.md): (W+2)(H+2)*6 uint accumulators, the outer ring = what waits to pour onto the floor
        public const int PlowChannels = 6;
        public const int PlowAccLength = (SimW + 2) * (SimH + 2) * PlowChannels;
        const int MaxPlowsPerFrame = 8;
        ComputeBuffer _plowAcc;
        readonly Queue<PlowSweep> _plows = new Queue<PlowSweep>();
        bool _allocated, _loggedNoCompute;
        float _simTime;
        int _frameCounter;
        Texture2D _noise;
        SimStats _stats, _initStats;

        public RenderTexture Fluid => _fluid[_curFluid];
        public RenderTexture Soap => _soap[_curSoap];
        public RenderTexture Dirt => _dirt;
        public RenderTexture Crust => _crust;
        public RenderTexture Fiber => _fiber[_curFiber];
        public RenderTexture Bed => _bed;
        public RenderTexture FiberSet => _fiberSet;
        public RenderTexture EdgeOut => _edgeOut;
        public RenderTexture EdgeOut2 => _edgeOut2;
        public ComputeBuffer EmitBuffer => _emitBuf;
        public ComputeBuffer StampBuffer => _stampBuf;
        /// <summary>Squeegee plow accumulators incl. the off-carpet spill ring (CarpetDebugApi reads the ring).</summary>
        public ComputeBuffer PlowAccBuffer => _plowAcc;
        /// <summary>Number of stamps uploaded for the current frame (valid after StepFrame until the next one).</summary>
        public int StampCount => _stampCount;
        /// <summary>
        /// True while the carpet has been handed over and the rect it covers is bare tile (Docs/HANDOVER.md).
        /// Owned by CarpetHandover. Two consumers: this sim uploads _StampCount = 0 to CarpetSim.compute, so no tool can
        /// wet an invisible carpet, while the stamp BUFFER stays filled for the floor sim and the debris system, which
        /// drop their carpet-rect wall and let every tool work on the exposed tile.
        /// </summary>
        [System.NonSerialized] public bool CarpetAway;
        public int Seed => seed;
        /// <summary>What the carpet on the sim is made of (Docs/DIRT_LEVEL.md).</summary>
        public DirtProfile CarpetDirt { get; private set; } = DirtProfile.FullySoiled;
        /// <summary>Dirt level of the carpet on the sim: 0 spotless .. 1 fully soiled (Docs/DIRT_LEVEL.md).</summary>
        public float DirtLevel => CarpetDirt.level;
        public Texture2D NoiseTex => _noise;
        public SimStats Stats => _stats;
        public SimStats InitStats => _initStats;
        public float Cleanliness { get; private set; }
        public float Wetness { get; private set; }
        /// <summary>
        /// Inspection lamp (Docs/HIGHLIGHTS.md 4): the level a carpet cell's DirtScore / wetness is lit above, set from the same
        /// stats snapshot as Cleanliness / Wetness. NoCut while the bar shows 100 %. Otherwise the carpet with every cell cut
        /// down to the level holds exactly what the bar tolerates at 100 %, so what glows is exactly what is left to do.
        /// </summary>
        public float LampCutDirt { get; private set; } = NoCut;
        public float LampCutWet { get; private set; } = NoCut;
        public float SoapLeft { get; private set; }
        public float FoamMean { get; private set; }
        public ToolId SuggestedTool { get; private set; }
        public string Hint { get; private set; } = "";
        public bool IsWon { get; private set; }
        public bool Ready => _allocated && compute != null;
        public float SimTime => _simTime;

        public event Action<SimStats> StatsUpdated;
        /// <summary>Fired at the end of ResetCarpet unless notify is false; FloorSimulation / DebrisSystem reset themselves on it,
        /// so the hand-over carpet-only reset (Docs/HANDOVER.md) passes false to keep the floor mess and the debris alive.</summary>
        public event Action CarpetReset;

        static readonly int ID_EdgeOut = Shader.PropertyToID("_EdgeOut"), ID_EdgeOut2 = Shader.PropertyToID("_EdgeOut2");
        static readonly int ID_EmitBuf = Shader.PropertyToID("_EmitBuf");
        static readonly int ID_FluidIn = Shader.PropertyToID("_FluidIn"), ID_FluidOut = Shader.PropertyToID("_FluidOut");
        static readonly int ID_Flux = Shader.PropertyToID("_Flux");
        static readonly int ID_SoapIn = Shader.PropertyToID("_SoapIn"), ID_SoapOut = Shader.PropertyToID("_SoapOut");
        static readonly int ID_Dirt = Shader.PropertyToID("_Dirt"), ID_Crust = Shader.PropertyToID("_Crust");
        static readonly int ID_FiberIn = Shader.PropertyToID("_FiberIn"), ID_FiberOut = Shader.PropertyToID("_FiberOut");
        static readonly int ID_FiberVel = Shader.PropertyToID("_FiberVel"), ID_Agit = Shader.PropertyToID("_Agit");
        static readonly int ID_ToolFlow = Shader.PropertyToID("_ToolFlow"), ID_Bed = Shader.PropertyToID("_Bed");
        static readonly int ID_Stamps = Shader.PropertyToID("_Stamps"), ID_StatsBuf = Shader.PropertyToID("_StatsBuf");
        static readonly int ID_NoiseTex = Shader.PropertyToID("_NoiseTex"), ID_FiberSet = Shader.PropertyToID("_FiberSet");
        static readonly int ID_PlowAcc = Shader.PropertyToID("_PlowAcc");
        static readonly int ID_PlowA = Shader.PropertyToID("_PlowA"), ID_PlowB = Shader.PropertyToID("_PlowB"), ID_PlowC = Shader.PropertyToID("_PlowC");
        static readonly int ID_DirtMask = Shader.PropertyToID("_DirtMask"), ID_DirtMix = Shader.PropertyToID("_DirtMix");
        static readonly int ID_HighlightCut = Shader.PropertyToID("_HighlightCut");

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            if (settings == null) settings = ScriptableObject.CreateInstance<SimSettings>();
            if (floorSettings == null) floorSettings = ScriptableObject.CreateInstance<FloorSettings>();
            _noise = NoiseTextureGenerator.Generate(7);
            _noise.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Shader.SetGlobalTexture(ID_NoiseTex, _noise);
            Allocate();
        }

        void Start()
        {
            // kept small: K_Init turns the seed into float noise offsets, and a huge offset eats the noise's precision
            if (randomFirstSeed) seed = UnityEngine.Random.Range(1, 1000);
            if (Ready) ResetCarpet(seed);
        }

        void OnDestroy()
        {
            AsyncGPUReadback.WaitAllRequests();
            for (int i = 0; i < 2; i++) { Release(ref _fluid[i]); Release(ref _soap[i]); Release(ref _fiber[i]); }
            Release(ref _flux); Release(ref _dirt); Release(ref _crust); Release(ref _fiberVel);
            Release(ref _agit); Release(ref _toolFlow); Release(ref _bed);
            Release(ref _edgeOut); Release(ref _edgeOut2); Release(ref _fiberSet);
            if (_stampBuf != null) { _stampBuf.Release(); _stampBuf = null; }
            if (_statsBuf != null) { _statsBuf.Release(); _statsBuf = null; }
            if (_emitBuf != null) { _emitBuf.Release(); _emitBuf = null; }
            if (_plowAcc != null) { _plowAcc.Release(); _plowAcc = null; }
            if (_noise != null) { Destroy(_noise); _noise = null; }
            _allocated = false;
        }

        static void Release(ref RenderTexture rt)
        {
            if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
        }

        static RenderTexture MakeRT(string name, GraphicsFormat fmt)
        {
            var rt = new RenderTexture(SimW, SimH, 0, fmt)
            {
                name = name, enableRandomWrite = true, filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp, useMipMap = false, autoGenerateMips = false,
                // Only private (non-serialized) fields reference these RTs, so an editor asset refresh /
                // Resources.UnloadUnusedAssets would destroy the unbound ping-pong half. Keep them alive.
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };
            rt.Create();
            return rt;
        }

        void Allocate()
        {
            if (compute == null)
            {
                if (!_loggedNoCompute) { Debug.LogError("[CarpetSimulation] compute shader not assigned; simulation disabled.", this); _loggedNoCompute = true; }
                return;
            }
            // Full float everywhere: slow processes (evaporation, hydration, wicking) add or remove far less than one
            // half-precision ulp per frame, which silently rounds to zero or to a whole ulp and breaks mass conservation.
            for (int i = 0; i < 2; i++)
            {
                _fluid[i] = MakeRT("Fluid" + i, GraphicsFormat.R32G32B32A32_SFloat);
                _soap[i] = MakeRT("Soap" + i, GraphicsFormat.R32G32B32A32_SFloat);
                _fiber[i] = MakeRT("Fiber" + i, GraphicsFormat.R32G32B32A32_SFloat);
            }
            _flux = MakeRT("Flux", GraphicsFormat.R32G32B32A32_SFloat);
            _dirt = MakeRT("Dirt", GraphicsFormat.R32G32B32A32_SFloat);
            _crust = MakeRT("Crust", GraphicsFormat.R8G8B8A8_UNorm);
            _fiberVel = MakeRT("FiberVel", GraphicsFormat.R32G32B32A32_SFloat);
            _fiberSet = MakeRT("FiberSet", GraphicsFormat.R32G32B32A32_SFloat);
            _agit = MakeRT("Agit", GraphicsFormat.R16G16B16A16_SFloat);
            _toolFlow = MakeRT("ToolFlow", GraphicsFormat.R16G16B16A16_SFloat);
            _bed = MakeRT("Bed", GraphicsFormat.R16_SFloat);
            _edgeOut = MakeRT("EdgeOut", GraphicsFormat.R32G32B32A32_SFloat);
            _edgeOut2 = MakeRT("EdgeOut2", GraphicsFormat.R32G32B32A32_SFloat);

            _stampBuf = new ComputeBuffer(MaxStamps, ToolStamp.Size, ComputeBufferType.Structured);
            _statsBuf = new ComputeBuffer(StatsLen, 4, ComputeBufferType.Structured);
            _emitBuf = new ComputeBuffer(FloorConsts.EmitCapacity, DebrisEmit.Size, ComputeBufferType.Append);
            _emitBuf.SetCounterValue(0);
            _plowAcc = new ComputeBuffer(PlowAccLength, 4, ComputeBufferType.Structured);

            kInit = compute.FindKernel("K_Init");
            kStamp = compute.FindKernel("K_Stamp");
            kFlux = compute.FindKernel("K_Flux");
            kIntegrate = compute.FindKernel("K_Integrate");
            kChemistry = compute.FindKernel("K_Chemistry");
            kFiber = compute.FindKernel("K_Fiber");
            kClearStats = compute.FindKernel("K_ClearStats");
            kReduce = compute.FindKernel("K_Reduce");
            kClearEdge = compute.FindKernel("K_ClearEdge");
            kEmit = compute.FindKernel("K_Emit");
            kClearFiberSet = compute.FindKernel("K_ClearFiberSet");
            kPlowScatter = compute.FindKernel("K_PlowScatter");
            kPlowApply = compute.FindKernel("K_PlowApply");
            kPlowDrain = compute.FindKernel("K_PlowDrain");
            kPlowClear = compute.FindKernel("K_PlowClear");
            _allocated = true;
            SetGlobals();
        }

        // ------------------------------------------------------------------ public API
        public void EnqueueStamp(in ToolStamp s) { _pending.Enqueue(s); }

        /// <summary>
        /// Queues one squeegee sweep for the plow kernels of the next StepFrame (Docs/SQUEEGEE.md). Ignored while the
        /// carpet is away, like the stamps: the floor sim still sees the squeegee's stamps and pushes the tile's water.
        /// </summary>
        public void EnqueuePlow(in PlowSweep s)
        {
            if (CarpetAway || _plows.Count >= 4 * MaxPlowsPerFrame) return;
            _plows.Enqueue(s);
        }

        public void ResetCarpet(int newSeed) { ResetCarpet(newSeed, true); }

        public void ResetCarpet(int newSeed, bool notify) { ResetCarpet(newSeed, notify, -1f); }

        /// <summary>
        /// Fresh carpet dirt for newSeed (RollDirt). A negative dirtLevel takes the seed's own roll and ensures at least
        /// 50% dust / 20% crust coverage; 0..1 forces the level and keeps the seed's legacy mix; 1 is the fully soiled carpet of before
        /// 2026-09-24 (Docs/DIRT_LEVEL.md).
        /// </summary>
        public void ResetCarpet(int newSeed, bool notify, float dirtLevel)
        {
            ResetCarpet(newSeed, notify, RollDirt(newSeed, dirtLevel), dirtLevel < 0f);
        }

        /// <summary>
        /// Fresh carpet with an explicit dirt profile instead of the seed's roll (store staging, CarpetDebugApi.ResetWithProfile,
        /// Docs/DIRT_LEVEL.md 8). The seed still places the crust plates, the stain blotches and the spill discs.
        /// </summary>
        public void ResetCarpet(int newSeed, bool notify, DirtProfile dirt)
        {
            ResetCarpet(newSeed, notify, dirt, false);
        }

        void ResetCarpet(int newSeed, bool notify, DirtProfile dirt, bool requireCoverage)
        {
            if (!Ready) return;
            seed = newSeed;
            CarpetDirt = dirt;
            _pending.Clear();
            _plows.Clear();
            _simTime = 0f;
            _frameCounter = 0;
            UploadUniforms(DtSim * 4f);
            compute.SetInt("_Seed", seed);
            DispatchInit();
            _curFluid = _curSoap = _curFiber = 0;
            compute.SetTexture(kClearFiberSet, ID_FiberSet, _fiberSet);
            compute.Dispatch(kClearFiberSet, SimW / 8, SimH / 8, 1);
            compute.SetTexture(kClearEdge, ID_EdgeOut, _edgeOut);
            compute.SetTexture(kClearEdge, ID_EdgeOut2, _edgeOut2);
            compute.Dispatch(kClearEdge, SimW / 8, SimH / 8, 1);
            // squeegee roll accumulators and the spill ring: a fresh buffer is not guaranteed to be zero, and water still
            // waiting to pour off the old carpet must not appear on the new one
            compute.SetBuffer(kPlowClear, ID_PlowAcc, _plowAcc);
            compute.Dispatch(kPlowClear, (PlowAccLength + 255) / 256, 1, 1);
            _emitBuf.SetCounterValue(0);
            _stampCount = 0;
            _initStats = ReadStatsSync();
            if (requireCoverage) EnsureStartingDirtCoverage();
            // A low roll can leave only a trace nobody would find, which would hold the Clean bar at 0 % on a carpet that looks
            // spotless: make it the spotless carpet it looks like (Docs/DIRT_LEVEL.md). A failed readback never wipes dirt.
            if (CarpetDirt.level > 0f && _initStats.valid && DirtScore(_initStats) < TraceDirtScore)
            {
                var spotless = CarpetDirt;
                spotless.level = 0f; spotless.spill = 0f;
                CarpetDirt = spotless;
                DispatchInit();
                _initStats = ReadStatsSync();
            }
            ApplyStats(_initStats);
            SetGlobals();
            if (notify) CarpetReset?.Invoke();
        }

        // Gameplay carpets must have visible dust on at least half their cells and crust on at least a fifth.
        // Mix coefficients alone cannot ensure that: K_Init gates both layers with the spatial soil field.
        // Keep the seed's masks/mix and only raise level/crust as far as needed. Forced debug/store profiles bypass this.
        void EnsureStartingDirtCoverage()
        {
            var rolled = CarpetDirt;
            for (int i = 1; i <= 8; i++)
            {
                if (_initStats.valid && _initStats.dustCells >= 0.5f && _initStats.crustCells >= 0.2f) return;
                var d = rolled;
                float t = i / 8f;
                d.level = i == 8 ? 1f : Mathf.Lerp(rolled.level, 1f, t);
                d.crust = i == 8 ? 1f : Mathf.Lerp(rolled.crust, 1f, t);
                CarpetDirt = d;
                DispatchInit();
                _initStats = ReadStatsSync();
            }
            // At the last step level/crust are 1, with dust >= 0.5: at stock DIRT_DUST even the heaviest soil
            // retains grit >= 0.07, above the 0.04 visibility cut. A failed readback therefore cannot roll a clean rug.
        }

        // Carpet-mean DirtScore below which a new carpet counts as spotless: less than the smallest spill stain (~4e-4);
        // the fully soiled carpet scores ~1.6. K_Init already cuts dust and grime below a visible amount.
        const float TraceDirtScore = 2e-4f;

        /// <summary>K_Init with the current seed and CarpetDirt into both halves of every ping-pong pair, so the first swap never exposes garbage.</summary>
        void DispatchInit()
        {
            var d = CarpetDirt;
            compute.SetVector(ID_DirtMask, new Vector4(d.level, d.offset.x, d.offset.y, d.spread));
            compute.SetVector(ID_DirtMix, new Vector4(d.dust, d.mud, d.crust, d.spill));
            for (int b = 0; b < 2; b++)
            {
                compute.SetTexture(kInit, ID_NoiseTex, _noise);
                compute.SetTexture(kInit, ID_FluidOut, _fluid[b]);
                compute.SetTexture(kInit, ID_SoapOut, _soap[b]);
                compute.SetTexture(kInit, ID_Dirt, _dirt);
                compute.SetTexture(kInit, ID_Crust, _crust);
                compute.SetTexture(kInit, ID_FiberOut, _fiber[b]);
                compute.SetTexture(kInit, ID_FiberVel, _fiberVel);
                compute.SetTexture(kInit, ID_Bed, _bed);
                compute.SetTexture(kInit, ID_Agit, _agit);
                compute.SetTexture(kInit, ID_ToolFlow, _toolFlow);
                compute.Dispatch(kInit, SimW / 8, SimH / 8, 1);
            }
        }

        /// <summary>The dirt level a carpet rolls from its seed, uniform over 0 (spotless) .. 1 (fully soiled) (Docs/DIRT_LEVEL.md).</summary>
        public static float DirtLevelForSeed(int seed) { return Hash01(seed, 0x68E31DA4u); }

        /// <summary>
        /// The dirt a carpet rolls from its seed (Docs/DIRT_LEVEL.md). Full-avalanche hashes, not seed arithmetic, so seeds
        /// n and n + 1 (one Reset or hand-over apart) are unrelated. A negative forcedLevel floors dust/crust coefficients;
        /// ResetCarpet then ensures actual coverage. forcedLevel in 0..1 keeps the legacy mix; 1 or more is fully soiled.
        /// </summary>
        public static DirtProfile RollDirt(int seed, float forcedLevel = -1f)
        {
            if (forcedLevel >= 1f) return DirtProfile.FullySoiled;
            var p = new DirtProfile
            {
                level = forcedLevel >= 0f ? forcedLevel : DirtLevelForSeed(seed),
                spread = Mathf.Lerp(0.45f, 1.3f, Hash01(seed, 0x2C1B3C6Du)),
                dust = Mathf.Lerp(0.3f, 1f, Hash01(seed, 0x297A2D39u)),
                mud = 0.2f + 0.8f * Mathf.Sqrt(Hash01(seed, 0x6A09E667u)),   // skewed high: most carpets can get properly grimy
                // a hashed jump into the noise, so consecutive seeds never get a shifted copy of the same blobs
                offset = new Vector2(Hash01(seed, 0xB5297A4Du), Hash01(seed, 0x1B56C4E9u)) * 211f,
            };
            // dried-mud cake on 4 carpets in 10, and only once they are well soiled; spills grow with the level
            bool crusty = Hash01(seed, 0xBB67AE85u) < 0.4f;
            p.crust = crusty ? Mathf.Lerp(0.25f, 0.9f, Hash01(seed, 0x3C6EF372u)) * Smooth(0.25f, 0.75f, p.level) : 0f;
            p.spill = p.level * Mathf.Lerp(0.15f, 1f, Hash01(seed, 0xA54FF53Au));
            if (forcedLevel < 0f)
            {
                p.dust = Mathf.Max(0.5f, p.dust);
                p.crust = Mathf.Max(0.2f, p.crust);
            }
            return p;
        }

        // HLSL smoothstep (Mathf.SmoothStep is an interpolation, not this)
        static float Smooth(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }

        // lowbias32 (C. Wellons) over seed and salt -> [0, 1), 24 bits
        static float Hash01(int seed, uint salt)
        {
            unchecked
            {
                uint x = (uint)seed * 0x9E3779B9u ^ salt;
                x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16;
                return (x >> 8) * (1f / 16777216f);
            }
        }

        /// <summary>Runs K_ClearStats + K_Reduce and blocks for the result.</summary>
        public SimStats ReadStatsSync()
        {
            if (!Ready) return default;
            DispatchReduce();
            var req = AsyncGPUReadback.Request(_statsBuf);
            req.WaitForCompletion();
            if (req.hasError) return default;
            var data = req.GetData<uint>();
            for (int i = 0; i < StatsLen && i < data.Length; i++) _statsRaw[i] = data[i];
            return StatsFromRaw(_statsRaw);
        }

        /// <summary>Synchronous stats refresh: reads, applies (Cleanliness/Wetness/Hint) and fires StatsUpdated.</summary>
        public SimStats RefreshStatsSync()
        {
            var s = ReadStatsSync();
            if (s.valid) ApplyStats(s);
            return s;
        }

        public void StepFrame(float dtFrame)
        {
            if (!Ready) return;
            dtFrame = Mathf.Clamp(dtFrame, 1e-4f, 1f / 30f * Mathf.Max(TimeScale, 1f) + 1e-3f);
            int nSub = Mathf.Clamp(Mathf.RoundToInt(dtFrame / DtSim), 1, 8);
            _simTime += dtFrame;
            UploadUniforms(dtFrame);
            UploadStamps();

            // 0. clear this frame's edge-runoff accumulators and the debris emission list
            compute.SetTexture(kClearEdge, ID_EdgeOut, _edgeOut);
            compute.SetTexture(kClearEdge, ID_EdgeOut2, _edgeOut2);
            compute.Dispatch(kClearEdge, SimW / 8, SimH / 8, 1);
            _emitBuf.SetCounterValue(0);

            // 1. stamps (in place on current buffers; rewrites _Agit / _ToolFlow, accumulates chip/dust into _FiberVel.zw)
            compute.SetTexture(kStamp, ID_FluidOut, _fluid[_curFluid]);
            compute.SetTexture(kStamp, ID_SoapOut, _soap[_curSoap]);
            compute.SetTexture(kStamp, ID_Dirt, _dirt);
            compute.SetTexture(kStamp, ID_Crust, _crust);
            compute.SetTexture(kStamp, ID_FiberOut, _fiber[_curFiber]);
            compute.SetTexture(kStamp, ID_FiberVel, _fiberVel);
            compute.SetTexture(kStamp, ID_Agit, _agit);
            compute.SetTexture(kStamp, ID_ToolFlow, _toolFlow);
            compute.SetBuffer(kStamp, ID_Stamps, _stampBuf);
            compute.Dispatch(kStamp, SimW / 8, SimH / 8, 1);

            // 1b. debris emission (chunks / dust / droplets) into the append buffer consumed by DebrisSystem
            compute.SetTexture(kEmit, ID_FiberVel, _fiberVel);
            compute.SetTexture(kEmit, ID_Agit, _agit);
            compute.SetTexture(kEmit, ID_FluidOut, _fluid[_curFluid]);
            compute.SetTexture(kEmit, ID_FiberOut, _fiber[_curFiber]);
            compute.SetTexture(kEmit, ID_Crust, _crust);
            compute.SetBuffer(kEmit, ID_Stamps, _stampBuf);
            compute.SetBuffer(kEmit, ID_EmitBuf, _emitBuf);
            compute.Dispatch(kEmit, SimW / 8, SimH / 8, 1);

            // 1c. squeegee plow (Docs/SQUEEGEE.md): after K_Stamp wrung / pressed the pile, before the water moves.
            // Each sweep = scatter (ping-pong fluid + soap, swap) + apply (in place). The drain pours the spill ring onto
            // the floor every frame, also after the stroke ended.
            int nPlow = 0;
            while (_plows.Count > 0)
            {
                var pw = _plows.Dequeue();
                if (CarpetAway || nPlow >= MaxPlowsPerFrame || !PlowReachesCarpet(pw)) continue;
                DispatchPlow(pw);
                nPlow++;
            }
            compute.SetBuffer(kPlowDrain, ID_PlowAcc, _plowAcc);
            compute.SetTexture(kPlowDrain, ID_EdgeOut, _edgeOut);
            compute.SetTexture(kPlowDrain, ID_EdgeOut2, _edgeOut2);
            compute.Dispatch(kPlowDrain, SimW / 8, SimH / 8, 1);

            // 2. substepped shallow water + transport (boundary cells accumulate runoff into _EdgeOut/_EdgeOut2)
            for (int s = 0; s < nSub; s++)
            {
                int fc = _curFluid, fn = 1 - _curFluid, sc = _curSoap, sn = 1 - _curSoap;
                compute.SetTexture(kFlux, ID_FluidIn, _fluid[fc]);
                compute.SetTexture(kFlux, ID_FiberOut, _fiber[_curFiber]);
                compute.SetTexture(kFlux, ID_Bed, _bed);
                compute.SetTexture(kFlux, ID_ToolFlow, _toolFlow);
                compute.SetTexture(kFlux, ID_Flux, _flux);
                compute.Dispatch(kFlux, SimW / 8, SimH / 8, 1);

                compute.SetTexture(kIntegrate, ID_FluidIn, _fluid[fc]);
                compute.SetTexture(kIntegrate, ID_Flux, _flux);
                compute.SetTexture(kIntegrate, ID_SoapIn, _soap[sc]);
                compute.SetTexture(kIntegrate, ID_FiberOut, _fiber[_curFiber]);
                compute.SetTexture(kIntegrate, ID_ToolFlow, _toolFlow);
                compute.SetTexture(kIntegrate, ID_FluidOut, _fluid[fn]);
                compute.SetTexture(kIntegrate, ID_SoapOut, _soap[sn]);
                compute.SetTexture(kIntegrate, ID_EdgeOut, _edgeOut);
                compute.SetTexture(kIntegrate, ID_EdgeOut2, _edgeOut2);
                compute.Dispatch(kIntegrate, SimW / 8, SimH / 8, 1);
                _curFluid = fn; _curSoap = sn;
            }

            // 3. chemistry (in place)
            compute.SetTexture(kChemistry, ID_Agit, _agit);
            compute.SetTexture(kChemistry, ID_Crust, _crust);
            compute.SetTexture(kChemistry, ID_FluidOut, _fluid[_curFluid]);
            compute.SetTexture(kChemistry, ID_SoapOut, _soap[_curSoap]);
            compute.SetTexture(kChemistry, ID_Dirt, _dirt);
            compute.SetTexture(kChemistry, ID_FiberOut, _fiber[_curFiber]);
            compute.Dispatch(kChemistry, SimW / 8, SimH / 8, 1);

            // 4. fibers (In -> Out, swap)
            {
                int fc = _curFiber, fn = 1 - _curFiber;
                compute.SetTexture(kFiber, ID_FiberIn, _fiber[fc]);
                compute.SetTexture(kFiber, ID_FluidOut, _fluid[_curFluid]);
                compute.SetTexture(kFiber, ID_Dirt, _dirt);
                compute.SetTexture(kFiber, ID_FiberVel, _fiberVel);
                compute.SetTexture(kFiber, ID_FiberOut, _fiber[fn]);
                compute.SetTexture(kFiber, ID_FiberSet, _fiberSet);
                compute.SetBuffer(kFiber, ID_Stamps, _stampBuf);
                compute.Dispatch(kFiber, SimW / 8, SimH / 8, 1);
                _curFiber = fn;
            }

            // 5. stats every 10 frames (async)
            _frameCounter++;
            if (_frameCounter % 10 == 0)
            {
                DispatchReduce();
                AsyncGPUReadback.Request(_statsBuf, OnStatsReadback);
            }

            // 6. globals
            SetGlobals();
        }

        void LateUpdate()
        {
            if (IsPaused || !Ready) return;
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f) * TimeScale;
            if (dt <= 0f) return;
            StepFrame(dt);
        }

        // ------------------------------------------------------------------ internals
        void DispatchPlow(in PlowSweep p)
        {
            compute.SetVector(ID_PlowA, new Vector4(p.p0.x, p.p0.y, p.p1.x, p.p1.y));
            compute.SetVector(ID_PlowB, new Vector4(p.a0.x, p.a0.y, p.a1.x, p.a1.y));
            compute.SetVector(ID_PlowC, new Vector4(p.sign < 0f ? -1f : 1f, p.radiusMm, p.halfLengthMm, 0f));
            int fc = _curFluid, fn = 1 - _curFluid, sc = _curSoap, sn = 1 - _curSoap;
            compute.SetTexture(kPlowScatter, ID_FluidIn, _fluid[fc]);
            compute.SetTexture(kPlowScatter, ID_SoapIn, _soap[sc]);
            compute.SetTexture(kPlowScatter, ID_FiberIn, _fiber[_curFiber]);
            compute.SetTexture(kPlowScatter, ID_FluidOut, _fluid[fn]);
            compute.SetTexture(kPlowScatter, ID_SoapOut, _soap[sn]);
            compute.SetBuffer(kPlowScatter, ID_PlowAcc, _plowAcc);
            compute.Dispatch(kPlowScatter, SimW / 8, SimH / 8, 1);
            _curFluid = fn; _curSoap = sn;

            compute.SetTexture(kPlowApply, ID_FluidOut, _fluid[_curFluid]);
            compute.SetTexture(kPlowApply, ID_SoapOut, _soap[_curSoap]);
            compute.SetTexture(kPlowApply, ID_FiberOut, _fiber[_curFiber]);   // pushed water soaks into the pile ahead first
            compute.SetBuffer(kPlowApply, ID_PlowAcc, _plowAcc);
            compute.Dispatch(kPlowApply, SimW / 8, SimH / 8, 1);
        }

        /// <summary>False when the blade, its roll and the look-ahead are all off the carpet: the full-grid pass would only copy.</summary>
        bool PlowReachesCarpet(in PlowSweep p)
        {
            float look = settings != null ? Mathf.Max(settings.SQ_LOOKAHEAD, 0f) : 300f;
            float reach = p.halfLengthMm + p.radiusMm + look + 20f;
            var rect = new Rect(0f, 0f, CarpetWidthMm, CarpetHeightMm);
            return DistanceToRect(p.p0, rect) <= reach || DistanceToRect(p.p1, rect) <= reach;
        }

        static float DistanceToRect(Vector2 q, Rect r)
        {
            float dx = Mathf.Max(Mathf.Max(r.xMin - q.x, q.x - r.xMax), 0f);
            float dy = Mathf.Max(Mathf.Max(r.yMin - q.y, q.y - r.yMax), 0f);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // spacingMm per ToolId packed 4 per float4 (HLSL float arrays pad every element to 16 bytes); K_Fiber sweeps each
        // moving stamp over exactly this distance for the fiber set (Docs/FIBER_SET.md). Refilled on every upload: an AREA
        // upgrade widens a disc and its stamp spacing together (ToolTuning.Effective, Docs/ECONOMY.md 2).
        readonly Vector4[] _toolSpacing = new Vector4[4];
        // POWER upgrades: (P - 1) per ToolId, read by ToolPower() in DebrisCommon.hlsl
        readonly Vector4[] _toolPower = new Vector4[4];

        void UploadUniforms(float dtFrame)
        {
            ToolTuning.FillSpacing(_toolSpacing);
            ToolTuning.FillPower(_toolPower);
            compute.SetVectorArray("_ToolSpacing", _toolSpacing);
            compute.SetVectorArray("_ToolPower", _toolPower);
            compute.SetInt("_SimW", SimW);
            compute.SetInt("_SimH", SimH);
            compute.SetFloat("_DtSim", DtSim);
            compute.SetFloat("_DtFrame", dtFrame);
            compute.SetFloat("_Time", _simTime);
            compute.SetInt("_Seed", seed);
            if (settings != null) settings.Apply(compute);
            if (floorSettings != null) floorSettings.Apply(compute);
            // Grid spacing is geometry, never a tunable: old serialized assets must not use the old cell area.
            compute.SetFloat("_DX", CellSizeMm);
        }

        void UploadStamps()
        {
            int n = 0;
            while (n < MaxStamps && _pending.Count > 0) _stampArr[n++] = _pending.Dequeue();
            if (n > 0) _stampBuf.SetData(_stampArr, 0, 0, n);
            // the carpet itself ignores every stamp while it is away; _stampCount (floor + debris) still reports them
            compute.SetInt("_StampCount", CarpetAway ? 0 : n);
            _stampCount = n;
        }

        void DispatchReduce()
        {
            compute.SetBuffer(kClearStats, ID_StatsBuf, _statsBuf);
            compute.Dispatch(kClearStats, 1, 1, 1);
            compute.SetTexture(kReduce, ID_Dirt, _dirt);
            compute.SetTexture(kReduce, ID_FluidOut, _fluid[_curFluid]);
            compute.SetTexture(kReduce, ID_SoapOut, _soap[_curSoap]);
            compute.SetTexture(kReduce, ID_FiberOut, _fiber[_curFiber]);
            compute.SetBuffer(kReduce, ID_StatsBuf, _statsBuf);
            compute.Dispatch(kReduce, SimW / 8, SimH / 8, 1);
        }

        void OnStatsReadback(AsyncGPUReadbackRequest req)
        {
            if (!_allocated || _statsBuf == null || req.hasError) return;
            var data = req.GetData<uint>();
            for (int i = 0; i < StatsLen && i < data.Length; i++) _statsRaw[i] = data[i];
            ApplyStats(StatsFromRaw(_statsRaw));
        }

        static SimStats StatsFromRaw(uint[] raw)
        {
            const float inv = 1f / (256f * CellCount);
            var s = new SimStats
            {
                valid = true,
                crust = raw[0] * inv, mud = raw[1] * inv, stain = raw[2] * inv, grit = raw[3] * inv,
                slurry = raw[4] * inv, foamDirt = raw[5] * inv, water = raw[6] * inv, pileWater = raw[7] * inv,
                soap = raw[8] * inv, foam = raw[9] * inv, wetCells = raw[10] * inv, pooledCells = raw[11] * inv,
                dustCells = raw[12] * inv, crustCells = raw[13] * inv
            };
            return s;
        }

        public static float DirtScore(in SimStats s)
        {
            return 1.0f * s.crust + 0.8f * s.mud + 0.6f * s.stain + 0.9f * s.grit + 0.7f * s.slurry + 0.5f * s.foamDirt;
        }

        void ApplyStats(SimStats s)
        {
            _stats = s;
            if (!s.valid) return;
            float wcap = settings != null ? settings.W_CAP : 6f;
            float dirtInit = DirtScore(_initStats);
            Cleanliness = dirtInit > 1e-6f ? Mathf.Clamp01(1f - DirtScore(s) / dirtInit) : 1f;
            Wetness = Mathf.Clamp01(s.pileWater + s.water / wcap);
            SoapLeft = s.soap / 0.5f;
            FoamMean = s.foam;
            IsWon = Cleanliness >= 0.97f && Wetness <= 0.05f && FoamMean <= 0.02f;
            // inspection lamp: from this very snapshot (its histograms are in _statsRaw), so the lamp goes dark in the frame
            // the bar turns 100 % (Docs/HIGHLIGHTS.md 4). keep = the share of what is left that the bar tolerates at 100 %.
            float wetRaw = s.pileWater + s.water / wcap;
            LampCutDirt = ShownPercent(Cleanliness) >= 100 ? NoCut
                        : WaterLevel(0, (1f - ShownFull) * dirtInit / Mathf.Max(DirtScore(s), 1e-12f));
            LampCutWet = ShownPercent(1f - Wetness) >= 100 ? NoCut
                       : WaterLevel(1, (1f - ShownFull) / Mathf.Max(wetRaw, 1e-12f));
            Shader.SetGlobalVector(ID_HighlightCut, new Vector4(LampCutDirt, LampCutWet, 0f, 0f));
            UpdateHint(s);
            StatsUpdated?.Invoke(s);
        }

        /// <summary>
        /// The lamp's level for one measure (0 dirt, 1 wet): the value a at which the carpet with every cell cut down to a
        /// (the sum of min(value, a)) holds `keep` of what it holds now, i.e. just what the bar tolerates at 100 %. Cleaning /
        /// drying every cell down to a therefore brings the bar to exactly 100 %, and the shaders light what a cell holds above
        /// a: a fully soiled carpet gets a level near 0 (everything glows, no holes), and the level climbs past the last spot
        /// as the bar reaches 100 %. keep is capped below 1, so a bar under 100 % always leaves something lit. The sum is
        /// exact at the bin edges and linear in between.
        /// </summary>
        float WaterLevel(int which, float keep)
        {
            int massAt = StatsHist0 + which * HistBins, countAt = StatsHist0 + (2 + which) * HistBins;
            double total = 0.0, cells = 0.0;
            for (int b = 0; b < HistBins; b++) { total += _statsRaw[massAt + b] / HistScale; cells += _statsRaw[countAt + b]; }
            if (total <= 0.0 || cells <= 0.0) return NoCut;
            double allowed = Mathf.Clamp(keep, 0f, 0.999f) * total;
            // f(e_k) = the whole of every bin below edge e_k + e_k for every cell from bin k up; f(0) = 0
            double massBelow = 0.0, cellsFrom = cells, ePrev = 0.0, fPrev = 0.0;
            for (int k = 0; k <= HistBins; k++)
            {
                double e = Math.Pow(2.0, HistLo + k / (double)HistPerOct);
                double f = massBelow + e * cellsFrom;
                if (f >= allowed)
                {
                    double t = f > fPrev ? (allowed - fPrev) / (f - fPrev) : 0.0;
                    return (float)(ePrev + t * (e - ePrev));
                }
                if (k == HistBins) break;
                massBelow += _statsRaw[massAt + k] / HistScale;
                cellsFrom -= _statsRaw[countAt + k];
                ePrev = e; fPrev = f;
            }
            return NoCut;
        }

        /// <summary>Summed amount per histogram (0 dirt, 1 wet) as a per-cell mean: checks K_Reduce against the stats.</summary>
        public float HistMean(int which)
        {
            double total = 0.0;
            int offset = StatsHist0 + (which == 0 ? 0 : HistBins);
            for (int b = 0; b < HistBins; b++) total += _statsRaw[offset + b];
            return (float)(total / (HistScale * CellCount));
        }

        void UpdateHint(in SimStats s)
        {
            float crustR = _initStats.crust > 1e-6f ? s.crust / _initStats.crust : 0f;
            float mudR = _initStats.mud > 1e-6f ? s.mud / _initStats.mud : 0f;
            float stainR = _initStats.stain > 1e-6f ? s.stain / _initStats.stain : 0f;
            float gritR = _initStats.grit > 1e-4f ? s.grit / _initStats.grit : 0f;   // a new carpet can start dusty (Docs/DIRT_LEVEL.md)
            if (crustR > 0.15f) { SuggestedTool = ToolId.Scraper; Hint = "Scrape the dry crust off"; }
            else if (s.grit > 0.04f || gritR > 0.2f) { SuggestedTool = ToolId.DryVacuum; Hint = "Vacuum up the dust and crumbs"; }
            else if (mudR > 0.15f && Wetness < 0.25f) { SuggestedTool = ToolId.Hose; Hint = "Wet it down"; }
            else if (mudR > 0.15f) { SuggestedTool = ToolId.Brush; Hint = "Brush the mud loose"; }
            else if (stainR > 0.2f && SoapLeft < 0.2f) { SuggestedTool = ToolId.FoamSprayer; Hint = "Spray detergent on the stains"; }
            else if (stainR > 0.2f) { SuggestedTool = ToolId.Brush; Hint = "Scrub the foam into the stains"; }
            else if (s.slurry > 0.02f && s.pooledCells > 0.05f) { SuggestedTool = ToolId.Squeegee; Hint = "Squeegee the dirty water off"; }
            else if (s.slurry > 0.01f || FoamMean > 0.3f || Wetness > 0.35f) { SuggestedTool = ToolId.WetVacuum; Hint = "Wet-vac the water and foam out"; }
            else if (SoapLeft > 0.25f) { SuggestedTool = ToolId.Hose; Hint = "Rinse the soap out"; }
            else if (Wetness > 0.05f) { SuggestedTool = ToolId.Dryer; Hint = "Dry it"; }
            else { SuggestedTool = ToolId.Dryer; Hint = "Sparkling clean!"; }
        }

        void SetGlobals()
        {
            if (!_allocated) return;
            Shader.SetGlobalTexture("_FluidTex", _fluid[_curFluid]);
            Shader.SetGlobalTexture("_SoapTex", _soap[_curSoap]);
            Shader.SetGlobalTexture("_DirtTex", _dirt);
            Shader.SetGlobalTexture("_CrustTex", _crust);
            Shader.SetGlobalTexture("_FiberTex", _fiber[_curFiber]);
            Shader.SetGlobalTexture("_BedTex", _bed);
            Shader.SetGlobalMatrix("_CarpetToWorld", transform.localToWorldMatrix);
            if (_noise != null) Shader.SetGlobalTexture(ID_NoiseTex, _noise);
            Shader.SetGlobalVector("_SimTexel", new Vector4(1f / SimW, 1f / SimH, SimW, SimH));
            Shader.SetGlobalVector("_CarpetMm", new Vector4(CarpetWidthMm, CarpetHeightMm, 1f / CarpetWidthMm, 1f / CarpetHeightMm));
            float wcap = 6f, pileH = 20f, fmax = 12f, hmax = 10f;
            if (settings != null) { wcap = settings.W_CAP; pileH = settings.PILE_H; fmax = settings.F_MAX; hmax = settings.H_MAX; }
            Shader.SetGlobalVector("_SimConsts", new Vector4(wcap, pileH, fmax, hmax));
            // CarpetWater takes its normal from K_Flux's head (bed + BED_PILE (pileTop - PILE_H) + h), not from the pile step (Docs/WET_LOOK.md 5)
            Shader.SetGlobalVector("_SimConsts2", new Vector4(settings != null ? settings.BED_PILE : 0.3f, 0f, 0f, 0f));
            Vector3 rim = new Vector3(0.35f, 0.55f, 0.75f).normalized;
            Shader.SetGlobalVector("_RimDir", new Vector4(rim.x, rim.y, rim.z, 0f));
            Shader.SetGlobalVector("_RimColor", new Vector4(0.55f * 0.6f, 0.65f * 0.6f, 0.85f * 0.6f, 1f));
            Shader.SetGlobalVector(ID_HighlightCut, new Vector4(LampCutDirt, LampCutWet, 0f, 0f));
        }
    }
}
