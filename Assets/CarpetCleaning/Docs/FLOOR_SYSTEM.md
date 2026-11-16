# Floor + debris system — design contract (2026-09-19)

> Status: implemented and verified in Play mode the same day (see `SESSION_LOG.md` for numbers/screenshots and the tuning that changed the defaults below:
> F_H_REST is now 1.2 mm, D_CHIP_UP 1.5, chunk launch 0.9..2.2x stroke speed, D_CHIPS_PER_UNIT 0.12, D_DUST_PER_UNIT 0.06, chunk size 4..12 mm, D_FRICTION 4 (x3 on the pile), palette darkened).
> Implementation notes and deliberate deviations: `FLOOR_SIM_RENDER_NOTES.md` (lane A), `DEBRIS_SYSTEM.md` (lane B). `FloorSettings.cs` is the source of truth for constants.

User request: add a real floor; crust chunks scraped off the carpet scatter over the floor; water and foam
spill onto the floor when tools push them over the edge or spray past it; every dirt interaction leaves
something on the floor (dust, chunks, dirty water rings). Realistic look. Tools must also work ON the floor
(hose, squeegee, wet/dry vacuum, dryer, brush, foam) so the mess can be cleaned up.

> **2026-11-15 physical-scale update:** the original design and numeric examples below describe the earlier raster. Current geometry is a 3600x4500mm rug (512x640 cells, DX7.03125mm) and 12000x12000mm floor (1280x1280, DX9.375mm), centred at CMM(1800,2250), floor min(-4200,-3750). `Compute/SimulationDomain.hlsl` mirrors the C# constants. EdgePull now integrates a normalized density across at least two floor cells, retaining 60/40 distribution and half-weight corners; the old fixed two-band formula below is historical. Particle emission and per-cell caps scale by cell area (5.0625x); jitter follows the full cell. See `PHYSICAL_SCALE.md` for current grids, physical dimensions and GPU conservation evidence. The full-room floor uses x128 fixed-point statistics (C# and HLSL), so a 12 mm layer across 1280² cells cannot overflow uint32; the original x256 examples below are historical.

## 0. Frames, units, domains
- CMM ("carpet mm space"): origin = carpet UV (0,0) corner; x along u (0..1200), y along v (0..1500). Existing stamps use `uv * CARPET_MM`.
  CMM -> carpet-local metres: `lx = x/1000 - 0.6`, `lz = y/1000 - 0.75`, `ly = height_mm/1000`. CarpetRoot and FloorRoot share the identity transform;
  renderers still multiply by `unity_ObjectToWorld` / `_CarpetToWorld` so moving the roots keeps working.
- Heights: carpet backing top = 0 mm; pile top = `PileTopMm(f, w)`; FLOOR TOP = **-3 mm** (`FLOOR_Y`). Floor water surface = -3 + h.
- Floor domain: square 3600 x 3600 mm centred on the carpet centre (600, 750): `FLOOR_MIN = (-1200, -1050)`, `FLOOR_SIZE = 3600`.
  Resolution `FW = FH = 768`, `FDX = 4.6875 mm`, `FAREA = FDX*FDX`. Floor UV `fuv = (cmm - FLOOR_MIN) / FLOOR_SIZE`. Cell centre `cmm = FLOOR_MIN + (cell + 0.5) * FDX`.
- Carpet rectangle in CMM: `[0,1200] x [0,1500]`. **While a carpet is lying there** (`_CarpetAway == 0`, see Docs/HANDOVER.md 6) the floor bed gets a +40 mm wall at read time (`BedAt` in K_FFlux) AND `FloorWallCell()` closes every water/foam face into it (gravity, tool pushes, foam advection, slump diffusion); those cells are zeroed every frame so they never hold state, and floor water/foam/deposits are not rendered inside the rect. Once the carpet has been handed over the whole rect is ordinary tile: every kernel and every floor shader keys off `CarpetCoversCMM()` / the `_CarpetAway` global, never off `InsideCarpetCMM()` alone.
- Sim timestep: both sims use `DtSim = 1/240 s`, `nSub = clamp(round(dtFrame/DtSim), 1, 8)`; chemistry once per frame with `_DtFrame`.

## 1. Carpet-side changes (CarpetSim.compute, SimCommon.hlsl, CarpetSimulation.cs)
### 1.1 Edge runoff capture
- New RTs (384x480, R32G32B32A32_SFloat, enableRandomWrite): `_EdgeOut` = (waterVol mm^3, sOut, FOut F*mm^2, FdOut F*mm^2), `_EdgeOut2` = (sigmaOut, FAOut F*age*mm^2, 0, 0).
- New kernel `K_ClearEdge` (8x8): zero both. Dispatched at the top of `StepFrame` before K_Stamp.
- `K_Integrate`: for every face n whose neighbour is OUTSIDE the grid (boundary cells only) accumulate into `_EdgeOut[cell]` (own cell, no atomics):
  `waterVol += dt*fx[n]`, `sOut += s * phi_i * dt*fx[n] / (area*h + EPS)`, `sigmaOut += sigma * phi_i * dt*fx[n] / (area*h + EPS)`,
  `share = dt*foamOut[n] / (F*area + EPS)`, `FOut += dt*foamOut[n]`, `FdOut += Fd*share`, `FAOut += FA*share*area` (extensive, F*age*mm^2).
  UAV count of K_Integrate becomes 7 (Flux, FiberOut, ToolFlow, FluidOut, SoapOut, EdgeOut, EdgeOut2). OK.
