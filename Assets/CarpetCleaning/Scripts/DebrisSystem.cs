using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    /// <summary>
    /// One chunk captured by a vacuum nozzle (Debris.compute, the SUCKED transition). ToolAudio turns it into the
    /// crumb-in-the-tube rattle, Docs/AUDIO.md 8.
    /// </summary>
    public struct DebrisSuckEvent
    {
        public int particle;        // pool index, for tests that follow the chunk to its death
        public float sizeMm;
        /// <summary>Real seconds from capture until the chunk vanishes into the nozzle (sim seconds / TimeScale).</summary>
        public float arrivalS;
        public ToolId tool;         // DryVacuum or WetVacuum
        /// <summary>AudioTap.DspTime (AudioSettings.dspTime, or a capture take's virtual clock) when the capturing step was dispatched: the clock arrivalS counts from.</summary>
        public double captureDsp;
    }

    /// <summary>GPU debris particles (crust chunks, dust puffs, splash droplets) — Docs/FLOOR_SYSTEM.md section 3.
    /// LateUpdate order 105: after CarpetSimulation (100, fills the emit append buffer) and before FloorSimulation (110, consumes the
    /// deposits written into its inject buffer). Runs K_DebSpawn + K_DebSim on Compute/Debris.compute and draws the pool with three
    /// Graphics.RenderMeshIndirect calls (no GameObjects): chunk meshes, dust billboards and drop motion streaks (the last two
    /// use one unit quad mesh, DebrisMeshBuilder.BuildQuad; Docs/DEBRIS_SYSTEM.md). Works without a floor (no floor sampling / deposits).</summary>
    [DefaultExecutionOrder(105)]
    public sealed class DebrisSystem : MonoBehaviour
    {
        public const int ParticleCount = FloorConsts.ParticleCount;   // 16384
        public const int EmitCapacity = FloorConsts.EmitCapacity;     // 8192
        const int ThreadGroup = 64;

        public ComputeShader compute;
        public FloorSettings settings;          // D_* constants; falls back to carpet.floorSettings when null
        public CarpetSimulation carpet;
        public FloorSimulation floor;           // optional
        public Transform carpetRoot;            // frame of the particle CMM space; falls back to carpet.transform
        public Material chunkMaterial, dustMaterial, dropMaterial;

        ComputeBuffer _particles, _head, _emitCount, _dummyInject, _suckEvents;
        GraphicsBuffer _argsChunk, _argsDust, _argsDrop;
        Mesh _chunkMesh, _dropMesh, _dustMesh;
        RenderTexture _blackRT;
        int kInit = -1, kSpawn = -1, kSim = -1, kClearEvents = -1;
        bool _allocated, _subscribed, _loggedNoCompute, _killRectPending;
        CarpetSimulation _subscribedTo;
        float _lastSimTime = -1f;
        readonly uint[] _headRaw = new uint[1];

        public ComputeBuffer Particles => _particles;
        public ComputeBuffer HeadBuffer => _head;
        public bool Ready => _allocated && compute != null;
        /// <summary>Total particles spawned since the last reset, as of the last synchronous readback.</summary>
        public int LastSpawnedTotal { get; private set; }

        // ---- suction events (Docs/AUDIO.md 8); layout must match Debris.compute SUCK_EV_*
        public const int SuckEventMax = 64;
        const int SuckEventStride = 4, SuckEventHeader = 2;

        /// <summary>Raised on the main thread for every chunk a vacuum captured, 1-3 frames after the capture.</summary>
        public event Action<DebrisSuckEvent> DebrisSucked;
        /// <summary>Events delivered since the last reset.</summary>
        public int SuckEventsReceived { get; private set; }
        /// <summary>Captures beyond SuckEventMax in a single step: counted, not delivered.</summary>
        public int SuckEventsOverflow { get; private set; }
        /// <summary>The GPU's own running total ([1]) as of the newest readback; equals Received + Overflow when nothing was lost.</summary>
        public int SuckEventsGpuTotal { get; private set; }
        int _suckGeneration;           // bumped on reset, so a readback issued before it cannot count into the new run

        static readonly int ID_Particles = Shader.PropertyToID("_Particles");
        static readonly int ID_Head = Shader.PropertyToID("_Head");
        static readonly int ID_EmitIn = Shader.PropertyToID("_EmitIn");
        static readonly int ID_EmitCount = Shader.PropertyToID("_EmitCount");
        static readonly int ID_Stamps = Shader.PropertyToID("_Stamps");
        static readonly int ID_FInject = Shader.PropertyToID("_FInject");
        static readonly int ID_FiberTex = Shader.PropertyToID("_FiberTex");
        static readonly int ID_FluidTex = Shader.PropertyToID("_FluidTex");
        static readonly int ID_FFluidTex = Shader.PropertyToID("_FFluidTex");
        static readonly int ID_StampCount = Shader.PropertyToID("_StampCount");
        static readonly int ID_FloorValid = Shader.PropertyToID("_FloorValid");
        static readonly int ID_DtFrame = Shader.PropertyToID("_DtFrame");
        static readonly int ID_Time = Shader.PropertyToID("_Time");
        static readonly int ID_SimConsts = Shader.PropertyToID("_SimConsts");
        static readonly int ID_KindFilter = Shader.PropertyToID("_KindFilter");
        static readonly int ID_CarpetToWorld = Shader.PropertyToID("_CarpetToWorld");
        static readonly int ID_KillInCarpet = Shader.PropertyToID("_KillInCarpet");
        static readonly int ID_CarpetAway = Shader.PropertyToID("_CarpetAway");
        static readonly int ID_SuckEvents = Shader.PropertyToID("_SuckEvents");

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            Allocate();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void Start()
        {
            if (Ready) ResetDebris();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void OnDestroy()
        {
            Unsubscribe();
            _suckGeneration++;                                            // before the drain: its callbacks must find a stale generation
            AsyncGPUReadback.WaitAllRequests();
            ReleaseBuffer(ref _particles); ReleaseBuffer(ref _head); ReleaseBuffer(ref _emitCount); ReleaseBuffer(ref _dummyInject);
            ReleaseBuffer(ref _suckEvents);
            ReleaseGB(ref _argsChunk); ReleaseGB(ref _argsDust); ReleaseGB(ref _argsDrop);
            if (_chunkMesh != null) { Destroy(_chunkMesh); _chunkMesh = null; }
            if (_dropMesh != null && _dropMesh != _dustMesh) Destroy(_dropMesh);  // normally the same quad as the dust
            _dropMesh = null;
            if (_dustMesh != null) { Destroy(_dustMesh); _dustMesh = null; }
            if (_blackRT != null) { _blackRT.Release(); Destroy(_blackRT); _blackRT = null; }
            _allocated = false;
        }

        static void ReleaseBuffer(ref ComputeBuffer b) { if (b != null) { b.Release(); b = null; } }
        static void ReleaseGB(ref GraphicsBuffer b) { if (b != null) { b.Release(); b = null; } }

        void Subscribe()
        {
            if (_subscribed || carpet == null) return;
            carpet.CarpetReset += OnCarpetReset;
            _subscribedTo = carpet;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_subscribedTo != null) _subscribedTo.CarpetReset -= OnCarpetReset;
            _subscribedTo = null;
            _subscribed = false;
        }

        void OnCarpetReset() { ResetDebris(); }

        void Allocate()
        {
            if (compute == null)
            {
                if (!_loggedNoCompute) { Debug.LogError("[DebrisSystem] compute shader not assigned; debris disabled.", this); _loggedNoCompute = true; }
                return;
            }
            _particles = new ComputeBuffer(ParticleCount, DebrisParticle.Size, ComputeBufferType.Structured);
            _head = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);
            _emitCount = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);   // CopyCount target, read as StructuredBuffer<uint>
            _dummyInject = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.Structured); // bound when there is no floor (kernel skips deposits)
            _dummyInject.SetData(new uint[4]);
            _head.SetData(new uint[1]);
            _emitCount.SetData(new uint[1]);
            _suckEvents = new ComputeBuffer(SuckEventHeader + SuckEventMax * SuckEventStride, sizeof(uint), ComputeBufferType.Structured);
            _suckEvents.SetData(new uint[SuckEventHeader + SuckEventMax * SuckEventStride]);

            _chunkMesh = DebrisMeshBuilder.BuildChunk();
            _dustMesh = DebrisMeshBuilder.BuildQuad();                  // dust billboards and drop streaks: both expand the quad in their vertex shaders
            _dropMesh = _dustMesh;                                      // drops are screen-space motion streaks (DebrisDrop.shader)
            _chunkMesh.hideFlags = _dustMesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _argsChunk = MakeArgs(_chunkMesh);
            _argsDust = MakeArgs(_dustMesh);
            _argsDrop = MakeArgs(_dropMesh);

            _blackRT = new RenderTexture(1, 1, 0, GraphicsFormat.R32G32B32A32_SFloat)
            {
                name = "DebrisNoFloor", enableRandomWrite = false, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };
            _blackRT.Create();
            var prev = RenderTexture.active;
            RenderTexture.active = _blackRT;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = prev;

            compute.SetInt(ID_KillInCarpet, 0);
            kInit = compute.FindKernel("K_DebInit");
            kSpawn = compute.FindKernel("K_DebSpawn");
            kSim = compute.FindKernel("K_DebSim");
            kClearEvents = compute.FindKernel("K_DebClearEvents");
            _allocated = true;
        }

        static GraphicsBuffer MakeArgs(Mesh mesh)
        {
            var args = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            var data = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            data[0].indexCountPerInstance = mesh.GetIndexCount(0);
            data[0].instanceCount = (uint)ParticleCount;
            data[0].startIndex = 0;
            data[0].baseVertexIndex = 0;
            data[0].startInstance = 0;
            args.SetData(data);
            return args;
        }

        // ------------------------------------------------------------------ public API
        /// <summary>Kills every particle and zeroes the ring head (K_DebInit). Called on Start and on CarpetSimulation.CarpetReset.</summary>
        public void ResetDebris()
        {
            if (!Ready) return;
            compute.SetBuffer(kInit, ID_Particles, _particles);
            compute.SetBuffer(kInit, ID_Head, _head);
            compute.Dispatch(kInit, ParticleCount / ThreadGroup, 1, 1);
            _lastSimTime = carpet != null ? carpet.SimTime : 0f;
            LastSpawnedTotal = 0;
            if (_suckEvents != null) _suckEvents.SetData(new uint[SuckEventHeader + SuckEventMax * SuckEventStride]);
            _suckGeneration++;
            SuckEventsReceived = 0; SuckEventsOverflow = 0; SuckEventsGpuTotal = 0;
        }

        /// <summary>
        /// One-shot: kills every particle whose CMM footprint is inside the carpet rect. Used when the carpet is carried
        /// off (its crumbs go with it instead of hanging in mid-air over the bare tile) and again when a new carpet is
        /// laid down (nothing gets buried in the fresh pile). Docs/HANDOVER.md.
        /// Dispatched with dt = 0, so every surviving particle early-outs and nothing else about the frame changes.
        /// </summary>
        public void KillCarpetRect()
        {
            if (!Ready || carpet == null || !carpet.Ready) { _killRectPending = true; return; }
            compute.SetInt(ID_KillInCarpet, 1);
            compute.SetFloat(ID_DtFrame, 0f);
            DispatchSim();
            compute.SetInt(ID_KillInCarpet, 0);
            _killRectPending = false;
        }

        /// <summary>Blocking readback of the pool: live particles, and how many of them sit inside the carpet rect.</summary>
        public int CountAliveSync(out int insideRect)
        {
            return CountAliveSync(out insideRect, null, out _);
        }

        /// <summary>
        /// As above, plus live particles per kind (kinds[0..2] = chunk, dust, drop, when the array is given) and the splash
        /// drops in the air (kind 2 higher than 25 mm: above the pile top), for the splash tests (Docs/DEBRIS_SYSTEM.md).
        /// </summary>
        public int CountAliveSync(out int insideRect, int[] kinds, out int dropsAir)
        {
            insideRect = 0; dropsAir = 0;
            if (kinds != null) Array.Clear(kinds, 0, kinds.Length);
            if (!Ready || _particles == null) return 0;
            var req = AsyncGPUReadback.Request(_particles);
            req.WaitForCompletion();
            if (req.hasError) return 0;
            var data = req.GetData<DebrisParticle>();
            int alive = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (((data[i].kindState >> 8) & 0xFFu) == 0u) continue;      // dead
                alive++;
                Vector3 p = data[i].pos;                                     // (CMM x, height mm, CMM y)
                if (p.x >= 0f && p.x <= CarpetSimulation.CarpetWidthMm && p.z >= 0f && p.z <= CarpetSimulation.CarpetHeightMm) insideRect++;
                int kind = (int)(data[i].kindState & 0xFFu);
                if (kinds != null && kind < kinds.Length) kinds[kind]++;
                if (kind == 2 && p.y > 25f) dropsAir++;
            }
            return alive;
        }

        /// <summary>Blocking readback of _Head[0] = total particles spawned since the last reset (ring head, not the live count).</summary>
        public int ReadSpawnedTotalSync()
        {
            if (!Ready || _head == null) return 0;
            var req = AsyncGPUReadback.Request(_head);
            req.WaitForCompletion();
            if (req.hasError) return LastSpawnedTotal;
            var data = req.GetData<uint>();
            if (data.Length > 0) _headRaw[0] = data[0];
            LastSpawnedTotal = (int)Math.Min(_headRaw[0], int.MaxValue);
            return LastSpawnedTotal;
        }

        // ------------------------------------------------------------------ frame
        void LateUpdate()
        {
            if (carpet == null || !carpet.Ready) return;
            if (!_allocated) { Allocate(); if (!_allocated) return; }
            Subscribe();

            // the carpet advanced its sim time this frame -> exactly its dtFrame; otherwise (paused) only re-draw
            float simTime = carpet.SimTime;
            float dt = simTime - _lastSimTime;
            if (_lastSimTime < 0f || dt < 0f) dt = 0f;
            _lastSimTime = simTime;
            if (dt > 0f) Step(dt, simTime);
            Render();
        }

        /// <summary>Explicit scripted step (CarpetDebugApi): call right after carpet.StepFrame(dt) and before FloorSimulation.StepFrame.</summary>
        public void StepFrame(float dt)
        {
            if (carpet == null || !carpet.Ready || dt <= 0f) return;
            if (!_allocated) { Allocate(); if (!_allocated) return; }
            Subscribe();
            Step(dt, carpet.SimTime);
            _lastSimTime = carpet.SimTime;
        }

        void Step(float dt, float simTime)
        {
            ComputeBuffer.CopyCount(carpet.EmitBuffer, _emitCount, 0);
            UploadUniforms(dt, simTime);
            DispatchSpawn();
            if (_killRectPending) compute.SetInt(ID_KillInCarpet, 1);        // queued before the system was ready
            DispatchClearEvents();
            DispatchSim();
            RequestSuckEvents(dt);
            if (_killRectPending) { compute.SetInt(ID_KillInCarpet, 0); _killRectPending = false; }
        }

        void DispatchClearEvents()
        {
            compute.SetBuffer(kClearEvents, ID_SuckEvents, _suckEvents);
            compute.Dispatch(kClearEvents, 1, 1, 1);
        }

        /// <summary>
        /// Async readback of this step's suction events. GPU commands run in order, so the copy sees exactly this
        /// step's K_DebSim writes and the next step's K_DebClearEvents cannot race it. The dsp clock is captured
        /// here, at dispatch, because the arrival times count from this step, not from when the data comes back.
        /// </summary>
        void RequestSuckEvents(float dt)
        {
            if (_suckEvents == null) return;
            double captureDsp = AudioTap.DspTime;   // the clock ToolAudio schedules on: real, or a capture take's virtual one
            float timeScale = carpet != null ? Mathf.Max(carpet.TimeScale, 1e-3f) : 1f;
            int generation = _suckGeneration;
            int clockEpoch = AudioTap.ClockEpoch;   // which clock captureDsp is on (changes only when a take starts / ends)
            AsyncGPUReadback.Request(_suckEvents, req => OnSuckEvents(req, captureDsp, clockEpoch, timeScale, generation));
        }

        void OnSuckEvents(AsyncGPUReadbackRequest req, double captureDsp, int clockEpoch, float timeScale, int generation)
        {
            if (this == null || !_allocated || generation != _suckGeneration || req.hasError) return;
            var d = req.GetData<uint>();
            if (d.Length < SuckEventHeader) return;
            int count = (int)Math.Min(d[0], int.MaxValue);
            int n = Math.Min(count, SuckEventMax);
            SuckEventsGpuTotal = Math.Max(SuckEventsGpuTotal, (int)Math.Min(d[1], int.MaxValue));   // callbacks can arrive out of order
            SuckEventsReceived += n;
            SuckEventsOverflow += count - n;
            // stamped on another audio clock (a capture take's virtual one began or ended since): its captureDsp would
            // schedule the rattle ~1000 s off, or into the next take's timeline. Counted, not delivered. Never in the game.
            if (clockEpoch != AudioTap.ClockEpoch) return;
            var handler = DebrisSucked;
            if (handler == null) return;
            for (int k = 0; k < n; k++)
            {
                int b = SuckEventHeader + k * SuckEventStride;
                handler(new DebrisSuckEvent
                {
                    particle = (int)d[b],
                    sizeMm = BitConverter.Int32BitsToSingle((int)d[b + 1]),
                    arrivalS = BitConverter.Int32BitsToSingle((int)d[b + 2]) / timeScale,
                    tool = (ToolId)(int)d[b + 3],
                    captureDsp = captureDsp
                });
            }
        }

        void UploadUniforms(float dt, float simTime)
        {
            compute.SetFloat(ID_DtFrame, dt);
            compute.SetFloat(ID_Time, simTime);
            float wcap = 6f, pileH = 20f, fmax = 12f, hmax = 10f;
            var ss = carpet.settings;
            if (ss != null) { wcap = ss.W_CAP; pileH = ss.PILE_H; fmax = ss.F_MAX; hmax = ss.H_MAX; }
            compute.SetVector(ID_SimConsts, new Vector4(wcap, pileH, fmax, hmax));
            // while the carpet is away the rect is tile: chunks must fall to floor height inside it, not onto a ghost pile
            compute.SetInt(ID_CarpetAway, carpet.CarpetAway ? 1 : 0);
            var fs = settings != null ? settings : carpet.floorSettings;
            if (fs != null) fs.Apply(compute);
        }

        void DispatchSpawn()
        {
            compute.SetBuffer(kSpawn, ID_Particles, _particles);
            compute.SetBuffer(kSpawn, ID_Head, _head);
            compute.SetBuffer(kSpawn, ID_EmitIn, carpet.EmitBuffer);
            compute.SetBuffer(kSpawn, ID_EmitCount, _emitCount);
            compute.Dispatch(kSpawn, (EmitCapacity + ThreadGroup - 1) / ThreadGroup, 1, 1);
        }

        void DispatchSim()
        {
            bool floorOk = floor != null && floor.Ready && floor.Fluid != null && floor.InjectBuffer != null;
            compute.SetBuffer(kSim, ID_Particles, _particles);
            compute.SetBuffer(kSim, ID_Stamps, carpet.StampBuffer);
            compute.SetInt(ID_StampCount, carpet.StampCount);
            compute.SetTexture(kSim, ID_FiberTex, carpet.Fiber);
            compute.SetTexture(kSim, ID_FluidTex, carpet.Fluid);
            compute.SetTexture(kSim, ID_FFluidTex, floorOk ? floor.Fluid : _blackRT);
            compute.SetBuffer(kSim, ID_FInject, floorOk ? floor.InjectBuffer : _dummyInject);
            compute.SetBuffer(kSim, ID_SuckEvents, _suckEvents);
            compute.SetInt(ID_FloorValid, floorOk ? 1 : 0);
            compute.Dispatch(kSim, ParticleCount / ThreadGroup, 1, 1);
        }

        // What is queued this frame, per kind (chunk, dust, drop). A RenderMeshIndirect call stays queued until the next
        // player-loop frame starts, and every camera rendered before then draws it: the Game view, and a manual Camera.Render
        // from an eval after that frame (StoreShotApi) too. Evals between frames share one Time.frameCount.
        int _queuedFrame = -1;
        readonly Material[] _queuedMat = new Material[3];

        /// <summary>
        /// Makes sure the particle draws are queued for a manual Camera.Render outside the player loop (StoreShotApi captures).
        /// Queues only the kinds that are not queued yet with their current material this frame: the draw LateUpdate made
        /// is still live, and a second copy would draw every drop and puff twice (alpha compounds; Docs/DEBRIS_SYSTEM.md,
        /// capture trap). A kind whose material was null at LateUpdate (the flush recipe) is queued here once.
        /// </summary>
        public void SubmitDrawsForCapture()
        {
            if (carpet == null || !carpet.Ready || !_allocated) return;
            Render();
        }

        void Render()
        {
            if (_queuedFrame != Time.frameCount) { _queuedFrame = Time.frameCount; Array.Clear(_queuedMat, 0, _queuedMat.Length); }
            Transform root = carpetRoot != null ? carpetRoot : carpet.transform;
            Matrix4x4 m = root.localToWorldMatrix;
            Vector3 centre = m.GetColumn(3);
            var bounds = new Bounds(centre, new Vector3(20f, 20f, 20f));
            Draw(chunkMaterial, _chunkMesh, _argsChunk, 0, m, bounds);
            Draw(dropMaterial, _dropMesh, _argsDrop, 2, m, bounds);
            Draw(dustMaterial, _dustMesh, _argsDust, 1, m, bounds);
        }

        void Draw(Material mat, Mesh mesh, GraphicsBuffer args, int kind, Matrix4x4 carpetToWorld, Bounds bounds)
        {
            if (mat == null || mesh == null || args == null) return;
            if (_queuedMat[kind] == mat) return;                        // already queued with this material this frame
            _queuedMat[kind] = mat;
            mat.SetBuffer(ID_Particles, _particles);
            mat.SetInt(ID_KindFilter, kind);
            mat.SetMatrix(ID_CarpetToWorld, carpetToWorld);
            var rp = new RenderParams(mat)
            {
                worldBounds = bounds,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                layer = 0,
                lightProbeUsage = LightProbeUsage.Off,
                reflectionProbeUsage = ReflectionProbeUsage.Off,
                motionVectorMode = MotionVectorGenerationMode.ForceNoMotion
            };
            Graphics.RenderMeshIndirect(rp, mesh, args, 1);
        }
    }
}
