# Squeegee: a plow, not a velocity (tool 5, hotkey 6)

Behind the blade there is no foam and no standing water, at any drag speed, and the pile is left damp (`SQ_W_DAMP`).
What the blade collects rides ahead of it as a roll (soaked pile + a thin film + pink foam) and pours onto the floor
when it is pushed over the carpet edge. Code: `CarpetSim.compute` (K_Stamp tool 5, K_PlowScatter / K_PlowApply /
K_PlowDrain / K_PlowClear), `ToolController.EnqueuePlow`, `CarpetSimulation.EnqueuePlow` / `DispatchPlow`.

## 1. Why it was rewritten (2026-09-24)
The squeegee used to write `vTool = vStroke` into `_ToolFlow`; K_Flux imposed that flux on the water and FoamOutflow advected
the foam with it. That push existed only on frames where a stamp landed. `StrokeSegment` lays a real stamp every
`spacingMm` (15 mm) of travel and a HOLD stamp (`speed = 0, vStroke = 0`) on the frames in between.
- **Slow drag:** foam was pushed on 1 frame in N, which left ribs at the 15 mm spacing.
- **Fast drag:** above the ±600 mm/s foam clamp and the CFL limit (DtSim 1/240, DX 3.125 mm), the foam fell behind the blade.

Only a narrow band of speeds looked right. Any velocity field has the same limits, so the transport is now geometric.

## 2. Frame flow
`StrokeSegment` (mouse, `CarpetDebugApi.StrokeNow/Hold`, `DebugLiveRunner`) -> `ToolController.EnqueuePlow`: one sweep from
the previous blade (centre P0, axis a0) to this one (P1, a1), `sign` = travel sign along the axis.
- **a0:** the previous axis only when the sweep continues the last one (same point < 1 mm, turn < ~20 deg, same stroke).
  Otherwise a0 = a1: a stroke start or heading snap would rotate the 220 mm blade through a bogus fan.
- **Long segments:** split into `SQ_SUB_MM` pieces, at most 8.

`CarpetSimulation.StepFrame`: K_ClearEdge -> K_Stamp -> K_Emit -> **per sweep: K_PlowScatter (ping-pong fluid + soap, swap) ->
K_PlowApply** -> **K_PlowDrain (every frame)** -> substeps -> K_Chemistry -> K_Fiber.
- Sweeps are dropped while `CarpetAway` (the floor sim still gets the stamps).
- They are also skipped when blade + roll + look-ahead miss the carpet rect.
- `ResetCarpet` clears the queue and zeroes the accumulators (K_PlowClear).

UAVs: Scatter 3 (_FluidOut, _SoapOut, _PlowAcc), Apply 4 (+ _FiberOut), Drain 3 (_PlowAcc, _EdgeOut, _EdgeOut2), Clear 1.
K_Stamp stays at 8/8: its squeegee branch only lost code.

## 3. K_Stamp tool 5
- **Pile to damp**, by footprint `m` (not pressure, so a right-button pass leaves no water either):
  - `w > SQ_W_DAMP`: wrung out into `h`, only as far as `H_MAX - h` has room (so the clamp deletes nothing);
  - `w < SQ_W_DAMP`: surface water is pressed into the pile up to it. After a hose pass `w` is only 0.1-0.2 (WET_LOOK.md), and
    this is what makes the strip read damp.
- Fibre pressing as before (`f >= 0.85 mp`, bend along the stroke). K_Fiber's set capture (aim 0.6 / 0.85) works because the
  pile is left at wetness 0.3 (`wetSet` about 0.74): the stripes stay.
- Since 2026-09-27 it presses dry and damp pile too: capture gain `max(wetSet, SQ_SET_DRY)`; on dry pile the stripe (b ~0.55
  along the stroke, f ~0.8) stands back up over a few seconds (`FIBER_SET.md` 7).
- **Removed:**
  - the `vTool / wt / wf` push;
  - the `agit` term: K_Chemistry `K_GEN * agit * soapy * wet` regrew foam behind the blade.

## 4. Geometry of one sweep (K_PlowScatter)
End-blade frame: `n1 = sign * a1` (travel), `t = perp(a)` (along the blade). Cell coords: `sigma = (p-P1).n1`, `tau = (p-P1).t1`.

| quantity | formula / value | meaning |
|---|---|---|
| column travel | `D = clamp(((P1-P0) + tau (t1-t0)).n1, 0, 400)` | the tau term is rotation: the outer end of a turning blade sweeps further |
| lateral slide | `T = (same vector).t1` | how far the blade slid along itself |
| swept band | `sigma in [-r - D, bs)` | r = 15 mm (tool radius) |
| roll start | `bs = r + 1.5 DX` | greater than r + sqrt(2) DX, so no bilinear tap of the roll ever lands on a cell under or behind the body |