- These textures are read by the floor sim (`Texture2D<float4>` SRV) in the same frame; nothing else clears them.
### 1.2 Scraper chips + dust accumulators
- `_FiberVel` zw are free (K_Fiber writes `float4(vb,0,0)` every frame). K_Stamp now writes `float4(vb, chipAcc, dustAcc)`.
- Scraper branch: `x` = crust chipped (unchanged formula). New split: `fly = x * _D_CHIP_FLY * mech` (clamp `_D_CHIP_FLY <= 0.8`),
  `crust -= x; grit += 0.8*x - fly; mud += 0.2*x; chipAcc += fly; dustAcc += x * _D_DUST_FRAC;` (dust is visual only, not mass-conserved).
- Dryer branch additionally pushes foam radially (not water): `vTool = radial * 300 * mp; wt = max(wt, mp);` and leaves `wf` untouched (0 when only the dryer stamps -> K_Flux water blend `tool.z*tool.w` = 0, foam push uses `tool.z` only).
### 1.3 Emission kernel `K_Emit` (8x8, own cell, after K_Stamp, before the substeps)
- Reads `_FiberVel.zw` (chipAcc, dustAcc), `_Agit.y` (hose inflow mm this frame), `_FluidOut` (s, h for droplet dirt), `_FiberOut` (pile top), `_Crust.y` (plateShade), `_Stamps`/`_StampCount` (nearest scraper / hose stamp for direction and speed), `_Time`, `_Seed`.
- Writes only `AppendStructuredBuffer<DebrisEmit> _EmitBuf`. The buffer has 8192 entries; the CPU resets its counter each frame (`SetCounterValue(0)`), overflow appends are dropped by the GPU.
- Expected counts (Poisson-ish via `floor(lambda) + (hash < frac(lambda))`, hash = `Hash1(float2(cell) * 0.37 + _Time * 13.1)`):
  chunks `lambda = chipAcc * _D_CHIPS_PER_UNIT`; dust `lambda = dustAcc * _D_DUST_PER_UNIT`; droplets `lambda = inflow * _D_DROPS_PER_MM`.
- Chunk: `pos = (cellCentreCMM + jitter(+-1.5), PileTopMm + 4)`, `size = lerp(_D_CHIP_SIZE_MIN, _D_CHIP_SIZE_MAX, h^2)` (h hash, biased small), stroke `dir`, `speed = clamp(st.speed, 150, 1500)`,
  `vel = dir * speed * (0.55 + 0.7 h) + perp * speed * 0.35 * (h-0.5) * 2 + up * speed * _D_CHIP_UP * (0.35 + 0.8 h)`, `kind = 0`, `amount = 1`, `dirt = plateShade` (from `_Crust.y`), `seed = hash`.
- Dust: `pos = (cellCMM, PileTopMm + 10)`, `vel = dir * 0.35 * speed + up * 80 + rand3 * 60`, `size = 25`, `life = _D_DUST_LIFE * (0.7 + 0.6 h)` (store life in `amount`? no: DebrisEmit has no life; K_DebSpawn sets `life = _D_DUST_LIFE * (0.7 + 0.6*seed)` for kind 1), `amount = _D_DUST_AMOUNT`, kind 1.
- Droplet: `pos = (cellCMM, PileTopMm + h_water + 2)`, radial from the hose stamp centre: `vel = radial * _D_DROP_SPEED * (0.35 + 0.6 h) + up * _D_DROP_SPEED * (0.55 + 0.6 h) + rand3 * 150` (a 50-57 degree splash crown since 2026-09-25, was 0.5 + 0.8 h / 0.35 + 0.5 h; DEBRIS_SYSTEM.md "Splash drop look"), `size = lerp(1.5, 3.0, h)`, `amount = _D_DROP_WATER` (mm^3), `dirt = C = s/(h + w*W_CAP + 1e-3)` (concentration carried), kind 2.
### 1.4 C# (CarpetSimulation)
- Allocates `_edgeOut`, `_edgeOut2`, the `ComputeBuffer emitBuf` (8192 x 48 B, `ComputeBufferType.Append`), exposes `EdgeOut`, `EdgeOut2`, `EmitBuffer`, `StampBuffer`, `StampCount` (int uploaded this frame), `Seed`.
- StepFrame: `emitBuf.SetCounterValue(0)`; K_ClearEdge; K_Stamp; K_Emit; substeps; ...
- `SimSettings` gets nothing new; the debris/floor constants live in `FloorSettings` (below) and are ALSO applied to the carpet compute (`floorSettings.Apply(compute)`) because K_Stamp/K_Emit read `_D_*`. CarpetSimulation gets a `public FloorSettings floorSettings;` field; if null, create a default instance in Awake (`ScriptableObject.CreateInstance<FloorSettings>()`).
- Add `public event Action CarpetReset;` invoked at the end of `ResetCarpet`.

