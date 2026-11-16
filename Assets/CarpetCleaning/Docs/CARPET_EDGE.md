# Carpet edge: no bare strip along the rim (2026-09-19)

Request: at every carpet edge a strip of fibres always stayed visibly "untouched" between the foam (or water) on the
carpet and the foam on the tile (user screenshots: near edge and right edge). The user guessed the fibres seen from the
side; that is only the smallest part of it.

## 0. What the strip was
Measured after a foam spray + brush at the near edge (rim column u = 0.5, rows 0..14 from the rim inward):

| | rim cell | +1 | +2 | +3 | +4 | +5 | +6 | +7 | +8 | +12 |
|---|---|---|---|---|---|---|---|---|---|---|
| F before | 0.004 | 0.19 | 0.36 | 0.50 | 0.62 | 0.74 | 0.86 | 0.96 | 1.06 | 1.22 |
| h before (mm) | 0.001 | 0.067 | 0.115 | 0.141 | 0.158 | 0.180 | 0.207 | 0.230 | 0.241 | 0.200 |
| F after | 1.95 | 1.86 | 1.71 | 1.55 | 1.40 | 1.31 | 1.27 | 1.26 | 1.28 | 1.36 |
| h after (mm) | 0.197 | 0.225 | 0.248 | 0.253 | 0.253 | 0.262 | 0.279 | 0.294 | 0.298 | 0.235 |

while the first floor cell outside the rim held F 4.9 (the drip band, `FLOOR_SYSTEM.md` 1.1). Three things stacked up:
1. **Simulation boundary layer.** The open edge (`K_Flux` ghost neighbour: h = 0, bed 5 mm lower = `EDGE_DROP`) drained
   the rim cell's water every substep, however thin the film; the foam followed it because foam moves with
   `FOAM_SLIP * water velocity` and velocity = flux / depth is large for a thin film. So the last ~10 cells (30 mm) of
   carpet were always bare of foam and dry-looking (little pile moisture), whatever the tools did.
2. **Two mounds with a valley.** The floor foam heaped up right outside the rim (F 4.9, 35 mm tall) but sat on the tile
   (-3 mm) because the cross-domain field lifted the tile-side base only by the carpet's own rim F (~0): a valley
   between the carpet mound (ending 9 mm before the rim) and the tile mound. From the 55-degree camera a 35 mm mound
   hides only ~10 mm of ground behind it, so the valley floor - bare rim cells + the fibre wall - stayed visible.
3. **The fibre wall.** Eye rays entering through the open side of the shell stack see the lower (caked, dark) shells over
   the last ~2.5 mm. Real and acceptable (a carpet edge does show its pile from the side); it only reads as a "strip"
   when nothing covers it.

## 1. Simulation fix (`CarpetSim.compute`, `SimCommon.hlsl`, `SimSettings`)
- New setting `EDGE_LIP = 0.3` mm (uploaded as `_EDGE_LIP`, in `SimSettings.asset`): the bound edge holds a film this
  deep. On ghost faces only `h_f = max(h - EDGE_LIP, 0)` takes part in the flux cross-section, the head difference and
  the tool blend (`fTool = h_f * DX * ...`). Puddles (hose, > 1 mm) still run off; the film stays. Interior faces are
  untouched.
- `FoamOutflow`: on ghost faces the water-slip part of the foam velocity is gated by
  `lipGate = saturate((h - EDGE_LIP) / (0.5 EDGE_LIP))`; the tool push part is not (a brush still sweeps foam off the
  edge onto the tile). Interior faces unchanged.
- Foam slump diffusion uses clamped taps (`SoapInC`) instead of skipping the rim cells: zero-gradient boundary, the rim
  row takes part and the sum is conserved (the exclusion left a one-cell trench along every edge and was not
  conservative at the boundary).
- Consequence to know: foam that arrives at the rim while the water is below the lip stops there. Right after a stroke
  toward the edge the rim cell can hold a 1-cell heap (right-edge brush test: F 11.6 vs 5.3 in the next cell), which
  the slump diffusion (D_FOAM 20 mm^2/s) relaxes within a few seconds (6.96 / 5.9 / 4.8 / 3.9 after 3 s). While it is
  sharp it renders as a thin ridge along the rim; in normal play (frames ticking) it is gone before it is noticed.
- Runoff feature preserved: hose held 3 s at u = 0.05 -> floor water mean 0.079 mm/cell (0.08 before the change).

## 2. Rendering fix (`CarpetFoam.shader`, `FloorRenderer.cs`)
`FieldAt(mm, out surfMm)` now returns (sigma, F, Fd, Fage) for the shading and the SURFACE height as the union of two
heaps that both cross the rim (`Rim()` gives the geometry):
```
carpet mound: inside  = carpetBase + foam(F_c)
              outside = lerp(floorBase, carpetBase, lift) + foam(F_c(rim) * fall),  fall = 1 - smoothstep(0, 45 mm, dOut)
                        lift = smoothstep(0.03, 0.12, F_c(rim)) * fall              (base follows the carpet top only where the film shows)
tile heap:    outside = floorBase + foam(F_f)                                        (F_f sampled >= half a floor cell outside the rim:
              inside  = floorBase(rim) + foam(F_f(rim) * fallIn), fallIn = 1 - smoothstep(0, 24 mm, dIn)   under-carpet floor cells are zero)
surface = smax(carpet mound, tile heap, 6 mm);   F_shade = max(F_c * fall, F_f * fallIn * show)
```
- `lift` is blended from 1 at the rim to `smoothstep(F_c)` over the first 6 mm outside, so the surface is continuous at
  the rim for thin films too; `show = smoothstep(-3, 1, tileHeap - carpetMound)` inside the carpet (1 outside) keeps the
  tile heap's film, colour and caps off the rim band wherever the heap stands below the carpet surface (the height
  union alone only hid its geometry). Inside a corner square both rims are sampled (`mmF2`, `fallIn2`); outside, the
  floor tap keeps half a cell clear of every rim line per axis (the under-carpet floor cells are zero). A vertex whose
  rounding residue puts it 1e-4 mm outside the rim counts as on the rim (`outside = dOut > 1e-3`).