Lateral coverage `eta` (`PlowLateral`):
- The body passes `sigma` for `mu` in `[(-r - sigma)/D, (r - sigma)/D]`, and at that moment the point is at blade coordinate `tau + mu T`.
- `eta` = 1 if any of those lies within ±L/2 (4 mm soft edge, as `FootprintWeight`). This is the hull of a sliding / rotating blade.
- Ahead of the body only the end position counts.

## 5. Packing and moving
- **Fullness:** `max(h / SQ_H_PACK, F / SQ_F_PACK)`; each cell takes at most one cell of room in the roll.
  - A deep puddle or thick foam ahead keeps its density and moves as a block only once the roll reaches it.
  - With room = raw fullness, a puddle denser than the pack pushed itself forward before the roll touched it: "packed" 0.5 mm
    against 1 mm puddles shoved the whole puddle 300 mm ahead and off the edge.
- **Slot:** `cumBefore` = integral of `eta * min(fullness, 1)` along -n1 from the cell back to the rear edge (bilinear samples,
  midpoint rule, exact last step). Slot centre `y = bs + cumBefore + 0.5 * room`.
- **Who moves:**
  - every swept cell (`sigma < bs`, so `y >= bs`);
  - a cell ahead only when the roll overruns it (`y >= sigma + 0.25 DX`). Content never slides back toward the blade, and a
    held blade does not jitter a roll that is already packed. The look-ahead `SQ_LOOKAHEAD` bounds the integral.
- **What moves:**
  - `eta` of `h`, `F`, `Fd`, `F*Fage` (age clamped 600 s);
  - `eta * phi` of `s` and `sigma`, where `phi = h / (h + w W_CAP)`: the dissolved species are cell totals, and only the free
    water's share leaves;
  - u, v are scaled by `1 - eta`.
- **Foam crush:** moved foam keeps `exp(-SQ_FOAM_CRUSH * D)`; the crushed share's dirt goes into the moved slurry. The roll
  slowly turns into dirty water instead of growing without bound. It is the only intended loss, and foam volume is air.
- **Exact conservation:**
  - `q = floor(amount * S)` per channel;
  - integer bilinear shares with the last tap taking the remainder;
  - the source keeps `orig - q/S`.
  - Scales: h, F 2^14; s, sigma, Fd 2^12; F*age 32.
- `_PlowAcc` is `(W+2)(H+2) x 6` uint with a one-cell **ghost ring**. Taps beyond the carpet clamp onto the ring, which is the
  spill reservoir.

## 6. Deposit (K_PlowApply) and spill (K_PlowDrain)
- **Apply** adds the accumulators into the new current halves, in place, and zeroes them.
  - Pushed water **soaks into the pile first**, up to `SQ_W_ROLL`; only the rest stands as `h`.
  - A free roll of about 4 mm ran straight back into the cleaned strip when the blade lifted: 46 % of the strip wet again
    after 2.5 s.
  - The blade wrings that soaked band out again (K_Stamp) when it arrives, so the water still travels with it.
- **Drain** runs every frame, also after the stroke. Each ring entry is owned by the boundary cell next to it (corner ghosts
  by the corner cell).
  - It pours at most `SQ_SPILL_H_RATE * dt` of water and `SQ_SPILL_F_RATE * dt` of foam into that cell's `_EdgeOut/_EdgeOut2`,
    in K_Integrate units: water `h DX^2` mm^3, foam `F DX^2`, `F age DX^2`; s, sigma, Fd as amounts.
  - The pacing exists because FloorSim K_FInject clamps floor F in its 10 mm drip band: a whole roll in one frame would lose
    most of its foam there.

## 7. Settings (`SimSettings`, header "Squeegee plow"; `.asset` in sync)
| field | default | meaning |
|---|---|---|
| SQ_W_DAMP | 0.30 | pile moisture left behind the blade (keep < 0.35: HUD hint) |
| SQ_W_ROLL | 0.9 | pushed water soaks the pile ahead up to this before standing on top |
| SQ_H_PACK | 1.5 mm | free film of a packed roll cell; must stay above normal puddle depth (see 5) |
| SQ_F_PACK | 7 | foam of a packed roll cell (0.6 F_MAX) |
| SQ_FOAM_CRUSH | 0.004 /mm | pushed foam collapses with the distance shoved |
| SQ_LOOKAHEAD | 300 mm | roll re-packed this far ahead of the blade |
| SQ_SUB_MM | 60 mm | longest single sweep (ToolController splits longer frames) |
| SQ_SPILL_H_RATE | 400 mm/s | water one rim cell may pour onto the floor |
| SQ_SPILL_F_RATE | 600 /s | foam one rim cell may pour onto the floor |