## 2. Shared structs (SimCommon.hlsl <-> C#)
```
struct DebrisEmit      // 48 bytes
{ float3 pos; float size; float3 vel; uint kind; float amount; float seed; float dirt; float pad; };
struct DebrisParticle  // 80 bytes
{ float3 pos; float size; float3 vel; float life; float4 rot; float3 angVel; uint kindState; float amount; float seed; float dirt; float pad; };
// kind: 0 chunk, 1 dust, 2 droplet.  kindState = kind | (state << 8).
// state: 0 dead, 1 flying/sliding, 2 resting on floor, 3 resting on carpet, 4 being sucked, 5 dissolving (chunk sitting in water)
```
C# mirrors: `DebrisEmit` (StructLayout Sequential, Vector3/float/Vector3/uint/float/float/float/float, Size 48) and `DebrisParticle` (Size 80) in `Scripts/DebrisStructs.cs`.
The HLSL structs live in `Compute/DebrisCommon.hlsl` which BOTH `SimCommon.hlsl` (for K_Emit) and `Debris.compute` include (include guard).

## 3. Debris system (Debris.compute, DebrisSystem.cs, DebrisMeshBuilder.cs, DebrisChunk/DebrisDust/DebrisDrop shaders)
- Pool `N = 16384` `RWStructuredBuffer<DebrisParticle> _Particles`; `RWStructuredBuffer<uint> _Head` (1 uint, ring head); `StructuredBuffer<DebrisEmit> _EmitIn` + `StructuredBuffer<uint> _EmitCount` (filled by `ComputeBuffer.CopyCount(emitBuf, countBuf, 0)`).
- Kernels: `K_DebInit` (all dead, head 0), `K_DebSpawn` (64 threads; `i < _EmitCount[0] && i < 8192` -> `slot = InterlockedAdd(_Head[0], 1) % N`, initialise particle, random quaternion, angVel), `K_DebSim` (64 threads, one per particle).
- K_DebSim inputs: `_Stamps` + `_StampCount` (carpet's buffer), `_FiberTex` (carpet pile top: `PileTopMm`), `_FluidTex` (carpet h, flow), `_FFluidTex` (floor h, flow), `_DtFrame`, `_Time`, the `_D_*` and floor uniforms; outputs `_Particles` and `RWStructuredBuffer<uint> _FInject` (floor inject, 768*768*4 uints, fixed point x1e4: ch0 water mm, ch1 slurry, ch2 foam F, ch3 dust).
- Ground height at (x, y): inside carpet rect -> `PileTopMm(fb) + carpet h`; else `FLOOR_Y + floor h`. `InsideCarpet(p) = p.x>=0 && p.x<=1200 && p.y>=0 && p.y<=1500`.
- Flying: `vel.y -= _D_GRAV*dt` (dust: gravity x0.04 and drag `vel *= exp(-2.5 dt)`, buoyant drift +up 20 mm/s), `pos += vel*dt`, chunks spin `rot = integrate(angVel)`.
  Contact when `pos.y - 0.5*size <= ground`: chunk -> reflect `vel.y = -vel.y * rest` (rest = `_D_REST_CARPET` on carpet, `_D_REST_FLOOR` on floor), tangential `vel.xz *= 0.7`, `angVel *= 0.6`; if `|vel| < 60` -> snap to ground, `vel = 0`, state 2/3, align rotation so the flat side is down (random yaw + small tilt from seed).
  dust -> clamped to the ground on contact (keeps drifting), dies at `life <= 0`: if over floor deposit `amount` into ch3 at the floor cell, else nothing.
  droplet -> dies at first contact: if over floor deposit `amount/FAREA` mm water into ch0 and `dirt * amount * 0.001` into ch1; on carpet nothing.
  Sliding (state 1 on the ground with |vel.y| small): `vel.xz *= exp(-_D_FRICTION*dt)`; stop -> resting.
- Resting chunk each frame: loop over stamps with `FootprintWeight` (same function as the carpet, p in CMM):
  DryVac(1)/WetVac(6): `m > 0.15` -> state 4, target = stamp centre CMM at ground+30 (store in angVel), `life = 0.4` timer.
  Squeegee(5): `vel.xz = st.vStroke * 0.85`, state 1. Brush(4): `vel.xz += st.vStroke*0.4*m; vel.y = 120*m`, state 1. Hose(2): `vel.xz += radial * 500 * m * dt`, if `|vel.xz| > 30` state 1. Dryer(7): `size < 6` -> `vel.xz += radial * 900 * m * dt`, state 1.
  Water: sample floor/carpet `h` at pos; if `h > 0.3`: `vel.xz += (flow - vel.xz) * (1 - exp(-3 dt))`, state 1 if `|vel.xz| > 20`; dissolving: `size -= _D_DISSOLVE * dt * saturate(h/0.5)`; if `size < 1.5` -> die and deposit slurry `amount * 0.02` into ch1 when over floor.
- Sucked (state 4): `pos = lerp(pos, target, 1 - exp(-_D_SUCK_SPEED dt))`, `size *= exp(-4 dt)`, `life -= dt`; dies when `life <= 0` or `dist < 8`.
- Deposits: `InterlockedAdd(_FInject[(cy*768 + cx)*4 + ch], (uint)(value * 10000.0 + 0.5))`, cell from `floor((p - FLOOR_MIN)/FDX)` clamped, skip if outside the domain.
- Rendering (DebrisSystem.LateUpdate after the kernels): three `Graphics.RenderMeshIndirect(rp, mesh, args, 1)` calls with `RenderParams { material, worldBounds = 20 m cube, shadowCastingMode Off, receiveShadows false }` and `GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = mesh.GetIndexCount(0), instanceCount = N }`. Materials get `_Particles` (SetBuffer), `_CarpetToWorld` (Matrix4x4 from CarpetRoot localToWorld), `_KindFilter` (0/1/2). Dead or other-kind instances: chunk and dust collapse to scale 0 (degenerate, never NaN), the drop streak puts all four corners outside the clip volume. Each kind is queued at most once per frame per material (captures included; DEBRIS_SYSTEM.md, capture trap).
- Meshes (`DebrisMeshBuilder`): `BuildChunk()` = icosphere (1 subdivision, 80 tris) with radial jitter `r *= 0.7 + 0.6 hash`, flattened y x0.45 -> plate-like shard, unit diameter 1.0 (particle `size` scales it); `BuildQuad()`: one mesh shared by the dust and, since 2026-09-25, the droplet draws (centred, +-0.5, one winding, both passes Cull Off, expanded in the vertex shader: a camera-facing billboard / a screen-space motion streak; the icosphere `BuildDroplet()` is gone).
- Shaders: instance data from `StructuredBuffer<DebrisParticle> _Particles` with `SV_InstanceID` (`#pragma target 4.5`, no `UNITY_INSTANCING`). World pos = `mul(_CarpetToWorld, float4(cmmToLocal(pos), 1))`.
  DebrisChunk: opaque, Lambert wrap + `SampleSH` + small spec; albedo `lerp(_CrustColor (0.50,0.45,0.38), _MudColor (0.24,0.15,0.08), 0.35*dirt + 0.25*(1-seed))`, underside darker (dot(N, up)), fresnel rim 0.05. Depth-only pass too.
  DebrisDrop: since 2026-09-25 a screen-space motion streak (1/60 s shutter, >= 3 px wide at 1080p), premultiplied `Blend One OneMinusSrcAlpha`, ZWrite off: clear lens centre, dark refraction band + Fresnel rim, HDR sun glint at the head (pixel-integrated, skewed per drop), lamp-aware; launched from the hose skirt since 2026-09-26 — DEBRIS_SYSTEM.md "Splash drop look" (was a physically sized icosphere, sky tint * 0.4 + spec pow 60, alpha 0.75: 1-2 px, never seen).
  DebrisDust: transparent billboard, `Blend SrcAlpha OneMinusSrcAlpha`, ZWrite off, size grows `size * (1 + 3*(1 - life01))`; since 2026-09-25 alpha = `1 - exp(-tau)` with centre optical depth `_Density / grow^2 * exp(-_Settle age)` (1/area, the dust settles), Gaussian profile, log-normal wisps, soft fade above the ground (`p.dirt`, low-passed in K_DebSim), colour (0.68, 0.64, 0.58) — DEBRIS_SYSTEM.md "Dust puff look" (was `0.35 * 4 * life01 * (1-life01) * radialFalloff`, later 0.8, which grew into opaque discs). `life01 = life / lifeMax` where lifeMax is stored in `pad` at spawn.

## 4. Floor simulation (FloorSim.compute, FloorCommon.hlsl, FloorSimulation.cs, FloorSettings.cs)
### 4.1 State (768x768; all R32G32B32A32_SFloat unless noted)
- `_FFluid[2]` (h mm, u, v mm/s, s slurry), `_FSoap[2]` (sigma, F, Fd, Fage), `_FDeposit` (dust, residue, wetFilm, spare), `_FFlux`, `_FBed` (R32_SFloat, mm), `_FToolFlow` (R16G16B16A16: vTool.xy, wt, wf), `_FAgit` (R16G16B16A16: agit, inflow, dryer, 0).
- `RWStructuredBuffer<uint> _FInject` (768*768*4) owned by FloorSimulation, shared with DebrisSystem (`FloorSimulation.InjectBuffer`).
- Uniform names in FloorCommon.hlsl: `int _FW, _FH, _StampCount; float _FDX, _DtSim, _DtFrame, _Time; float2 _FloorMin; float _FloorSize;` + every `FloorSettings` float as `_NAME`.
- Globals for shaders: `_FFluidTex _FSoapTex _FDepositTex _FBedTex`, `_FloorRect = (FLOOR_MIN.x, FLOOR_MIN.y, 1/FLOOR_SIZE, FLOOR_Y)`, `_FloorTexel = (1/768, 1/768, 768, 768)`, `_FloorConsts = (F_H_MAX, F_F_MAX, FDX, 0)`.
### 4.2 Kernels
- `K_FInit`: bed = tiles: `tile = floor(cmm / _F_TILE)`, grout where `frac(cmm/_F_TILE)*_F_TILE < _F_GROUT_W` on either axis -> `-_F_GROUT_DEPTH` with a smooth 1.5 mm chamfer; per-tile tilt `0.25 * dot(Hash2(tile)-0.5, localOffset/_F_TILE)`; micro-noise `0.08*(Fbm(cmm*0.02)*2-1)`; `+40` inside the carpet rect. Clear all state. `K_FClearInject` (1D, 768*768*4 threads / 256) zeroes the inject buffer.
- `K_FInject` (8x8, own cell): (a) EdgeOut pull — see 4.3; (b) `_FInject` consume: for ch 0..3 read, convert `/1e4`, zero it; `h += water`, `s += slurry`, `F += foam`, `dust += dust`; (c) stamps (4.4) writing `_FAgit`, `_FToolFlow` fully every frame. UAVs: FFluidOut, FSoapOut, FDeposit, FAgit, FToolFlow, FInject = 6.
- `K_FFlux`: virtual pipes (faces into `FloorWallCell` cells are closed; no `continue` inside the [unroll] face loop, FXC cannot unroll it). `head = bed + h`; mobile height `hm = (tool.z > 0.05) ? h : max(0, h - _F_H_REST)`; `A = _FDX * hm`; `drag = lerp(_F_DRAG_HI, _F_DRAG_LO, saturate(h/2))`; ghost neighbours `h_n = 0, b_n = b_i - _F_EDGE_DROP`; tool blend as in the carpet; limiter `K = min(1, hm*FDX^2 / (dt*sumF + EPS))`.
- `K_FIntegrate`: as carpet `K_Integrate` with `phi = 1` (no pile) and foam transport (`_F_FOAM_SLIP`, tool push, slump diffusion `_F_D_FOAM`). Ghost faces = real loss. Also carries `s` and `sigma`.
- `K_FChem` (per frame): `boost = 1 + _F_DRYER_BOOST*dryer`; evaporation `dEv = min(h, _F_E_SURF*boost*dt)`; residue from drying: `if (h < 0.05) { residue += s; s = 0; } else { r = _F_REDEP * s * dEv/(h + dEv + EPS); residue += r; s -= r; }`; `wet = saturate(h/0.2)`; `wetFilm = max(wetFilm, wet)`, `wetFilm -= wetFilm*(1-exp(-dt*boost/_F_TAU_WET))`;
  dust -> slurry when wet: `d = DecayAmount(dust, _F_K_DUST_DIS*wet, dt); dust -= d; s += d;`  residue -> slurry: `k = wet*(_F_K_RES_DIS + _F_K_RES_AGIT*agit)`; dryer blows dust: `dust -= DecayAmount(dust, 4*dryer, dt)`;
  soap/foam: `soapC = sigma/(h+1e-3)`, `soapy = saturate(soapC/_F_SOAP_C_REF)`, `gen = _F_K_GEN*agit*soapy*wet`, `F += min(F_MAX-F, gen dt)`, `sigma -= min(sigma, _F_GAMMA_SOAP*dGen)`;
  collapse `= _F_K_DRAIN + _F_K_DIL*saturate(inflow/(dt*10))*(1-soapy) + _F_K_DRYPOP*(1-wet) + 0.3*saturate((h-4)/4) + 3*dryer`; `dF = DecayAmount(F, collapse, dt)`; foam dirt returns to `s` (if h > 0.05) else to residue; foam scavenges slurry `dFd = min(s, _F_K_FD*F*C*dt)`; `Fage` as carpet.
### 4.3 EdgeOut pull (inside K_FInject)
- Floor cell centre `c`. `dxOut = max(max(0 - c.x, c.x - 1200), 0)`, `dyOut = max(max(0 - c.y, c.y - 1500), 0)`. If both are 0 (inside) or `max(dxOut, dyOut) > _F_DRIP_W` -> nothing.
- Side = the axis with the larger outside distance. Band index `j = floor(dOut / FDX)` (0 or 1 for DRIP_W 10 mm), weight `wj = (j == 0) ? 0.6 : 0.4`.
- Along-edge interval of the floor cell: `[a, b] = [c.along - FDX/2, c.along + FDX/2]` clamped to the carpet extent; carpet cells `k` from `floor(a/DXc)` to `floor((b - 1e-3)/DXc)` (DXc = 3.125, `k` clamped to the edge length); overlap fraction `o_k = overlap([a,b], [k DXc, (k+1) DXc]) / DXc`.
  Edge cell index: left edge `(0, k)`, right `(383, k)`, bottom `(k, 0)`, top `(k, 479)`. Sum `E = sum_k o_k * _EdgeOut[edgeCell_k]` and `E2` likewise.
- Apply: `h += wj * E.x / FAREA`, `s += wj * E.y`, `F += wj * E.z / FAREA`, `Fd += wj * E.w`, `sigma += wj * E2.x`, `FA = F_old*Fage + wj*E2.y/FAREA -> Fage = FA / max(F, 1e-4)`. The two end texels of every edge (the corners) are weighted 0.5 because both sides read them. Floor `h` is NOT clamped to F_H_MAX anywhere in the sim (only inflow caps and the rendered relief use it).
### 4.4 Tools on the floor (K_FInject, p = cellCMM - st.uv*CARPET_MM, same `FootprintWeight`)
- Scraper 0: `x = residue*0.3*mp*mech; residue -= x; dust += 0.5*x`.
- DryVac 1: `dust -= DecayAmount(dust, 6 mp, dtS)`, `residue -= DecayAmount(residue, 0.3 mp, dtS)`, `F -= F*(1-exp(-2 mp dtS))` (Fd proportionally).
- Hose 2: `add = max(0, min(_F_H_MAX - h, 150 mp dtS)); h += add; inflow += add; agit = max(agit, 0.5 mp); vTool = radial*400*mp; wt = max(wt, mp); wf = 1`.
- Foam 3: `sigma += 3 mp dtS; h += max(0, min(_F_H_MAX-h, 6 mp dtS)); F = min(_F_F_MAX, F + 12 mp dtS)`.
- Brush 4: `agit = max(agit, 1.5 mp saturate(speed/_F_AGIT_SPEED)); vTool = 0.8 vStroke; wt = max(wt, mp*mech); wf = 0.35`.
- Squeegee 5: `vTool = vStroke; wt = max(wt, mp*mech); wf = 1; agit = max(agit, 0.3 mp)`.
- WetVac 6: `dh = min(h, 200 mp dtS); frac = saturate(dh/(h+1e-3)); s -= s frac; sigma -= sigma frac; h -= dh; dF = min(F, 60 mp dtS); Fd -= Fd*saturate(dF/max(F,1e-4)); F -= dF; dust -= DecayAmount(dust, 4 mp, dtS)`.
- Dryer 7: `dryer = max(dryer, mp); vTool = radial*300*mp; wt = max(wt, mp)` (wf untouched).
### 4.5 FloorSettings (ScriptableObject, `Settings/FloorSettings.asset`, reflection Apply like SimSettings)
```
F_H_MAX 12, F_U_MAX 600, F_H_REST 0.35, F_DRAG_LO 1.5, F_DRAG_HI 25, F_G 9810, F_E_SURF 0.004, F_DRYER_BOOST 400, F_TAU_WET 45,
F_REDEP 1.0, F_K_DUST_DIS 3, F_K_RES_DIS 0.05, F_K_RES_AGIT 2, F_K_GEN 40, F_F_MAX 12, F_GAMMA_SOAP 0.01, F_K_DRAIN 0.03, F_K_DIL 1.2, F_K_DRYPOP 0.5,
F_K_FD 0.05, F_D_FOAM 20, F_FOAM_SLIP 0.9, F_SOAP_C_REF 0.3, F_C_SAT 2, F_EDGE_DROP 20, F_DRIP_W 10, F_GROUT_DEPTH 1.0, F_GROUT_W 6, F_TILE 300, F_SPEED_REF 50, F_AGIT_SPEED 300,
D_GRAV 9810, D_CHIP_FLY 0.45, D_DUST_FRAC 0.5, D_CHIPS_PER_UNIT 0.08, D_DUST_PER_UNIT 0.03, D_DROPS_PER_MM 0.004, D_CHIP_UP 0.6, D_CHIP_SIZE_MIN 3.5, D_CHIP_SIZE_MAX 9,
D_REST_CARPET 0.2, D_REST_FLOOR 0.35, D_FRICTION 6, D_DISSOLVE 0.15, D_DROP_SPEED 1500, D_DROP_WATER 3.5, D_DUST_LIFE 1.6, D_DUST_AMOUNT 0.02, D_SUCK_SPEED 12
```
### 4.6 FloorSimulation.cs
- `[DefaultExecutionOrder(110)]`, fields: `compute`, `settings (FloorSettings)`, `carpet (CarpetSimulation)`. Allocates RTs (`hideFlags DontUnloadUnusedAsset`), inject buffer, finds kernels, `ResetFloor()` (K_FInit + K_FClearInject) on Start and on `carpet.CarpetReset`.
- `StepFrame(dt)`: bind carpet `EdgeOut/EdgeOut2` (SRV) + `StampBuffer` + `_StampCount = carpet.StampCount`; upload uniforms (`settings.Apply(compute)`, `_FDX`, `_FloorMin`, `_FloorSize`, `_FW/_FH`, dt's, `_Time`); dispatch schedule; `SetGlobals()`.
- Public: `Fluid`, `Soap`, `Deposit`, `Bed`, `InjectBuffer`, `Ready`, `FloorStatsSync()` via a reduce kernel `K_FReduce` (8 sums: h, s, F, Fd, dust, residue, wetFilm, count(h>0.3); same pattern as the carpet) for `CarpetDebugApi.FloorStatsJson()`.

## 5. Floor rendering (FloorRenderer.cs, FloorSurface.shader, FloorWater.shader)
- `FloorRenderer` (order 200) builds at Awake: `FloorSurface` child = plane 12 x 12 m (4x4 quads, normals up) at local y = -0.003; `FloorWater` child = `BuildGrid(3.6, 3.6, 256, 256)` translated by (0, -0.003, 0); mesh bounds y expanded +0.1; `FloorFoam` child = `BuildGrid(3.6, 3.6, 384, 384)` at the same y, bounds 0.4 m tall, material `FloorFoam.mat` (`CarpetFoam` shader with `_FLOOR_DOMAIN`, see `FOAM_LOOK.md`). All renderers: shadows off, probes off.
- FloorSurface.shader (opaque, "Queue"="Geometry-10"): CMM from object-space `cmm = (posOS.x*1000 + 600, posOS.z*1000 + 750)`, `fuv`, inside-domain mask, inside-carpet mask -> treat as dry. Procedural tiles (`_F_TILE`-sized, grout `_F_GROUT_W`) with per-tile hash brightness (+-6 %), fbm mottle, fine speckle, grout darker + bevel normal; base colour `_TileColor (0.82, 0.83, 0.85)`, `_GroutColor (0.60, 0.60, 0.62)`.
  Wetness `wetv = saturate(max(h/0.15, wetFilm))`: albedo `*= lerp(1, lerp(0.86, 0.55, grout), wetv)` (glaze barely darkens, grout does; 5.1), smoothness `lerp(_CleanGloss, 0.92, wetv)`, spec F0 0.04. Dust overlay `_DustColor (0.40, 0.33, 0.23)` `a = 1 - exp(-3 dust)` with noise breakup, matte; residue `_ResidueColor (0.19, 0.13, 0.07)` `a = 1 - exp(-4 residue)`, ring emphasis by high-frequency noise. Lighting: main light GGX (use URP `UniversalFragmentPBR` with filled `InputData/SurfaceData`, metallic 0) + SH; the wet fresnel gets an extra `_SkyColor (0.62,0.68,0.76)` term `fres * wetv * 0.6`. Passes: UniversalForward, DepthOnly.

  **Dirty/clean contrast (2026-09-20).** The clean tile is deliberately near-white and polished, so the player
  reads progress from brightness and shine alone. Three terms, all gated on `polish = 1 - soil` where
  `soil = saturate(max(aD, aR) * 1.15 + 0.35 * saturate(dust * 6))`:
  1. base smoothness `lerp(_CleanGloss 0.74, _CleanGloss * 0.30, grout)` (was a flat 0.30 / 0.12) — dust still
     pulls it to 0.08 and residue to 0.28, so dirt is matte and clean tile is glossy;
  2. a dry-polish fresnel added to the wet one: `fres * (wetv * 0.6 + polish * _CleanSheen) * _SkyColor`;
  3. a tight sun glint `pow(dot(N, H), 260) * polish * (1 - grout) * (1 - 0.35 wetv) * _CleanSheen * 3` plus a
     coarse ~6 mm micro-twinkle (`hash1(floor(cmm * 0.16))`, top 2.5 % of cells) so the clean floor sparkles
     instead of reading as flat white paint.

  `_TileColor` 0.46 -> 0.82 is on purpose against the old "the 3x sun would blow out anything lighter" note: the
  brief is a floor that is *visibly white* when clean. At albedo 0.82 under the 3x sun at 50 deg the tile lands
  around 2.3 linear, which the Neutral tonemapper maps to ~0.9 and which is just above the 1.2 bloom threshold,
  so a clean floor glows slightly while a dusty one (0.40 albedo -> ~1.15 linear) stays under it entirely. If it
  ever reads too hazy, raise `Bloom.threshold` on `Settings/SampleSceneProfile.asset` rather than darkening the
  tile; `_CleanGloss` / `_CleanSheen` on `Materials/FloorSurface.mat` are the shine knobs.
  Since 2026-09-26 the scene uses the ACES tonemapper (user decision); the display values above were measured under Neutral.
- FloorWater.shader: copy of CarpetWater adapted: vertex `y = (FLOOR_Y + min(h, 6)) * 0.001 + 0.0003` (2026-09-19: the `0.9 F` foam lift and the Worley foam term moved to `FloorFoam` / `CarpetFoam.shader`), discard when inside the carpet rect (2 mm margin), under an opaque foam mound (`F > 1.2`) or `a < 0.01`; normal from `bed + h` central differences over one floor texel; refraction of the opaque texture, Beer-Lambert (`_AbsorbClean/_AbsorbDirty`), fresnel `_SkyColor`, spec; alpha `1 - exp(-h/0.12)`. Queue Transparent, `ZTest LEqual`, `ZWrite Off`, `Blend One OneMinusSrcAlpha`.

### 5.1 Water gloss: the room the water mirrors (2026-09-26, for the trailer hook)

Problem (store refresh 2026-09-26, `TRAILER.md` 8.3 item 1): a puddle on the white tile read as a cast shadow. The camera
looks ~55 deg down (pos (0, 1.5, -1.12)), so the sun's mirror point lies ~2.2 m behind it and a flat puddle never shows
the sun; the fresnel sky term is ~2-4 % there; and FloorSurface darkened the wet glazed tile to 0.55 albedo. Result: a flat
brown or grey disc with the grout showing through, which is exactly what a shadow looks like.

Fix (all shader-side, no C#):
- `Shaders/RoomReflection.hlsl` (new): a flat white ceiling 2.7 m above the floor with a grid of 60 x 60 cm LED panels
  (pitch 1.2 m, radiance 12 x main light colour, ceiling 0.30 x sky). The reflected ray is intersected with the ceiling
  plane, so the panels are parallax-correct. `ROOM_OFFSET` puts a panel's mirror image on the tiles just right of the rug's
  right rim (u ~1.05, v 0.5), where the squeegee pours. Water F0 0.02 (`RoomFresnel`).
- Mirror normal = the sim's surface slope + a slow swell; the fine 4 mm ripples only widen the panel edge (roughness =
  the reflected cone's footprint). With the ripple normal itself the panels broke into camouflage blotches (rejected).
- `FloorWater`: `+ RoomFresnel(N.V) * RoomRadiance(...)`. `CarpetWater`: the same, times the tuft occlusion `envOcc`, so a
  free sheet (the squeegee wave, a pool) mirrors the panels and a film between the tufts keeps its dark wet look.
  `FloorSurface`: a damp / detergent film mirrors them blurred (roughness 0.03), faded out under standing water.
- `FloorSurface` wet darkening `lerp(0.86, 0.55, grout)` instead of a flat 0.55.
Verified 2026-09-26 on a squeegee pour (4K probes, `scratchpad` A/B): the brown puddle reads as glossy liquid with a
wobbling panel image; clear water no longer reads as a grey smear; the dry floor is unchanged. Knobs: the `#define`s at
the top of `RoomReflection.hlsl`.

## 6. Scene builder + input + debug API
- `CarpetSceneBuilder.Build`: delete old `Floor`; create/refresh `FloorRoot` with `FloorSimulation` (compute `Compute/FloorSim.compute`, settings `Settings/FloorSettings.asset`, carpet ref), `FloorRenderer` (materials `Materials/FloorSurface.mat`, `Materials/FloorWater.mat`, `Materials/FloorFoam.mat`), `DebrisSystem` (compute `Compute/Debris.compute`, materials `Materials/DebrisChunk.mat`, `DebrisDust.mat`, `DebrisDrop.mat`, refs to carpet + floor, `carpetRoot` transform); `BuildFloorSettings()` creates the asset; `CarpetSimulation.floorSettings` assigned. Children placeholders `FloorSurface`, `FloorWater`, `FloorFoam` with the materials.
- `ToolController.TryGetCarpetUV` -> also accept floor hits: if the collider raycast misses, intersect the plane `y = 0` of the carpet root, compute uv the same way, accept when `u in [-1.0, 2.0]` and `v in [-0.7, 1.7]` (floor domain). No clamping of uv in that case. Add `public bool PointerOnCarpet`.
- `CarpetDebugApi`: `FloorStatsJson()`, `SaveFloorView(which, png)` for `ffluid|fsoap|fdeposit|fbed`, `DebrisCount()` (readback of `_Head`, i.e. total spawned).

## 7. Verification plan (integration, done by the main agent)
1. Recompile clean; `CarpetCleaning/Build Scene`; enter play; `Reset(1)`; screenshot -> tiles visible, no water/dust anywhere, ~120+ fps.
2. Scraper strokes near the left edge pushing outward -> chunks visible on the floor left of the carpet and on the carpet; `DebrisCount()` grows; dust puffs in a mid-stroke screenshot.
3. Hose hold at `(0.05, 0.5)` 3 s -> puddle forms on the floor beside the carpet (`FloorStatsJson().water > 0`), droplets visible in a mid-hold capture.
4. Foam sprayer + brush near the edge pushing outward -> foam on the floor; wait 20 s -> foam collapses; after the dryer a dirty ring appears (`residue > 0`).
5. Squeegee stroke across the puddle on the floor -> puddle moves, thin wet film stays behind and dries over ~45 s (wetFilm).
6. Dry vacuum over floor chunks -> they get sucked; wet vacuum over the puddle -> `water` drops.
7. Tools used far from the carpet do not affect carpet stats; carpet `clean%` behaviour unchanged.