- Shadow march: each tap is weighted by the film there, so the 20 mm carpet block beside bare tile never casts the
  55 % skirt (the scene has no shadow maps anywhere, the carpet does not shadow the tile either).
- No valley: whichever heap is taller shows; a tile heap taller than the carpet top + carpet foam overhangs the rim
  (foam heaped against the carpet's side), the carpet mound runs down onto the tile as before. A low tile heap next to
  a bare carpet does NOT climb the rim (it is seated on the floor base: it needs > 23 mm of foam, F > 1.75, to show
  over a 20 mm pile).
- Bubble seats use the union height (`tilt = surfC - surfMm`); `foamMm = FoamMmAt(F)` is still computed for colour /
  AO only.
- Silhouette exit test: min over 4 taps (6, 12, 24, 40 mm behind the pixel) of "ray clear of the foam" (`ExitTap`),
  a tap without a film counts as clear. The old 2-tap max test fired on every back-slope steeper than ~0.4 (lumps, the
  rim ridge) and punched pinholes into the film.
- Mesh seam: the carpet foam grid (3.125 mm) and the floor foam grid (9.375 mm) meet on the rim lines (the floor grid
  lines coincide with the carpet rect). The carpet's rim vertices now take the height the floor grid rasterizes along
  the rim (linear between the two enclosing floor grid points, `RimVertexHeight`, pitch from the new global
  `_FloorFoamPitchMm` set in `FloorRenderer.Awake`), so both meshes share one rim edge: no cracks, no stepped caps.
- Cost: near the rim (every tile pixel, the 24 mm rim band) a `FieldAt` call is 3 carpet + 2 floor taps + 2 `FoamMmAt`
  (2 noise taps each, skipped exactly where F <= 0.05), and the fragment makes 35 `FieldAt` calls (own, 4 gradient,
  8 shadow, 18 bubble seats, 4 exit); interior carpet foam pays 5 taps per call. Not measured separately; whole-carpet
  foam was 12.9..14.6 ms at 1440p before.
- Both foam materials must keep identical `_FoamHeight / _FoamKnee / _LumpMm / _CapLiftMm` (`ApplyFoamLook` writes
  both): each mesh evaluates its rim edge with its own material, a mismatch steps the seam.
- `FloorRenderer.foamSubdiv` must be a multiple of 24 (3600 / fq divides 150 mm, so the grid lines lie on the carpet
  rect); `Awake` warns otherwise.

## 3. Verified (live Editor, scripted `CarpetDebugApi` frames, captures in `Assets/Screenshots/edge_*.png`)
- `CarpetSim.compute` and `CarpetFoam.shader` import with 0 messages, dispatched once after import (no lazy kernel
  errors), C# recompiled clean, no console errors in any test.
- `edge_repro.png` (before) vs `edge_fix_v3.png` / `edge_fix_1440_v4.png` (after): foam spray + brush at the near edge
  and at the right edge - the dark strip along both edges is gone, foam is continuous from the carpet onto the tile.
- `edge_sprayonly*.png` (spray 45 mm from the rim, no brush), `edge_hose.png` (hose at the left rim: the wet dark look
  reaches the edge, the puddle forms on the tile beside it), `edge_floorspray*.png` (foam only on the tile: a small
  overhang over the rim, nothing else on the carpet), `edge_corner.png` (spray + brush through the corner): no strip.
- Two-mesh seam: per-column check at 1440p, the carpet foam's last pink row and the floor foam's first pink row differ
  by exactly 1 px along the whole rim.

- The bubble-seat 3x3 search and the 8-tap shadow march are `[loop]`, not `[unroll]`: with `FieldAt` inlined 26 times the
  compiled fragment shader grew so large that the D3D12 compile stalled the main thread ~30 s and the Editor then lost
  the GPU (`d3d12: Unrecoverable GPU device error`, 2026-09-19). Keep them dynamic; the compile check now takes ~4 s.

## 4. Knobs / open
- `EDGE_LIP` (0.3 mm): larger keeps more film and foam on the rim; 0 restores the old open edge.
- `EDGE_IN_MM` 24 / `EDGE_OUT_MM` 45 / smooth-max 6 mm are `#define`s in `CarpetFoam.shader`.
- The rim heap right after a stroke could be relaxed faster by letting the excess topple over the lip (export
  `D_FOAM * max(F_rim - F_inward, 0)` per substep into `_EdgeOut`); not done, the diffusion handles it in seconds.
- Water rendering still ends at the rim (the water grids are per domain, no union); a hose puddle at the rim shows the
  fibre wall for ~2.5 mm. Acceptable so far.
- Review note (contested, pre-existing): `K_FInject` adds the carpet's exported slurry / surfactant / foam dirt to the
  2.25x larger floor cells without an area scale while water, F and F*age are divided by `FLOOR_AREA`. One reading says
  these are cell totals and exactly conserved; the other that their per-depth concentrations jump 2.25x at the rim
  (tile foam from the carpet renders dirtier). Not changed; check `FLOOR_SYSTEM.md` 1.1 before touching it.
