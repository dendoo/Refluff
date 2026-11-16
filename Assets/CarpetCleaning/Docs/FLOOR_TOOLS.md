# Floor detergent + mop (2026-09-20)

User request: two more tools in the HUD, on the RIGHT, for cleaning the FLOOR. No hotkeys needed, mouse only.
A floor detergent and a mop, where the mop cleans the tile **perfectly** wherever detergent has been sprayed
and only smears where it has not.

## 1. Tools

`ToolId.FloorCleaner = 8`, `ToolId.Mop = 9`. `ToolDefinition` gained `bool floorTool`, which is what splits the
HUD strips (never an index range), and `ToolTable` gained `StripCount`, `NextOnStrip` and `FirstOnStrip`.

| Tool | Name | Hotkey | radius | length | blade | spacing |
| --- | --- | --- | --- | --- | --- | --- |
| 8 | Detergent | none (mouse) | 85 mm | - | no | 20 mm |
| 9 | Mop | none (mouse) | 30 mm | 320 mm | yes | 18 mm |

The mop is the widest blade in the game (the squeegee is 220 mm). Both are mouse-only (`hotkey = 0`, the user's call):
the HUD strip and, since 2026-09-26, the shop cards show them without a key chip (the shop used to print a "·" there,
`HUD.md` 9). `hotkey` is a number-row slot 1..10 (10 = the 0 key). The scroll wheel uses `ToolTable.NextOnStrip`, so it still cycles
the eight carpet tools exactly as before and only cycles Detergent/Mop when a floor tool is already selected.

Neither tool has a branch in `CarpetSim.compute` K_Stamp, so they are inert on the carpet by construction.

## 2. Where the detergent film lives

`_FDeposit` is `(dust, residue, wetFilm, detergent)`. The fourth channel was unused and is passed through by
both `K_FInject` and `K_FChem`, so the film costs **zero new render textures and zero UAVs** (the cap is 8 per
kernel; `K_FInject` stays at 6, `K_FChem` at 3). `FloorSurface.shader` already samples `_FDepositTex`, so the
look needed no new global either. Do not "clean this up" by giving the film its own RT: `K_FInit` would go to 8
UAVs and the next feature would silently break its bindings.

## 3. Model

### Detergent (`K_FInject`, `st.tool == 8`)
```
det   += min(F_DET_MAX - det, F_DET_RATE * mp * dtS)
sigma += F_DET_SOAP * mp * dtS
h     += min(F_H_MAX - h, F_DET_WATER * mp * dtS)
```
No `agit` and only a trace of surfactant on purpose: `K_FChem` generates foam from `agit * soapy * wet`, so a
spray bottle would otherwise raise a pink foam mound and steal the foam sprayer's identity. A brush dragged over
a sprayed patch *does* foam, which is a fair discoverable combination.

### Film ageing (`K_FChem`)
- `detv = saturate(det / F_DET_REF)` and `wetEff = max(wet, detv)`: a liquid film counts as wet for dissolving
  dust and residue, but never for foam generation.
- The drying rule is guarded: `if (h < 0.05 && det < 0.02) { residue += s; s = 0; }`. Without the `det` guard the
  dirt the film has just dissolved re-deposits as residue along the ragged fringe of every spray footprint, i.e.
  spraying would visibly **darken** the floor.
- `kR` (residue to slurry) gains `F_DET_K_RES * detv`: the film loosens what is under it but never removes it.
  That is the mop's job.
- Drying and dilution: `dDry` on a dry tile (`F_DET_DRY`, ~65 s), `dWash` only where **standing** water is
  deeper than 0.6 mm, so the 0.2-0.4 mm the spray leaves behind cannot wash away its own film. A film that is
  never mopped leaves `F_DET_TO_RES` of itself behind as a faint soapy haze.

### Mop (`K_FInject`, `st.tool == 9`)
```
k = lerp(F_MOP_K_DRY * mopStreak, F_MOP_K, detv) * mp
dust, residue, s  -= DecayAmount(..., k, dtS)      (residue x lerp(0.35, 1, detv): dried rings prefer the film)
water: dh = min(h - F_MOP_FILM, F_MOP_SOAK * mp * dtS), and its slurry / surfactant leave with it
foam:  dF = min(F, F_MOP_FOAM * mp * dtS), Fd proportional
wetFilm = lerp(wetFilm, min(wetFilm, F_MOP_WET), mp)   -- a CAP, not an imprint: the head WRINGS the tile
det -= DecayAmount(det, F_MOP_EAT * mp, dtS)
vTool = vStroke * 0.9, wt, wf = 1                  (pushes water like a squeegee)
```
`mopStreak = 0.25 + 0.75 * Fbm(c * 0.04 + 11.3)` is a static ~25 mm mottle **hoisted out of the stamp loop**, so
a dry mop leaves real streaks instead of an even smear and an Fbm never runs per stamp.

The mop adds no water and no agitation: it wipes, it does not whip, and a dry mop cleans only weakly and streakily.

**2026-09-20 change — the mop now dries as well as it cleans.** `wetFilm` used to be `max(wetFilm, F_MOP_WET * ...)`,
i.e. the head *stamped* a dark damp memory of 0.9 onto everything it touched, which then took `F_TAU_WET` = 45 s to
fade: a freshly mopped floor read soaked for a minute, the exact opposite of what a mop does. It is now a **cap**
(`lerp(wetFilm, min(wetFilm, F_MOP_WET), mp)`, `F_MOP_WET` 0.22): a soaked tile is pulled DOWN to a faint sheen, a
dry tile is left dry, and the soft footprint edge dries less than the centre rather than more. With `F_TAU_WET` 16 s,
`F_MOP_SOAK` 320 mm/s and `F_MOP_FILM` 0.008 mm (alpha `1 - exp(-h/0.12)` = 0.06, barely a sheet, gone in ~1 s at
`F_E_SURF` 0.008) the tile reads dry within about two seconds of the pass.

### Chunks (`Debris.compute` K_DebSim)
`st.tool == 9 && !onCarpet`: shards smaller than `D_MOP_PICK_MM` (13 mm, i.e. above `D_CHIP_SIZE_MAX` 12, so in
practice everything) are absorbed like a vacuum, bigger ones
are swept along the head at `vStroke * 0.7`. Purely pushing them would never let the tile read as clean; purely
vacuuming a 12 mm shard would look wrong. The stamp loop in that kernel is now explicitly `[loop]` (it contains a
`continue`, and an accidental unroll is the documented "Kernel at index (N) is invalid" trap).

## 4. Look (`FloorSurface.shader`)

With `det > 0.002`: `detc = saturate(detv * (0.55 + 0.9 * fbm3(cmm * 0.05 + 23)))` gives a ragged coverage edge,
so a sprayed patch never reads as a hard-edged decal. It then feeds the existing wetness path
(`wetv = max(h/0.15, wetFilm, detc)`) and, on top of the dust and residue overlays, tints and slicks the tile:
`albedo *= 0.90 * _DetTint` (0.86, 0.94, 0.98 = a 14 % cool shift, never a coloured overlay), `smooth -> 0.96`
(plain wet tile is 0.92), plus a sparse 4.5 mm glint (`_DetSparkle`, ~10 % of cells) added next to the wet
fresnel term. Cost: one `fbm3` and one `hash1` behind a branch that is false everywhere nothing was sprayed.

The film deliberately sits **over** dust and residue: soaked dirt should look wet, and only the mop removes it.

## 5. Constants (`FloorSettings`, `Settings/FloorSettings.asset` in sync)

```
F_DET_MAX 1      F_DET_REF 0.16   F_DET_RATE 7.0   F_DET_WATER 0.35  F_DET_SOAP 0.15
F_DET_DRY 0.015  F_DET_WASH 0.5   F_DET_TO_RES 0.10   F_DET_K_RES 0.60
F_MOP_K 260  F_MOP_K_DRY 8   F_MOP_EAT 45   F_MOP_SOAK 320   F_MOP_FILM 0.008   F_MOP_FOAM 120   F_MOP_WET 0.22
D_MOP_PICK_MM 13      (supporting: F_TAU_WET 16, F_E_SURF 0.008, F_K_DUST_DIS 6)
```
Dwell time sets the rates: a cell sits under the mop head for about `2 * radius / speed`, so 0.15 s at a 400 mm/s
pass. `260 * 0.15` leaves `exp(-39)` of the dirt (spotless in a single pass, which is the point), `8 * 0.15`
removes 15-70 % depending on the streak mask, so a dry mop is useful but visibly streaky.

History of the two tunings:
- `F_DET_RATE` 1.2 -> 3.0 and `F_DET_REF` 0.35 -> 0.30 after the first Play test: a brisk 630 mm/s spray pass only
  reached `det 0.32`, so the mop ran at a fraction of its rate and removed 64 % instead of ~99 % of the residue.
- 2026-09-20, on the "detergent + mop should feel decisive" note: `F_DET_RATE` 3.0 -> 7.0 and `F_DET_REF` 0.30 ->
  0.16, so even a fast single pass arms the mop at full rate; `F_DET_K_RES` 0.08 -> 0.60, so the spray visibly eats
  dried rings on its own while it sits; `F_MOP_K` 60 -> 260 and `F_MOP_K_DRY` 1.5 -> 8. `F_DET_WATER` 0.5 -> 0.35
  and `F_DET_TO_RES` 0.25 -> 0.10 keep the stronger film from leaving more to dry and more haze behind.

## 6. Verified in Play mode (2026-09-20)

- **Spray**: `Reset(1); Hold(8, -0.25, 0.5, 1)` -> detergent mean 0 -> 1.33e-3, water +6.3e-4, **foam stays 0**
  (the no-agit rule), dust/residue untouched, carpet stats untouched. After 1 s the film is still 1.329e-3, i.e.
  it does not wash itself away.
- **A/B on an identical mess** (scrape over the left edge, wash the carpet, squeegee the dirty water onto the
  tile, dryer to set it as residue): mess = residue 4.30e-4, slurry 1.31e-3, dirtyCells 0.0202.

  | | residue | slurry | water | dirtyCells |
  | --- | --- | --- | --- | --- |
  | spray + 1 mop pass | 1.13e-4 (-74 %) | 3.19e-4 | -55 % | 0.0057 |
  | dry mop, 1 pass | 4.41e-4 (-0.5 %) | 3.37e-4 | -54 % | 0.0078 |
  | dry mop, 3 passes | 4.06e-4 (-8 %) | 2.99e-4 | -58 % | 0.0075 |

  Water and suspended slurry come off either way, which is right: a mop soaks up water regardless. **Dried-in
  residue only comes off with detergent.**
- **Per pixel** (`SaveFloorView("fdeposit")`, inside the 1555 px residue footprint): mean 39.2 -> 7.9 after one
  detergent pass (-80 %), dirty pixels 100 % -> 27 %. A dry mop x3 reaches 35.1 (-10 %) and **spreads** the
  residue (nonzero pixels 3406 -> 5819), which is exactly the intended smear.
- **Two overlapping spray + mop passes**: 40.5 -> 0.99 (-97.6 %), dirty pixels 100 % -> 5 %. The damp trail goes
  216 -> 71 over the following 50 s.
- **Chunks**: one mop pass took the live particle count from 152 to 101 while the count inside the carpet rect
  stayed put (only tile shards are collected).
- Compute and shader imports report 0 messages, and re-reading them after a dispatch (the lazy-error gate) is
  still 0. Recompile clean, no console errors.

## 7. Open

- Dust deposits on the tile are still hard to produce: scraper dust puffs are born over the carpet and usually
  die there, so `dust` stayed 0 in every test. The mop's dust term is therefore untested against real data.
- No floor readout in the HUD. `FloorStatsSync` blocks (`WaitForCompletion`) and must never be called per frame;
  a floor bar needs an async readback path like the carpet's, plus the new `dirtyCells` stat.
- The detergent has no cost or limit, so spraying the whole floor is free.
