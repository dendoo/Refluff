# Floor simulation + rendering — implementation notes (lane A, 2026-09-19)

Companion to `FLOOR_SYSTEM.md` sections 4 and 5. Files: `Compute/FloorCommon.hlsl`, `Compute/FloorSim.compute`,
`Scripts/FloorSimulation.cs`, `Scripts/FloorRenderer.cs`, `Shaders/FloorSurface.shader`, `Shaders/FloorWater.shader`.

## Resource binding (why there are `*In` aliases)
- Every floor texture exists twice in `FloorCommon.hlsl`: `RWTexture2D _FX` (bound where a kernel WRITES) and `Texture2D _FXIn`
  (bound where a kernel only READS). Same RenderTexture, different declaration, so the 8-UAV cap is never approached:
  K_FInit 7, K_FClearInject 1, K_FInject 6, K_FFlux 1, K_FIntegrate 2, K_FChem 3, K_FClearStats 1, K_FReduce 1.
- `_EdgeOut/_EdgeOut2` (carpet runoff) and `_Stamps` are SRVs bound from `CarpetSimulation.EdgeOut/EdgeOut2/StampBuffer`;
  when the carpet is missing/not ready the floor binds `Texture2D.blackTexture` and a 1-entry dummy stamp buffer with `_StampCount = 0`.
- `_FInject` (768*768*4 uints) is owned here (`FloorSimulation.InjectBuffer`), zeroed by K_FClearInject on reset and
  consumed-and-zeroed per cell by K_FInject every frame. DebrisSystem only InterlockedAdds into it.

## Deviations from FLOOR_SYSTEM.md (deliberate)
- Floor cells INSIDE the carpet rect never receive stamps or inject deposits **while a carpet is actually on it** (they
  consume/zero the inject slots and drop the value). Otherwise a hose used on the carpet would fill the +40 mm wall cells and
  spill phantom water onto the floor. With the carpet handed over (`_CarpetAway`, HANDOVER.md 6) the same cells behave like
  any other tile.
- FloorWater/FloorSurface children sit at local y = -0.003 (FLOOR_Y); the water vertex shader therefore lifts by
  `(h + 0.9F)*0.001 + 0.0003` relative to the child, NOT by `FLOOR_Y + h + ...` (that would double-count the 3 mm).
- The water normal helper `SurfMm` ignores samples under a carpet that is there (uses the centre height) and clamps bed to
  <= 5 mm so the +40 mm wall never tilts the water normal at the carpet edge. The +40 mm is no longer baked into `_FBed`
  (K_FFlux adds it), so the clamp is now only belt-and-braces.
- FloorSurface reads tile parameters from the globals `_F_TILE`, `_F_GROUT_W`, `_F_GROUT_DEPTH` (set in `FloorSimulation.SetGlobals`).
- FloorStats are per-cell MEANS over the 768x768 domain (same convention as SimStats), fixed point x256 in K_FReduce;
  `pooledCells` is the fraction of cells with h > 0.3 mm. Numbers are small (a 100 mm puddle of 2 mm ~ 1.5e-3) but exact enough.
- EdgePull weights follow the doc literally (`j == 0 ? 0.6 : 0.4`); with FDX 4.6875 and DRIP_W 10 exactly two bands exist
  (centres at 2.34 and 7.03 mm from the edge). Raising F_DRIP_W above ~11.7 mm would add a third band and over-count mass.
- Both carpet and floor edge-runoff/flux limiters use the MOBILE height (h - F_H_REST) in K_FFlux, so a thin film never moves
  unless a tool pushes it (tool.z > 0.05) -> squeegee leaves a wet film that dries via wetFilm/TAU_WET.

## Dispatch schedule (FloorSimulation.StepFrame, LateUpdate order 110)
`K_FInject` (in place, current halves) -> nSub x (`K_FFlux` -> `K_FIntegrate` with swap) -> `K_FChem` (in place) -> SetGlobals.
Reset (`ResetFloor`, also on `carpet.CarpetReset`): `K_FInit` on both ping-pong halves + `K_FClearInject`.
dt = min(Time.deltaTime, 1/30) * carpet.TimeScale, skipped when `carpet.Paused`.

## Verification recipes (no domain reload)
- compute: `unity command eval` with `ImportAsset(... ForceSynchronousImport)` + `ShaderUtil.GetComputeShaderMessages` -> "0 msgs".
- shaders: same with `ShaderUtil.GetShaderMessages(Shader.Find("CarpetCleaning/FloorSurface"|"CarpetCleaning/FloorWater"))`.
- C#: concatenate FloorSimulation.cs + FloorRenderer.cs (+ DebrisStructs, FloorSettings, CarpetSimulation) -> `run_script --dry_run true`.
- Runtime: `FloorSimulation.FloorStatsSync()` after a hose hold beside the carpet -> `water > 0`, `pooledCells > 0`.