## 8. Verification (2026-09-24, Play mode, scripted `StepAll` frames)
- **Behind the blade**, 720 mm stroke through hose + foam + brush, band fully covered by the blade, probe inset 38 mm from the ends:

  | speed | foam cells | water cells | h | w |
  |---|---|---|---|---|
  | 150 mm/s | 2.7 % | 11 % | 0.014 mm | 0.29 |
  | 600 mm/s | 1.6 % | 5.9 % | 0.009 mm | 0.29 |
  | 2400 mm/s | 0.1 % | 0 | 2e-6 mm | 0.29 |

  Before the stroke it was 100 % / 100 %, with h 1.27 mm and F 1.26. The residue at slow speed is inflow from the unswept wet
  carpet during the 4.8 s stroke (see 9), not left by the blade: the images show a clean strip. The old tool left ribs at
  every speed except a narrow band.
- **Whole carpet:** hose + foam + brush everywhere, then six overlapping strokes off the top edge at 600 mm/s. Carpet foam cells
  0, water cells 0, max h 0.016 mm, max F 0.012, pile w 0.23 mean (max 0.30); spill ring drained.
- **Conservation**, evaporation off (runtime clones of both settings): carpet + floor + ring water 1,094,385 -> 1,094,377 mm^3
  (7 ppm); dirt carpet + floor 328,938.2 -> 328,937.4. An edge push put 154 mL onto the floor.
- **Edge cases:** diagonal, reversal, 2 s hold, circle (rotation every frame), 30 fps flick off the edge and onto the floor,
  `CarpetAway`: no NaN, no console errors, ring drained. The circle leaves a little foam on the outer edge of the swept ring,
  like a real side trail.
- **Cost:** 120 frames incl. a sync readback = 230 ms with the squeegee, 239 ms idle, 239 ms brush: not measurable.
- Full chain scraper -> dry vac -> hose/foam/brush -> squeegee -> wet vac -> dryer: water 0.246 -> 0.006, foam 0.319 -> 0.009,
  pooled 0.33 -> 0.002 after the squeegee; the wet vac then flushes the slurry from the damp pile. Capture `Screenshots/sq_strokes.png`.

## 9. Known limits / open
- **Seepage into the strip:** the pressed strip sits about 1 mm lower (pile head `BED_PILE * (pileTop - PILE_H)`, flattened
  pile), so free water from unswept wet carpet next to it seeps in over seconds. With `BED_PILE = 0` it was 10x less, but that
  is the global pile model and the approved fibre-set look. Overlapping strokes pick it up.
- **Roll left mid-carpet:** a roll dropped in the middle relaxes back a little when the blade lifts (a lot of collected water;
  0.5 mm mean near the roll after 2.5 s in the 720 mm test). Pushing it off the edge is the intended technique.
- **Reversal:** dragging back leaves the roll behind as a ridge, as with a real squeegee.
- **Roll foam:** can reach the `F_MAX` clamp at the front (foam is crushed there anyway); the foam roll looks slightly lumpy.
- **Not changed:** the floor squeegee (`FloorSim.compute` tool 5) and the debris push (`Debris.compute`) still use the per-stamp
  velocity.

## 10. Debug API
- `CarpetDebugApi.SurfaceProbeJson(u0, v0, u1, v1)`: mean / max h, F, w, s and the fraction of foam (F > 0.05) and water
  (h > 0.05 mm) cells.
- `CarpetDebugApi.PlowSpillJson()`: what waits in the ring to pour onto the floor.

## 11. POWER upgrade (2026-09-24, `ECONOMY.md` 2)

The blade still leaves no free water or foam at any level. POWER only sets how damp the pile stays:

- **K_Stamp tool 5:** `tgt = clamp(_SQ_W_DAMP * pow(ToolPower(5), -0.3), 0.20, 0.34)`.
- **K_PlowScatter:** the foam crush rate is multiplied by `ToolPower(5)`.

Measured, pile w after one pass over a soaked band:

| Level | Pile w |
|---|---|
| 0 | 0.34 |
| 3 (factory, the old 0.30) | 0.30 |
| 6 | 0.264 |

The width upgrade stretches the blade length (220 → 660 mm at AREA 6, ×3). `PlowReachesCarpet` and the rotation fan follow
`halfLengthMm` automatically.

**The ×3 blade** (2026-09-24, `ECONOMY.md` 2):
- A straight pass over soaked, foamy pile leaves h 0 and F 0 behind.
- On a 350 mm arc the middle and outer end are dry too.
- **Pivot limit:** when the turning radius is below half the blade, the inner end barely moves and some water stays near the
  pivot. The ×3 blade leaves h 0.78 mm; the factory blade on the same relative turn leaves 1.70 mm. This is old plow
  behaviour, but a long blade makes such turns common. A candidate fix is to re-collect the roll at the slow inner end.
