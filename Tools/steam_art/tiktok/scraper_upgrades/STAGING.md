# Scraper-upgrade TikTok: capture-only crust staging

`StageCrust.compute` is a filming helper, deliberately outside `Assets`. It does not change the game simulation or its shader. The root capture recipe temporarily imports a copy under an Editor-only scratch folder and removes that copy after capture.

## Interface and invocation

- Kernels: `K_Stage` (once after the normal full-dirt reset), `K_CleanCrumbs` (after each scripted sim frame, before rendering).
- Both kernels: `int2 _Size`, `int _Seed`, `RWTexture2D<float4> _Dirt`.
- Bind `_Dirt` to `sim.Dirt`, set `_Size` to `(384, 480)` and `_Seed` to the same seed on every dispatch. Each kernel has **one UAV** and `[numthreads(8,8,1)]`.
- Both dispatches are `(48,60,1)` for the current simulation dimensions.

```csharp
var stage = helper.FindKernel("K_Stage");
var clean = helper.FindKernel("K_CleanCrumbs");
helper.SetInts("_Size", CarpetSimulation.SimW, CarpetSimulation.SimH);
helper.SetInt("_Seed", sim.Seed);
helper.SetTexture(stage, "_Dirt", sim.Dirt);
helper.SetTexture(clean, "_Dirt", sim.Dirt);

// First do CarpetDebugApi.Reset(seed, 1f), then stage:
helper.Dispatch(stage, CarpetSimulation.SimW / 8, CarpetSimulation.SimH / 8, 1);

// Manual scripted frame: StepAll(dt); then this; then capture.
helper.Dispatch(clean, CarpetSimulation.SimW / 8, CarpetSimulation.SimH / 8, 1);
```

For real `EditorApplication.Step()` capture through `StoreGifApi`, dispatch cleanup inside the capture rig's `RenderPipelineManager.beginContextRendering` callback, after LateUpdate has executed and before cameras render. Calling it more than once per simulation frame is safe and idempotent. Do not dispatch `K_Stage` from that hook. Rebind the Dirt texture after any reset/reallocation.

## Intended appearance

The whole carpet starts with crust `0.90..1.00`, moderate mud `0.13..0.22`, stain `0.04`, grit `0`. Initialization's slab shades, crack distances, initial zero damage, pile, and all other RTs remain unchanged. Production `K_Stamp` alone removes the crust and production `K_Emit`/`DebrisSystem` alone emit and simulate chunks and dust.

Scraping normally leaves `0.8 * chipped - flying` as grit and `0.2 * chipped` as extra mud. The helper zeroes grit only in cells whose crust has decreased from the deterministic staged initial value, and clamps mud to the staged base. It does not remove the initial grime/stain. This shortcut is valid only for this dry, scraper-only filming scene; it must not be used for normal gameplay or wet-tool shots.

## Power and area notes

Use the actual `ToolTuning` path and its `Effective` geometry. Public `sim.StampBuffer`/`sim.StampCount` are available after `StepFrame`, but this helper needs neither.

`ToolTuning.Set(Scraper, power, area)` clamps each value to `0.05..20`. Ensure `ToolTuning.SetNeutral(false)` (a debug Reset enables Neutral) or changes have no effect.

Maximum power 20 already clears solid crust in one full-strength moving stamp: production damage gain is `0.6 * 20 * (0.3..1) = 3.6..12`, which reaches 1 even at the centre of a slab. Its chip multiplier is then `0.4 * 20 * (0.5 + plateShade) = 4..12`, clamped to existing crust. Keep the initial damage at 0; staging it to 1 unnecessarily widens the visible cracks before contact. At soft footprint edges or low speed the multiplier falls, so extend sweeps slightly past carpet edges and overlap them.

For the weak sequence, power around `0.08..0.15` gives a visible but extremely slow scratch after repeated strokes; the absolute minimum `0.05` can spend the first passes merely building invisible crack damage. A blade's area scales its width linearly, not by square root. Choose the area from the actual needed sweep width and 3D head appearance instead of blindly setting area 20.

## Verification

Static inspection: no includes, one active RW resource per kernel, no atomics, no unbounded loops, finite seeded noise, explicit thread bounds. Root must perform Unity compute import/message check, actual dispatch, a repeated post-dispatch message check, and before/after frame review. The helper intentionally has not been imported or dispatched by the helper agent.

## Settled physical crumbs (third kernel)

`K_ClearSettledCrumbs` additionally removes physical solid chunks after they settle onto the carpet. It preserves every airborne chunk, every dust puff/drop, all floor resting chunks, and particles outside the rug. The production state contract defines resting carpet chunks as state 3 and grounded sliding chunks as state 1 with **exactly** zero vertical velocity. The kernel further requires the chunk's flat underside to be within 1.5 mm of the current pile top; this prevents removal of floor sliders below the carpet rectangle. It is disabled while `sim.CarpetAway` is true.

The self-contained 80-byte particle declaration matches `DebrisStructs.cs`/`DebrisCommon.hlsl`. This kernel binds one UAV (`_Particles`) and one SRV (`_FiberTex`); it does not bind `_Dirt`. Invoke after `DebrisSystem.StepFrame` / in the same pre-render callback as `K_CleanCrumbs`. Rebind the current `sim.Fiber` on every dispatch because it ping-pongs.

```csharp
int settled = helper.FindKernel("K_ClearSettledCrumbs");
helper.SetInt("_ParticleCount", DebrisSystem.ParticleCount); // 16384
helper.SetInt("_CarpetAway", sim.CarpetAway ? 1 : 0);
helper.SetFloat("_PileHeightMm", sim.settings.PILE_H);
helper.SetBuffer(settled, "_Particles", debris.Particles);
helper.SetTexture(settled, "_FiberTex", sim.Fiber);
helper.Dispatch(settled, (DebrisSystem.ParticleCount + 63) / 64, 1, 1); // 256 groups
```

This is a filming exaggeration: settled particles disappear instead of requiring the dry vacuum. Particle generation, airborne travel, collisions, and every floor particle still follow the actual GPU simulation.
