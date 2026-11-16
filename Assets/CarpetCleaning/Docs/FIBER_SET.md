# Fiber set: the pile stays where it was laid while it is damp; dry pile stands back up

Workflow the user asked for (2026-09-21, replaces the earlier "dries pressed" rule):
1. Tools pressing **wet** pile (scraper, brush, squeegee, wet vacuum) leave it bent along the stroke and flattened. Those are the stripes.
2. The **wet vacuum cannot dry the carpet**. It takes all the foam and free water, but the pile stays damp
   (`WETVAC_W_MIN` = 0.25 of W_CAP), dark from moisture, with the stripes still in it.
3. The **dryer** finishes the job. As the pile dries, the set relaxes and the fibres stand up.
   **A fully dry fibre at rest is always upright.**

Added 2026-09-27 (user: "2, 3 (water) and 6 do not move the fibres"; tools counted by HUD hotkey):
4. **Water lays the pile the way it spreads** (hose, key 3): until the pile is dried, or pressed another way by a tool or
   by another flow (section 5).
5. **The dry vacuum sucks the pile in** (key 2): the pile around the round nozzle leans into it, the pile under its middle is
   pulled up; a moving head leaves the pile combed its way (section 6).
6. **The squeegee presses the pile the way it travels** (key 6) on dry pile too, not only on wet pile (section 7).
7. Rule 3 still holds: a lay on dry pile springs back up over a few seconds (`SET_DRY_RELAX`, section 4).

## 1. State
`_FiberSet` (R32G32B32A32_SFloat, 384x480, single buffer written in place). `K_Fiber` is its only reader and writer.

| channel | meaning |
|---|---|
| xy | `bSet`: remembered bend, \|bSet\| <= 1, same space as `_Fiber.xy` |
| z  | `fSet`: remembered flattening 0..1 |
| w  | unused (0) |

`K_ClearFiberSet` zeroes it in `ResetCarpet`. That covers the handover's new carpet and `CarpetDebugApi.Reset`.
It is not done in `K_Init`, because that kernel is already at the UAV cap.

## 2. Model (`K_Fiber`, CarpetSim.compute)
- The bend spring rests at `bSet` instead of 0: `vb += (-k (b - bSet) - c vb + Fext) dt`.
- Flattening relaxes toward `max(0.7 w, 0.6 crustCover, fSet)`.
- `K_Fiber` loops over this frame's `_Stamps` itself. The buffer is an SRV, so `K_Stamp` stays at 8/8 UAVs.
  `K_Fiber` has 5 UAVs: `_FiberOut`, `_FiberVel`, `_FluidOut`, `_Dirt`, `_FiberSet`.
- Order per cell and frame: flow lay (section 5) -> stamps (dry-vac pull, section 6, then the pressing / fluffing tools) ->
  drying relax (section 4) -> spring.
- Wetness gates:
  - `wetness = saturate(w + h / H_FILM)`;
  - `wetSet = smoothstep(SET_W_LO, SET_W_HI, wetness)`: capture strength;
  - `relaxGate = 1 - smoothstep(0.2 SET_W_LO, SET_W_LO, wetness)`: drying releases the set;
  - `dryGate = 1 - smoothstep(0.2 SET_W_LO, 0.5 SET_W_LO, wetness)`: dry brushing.
- **Capture is swept.** A moving stamp covers the path since the previous stamp. That distance is exactly the tool's `spacingMm`,
  uploaded as `_ToolSpacing[4]` (float4-packed, by ToolId) from `ToolTable`.
  - The cell's share of that sweep over the footprint's along-extent is `overlap / (2E)`:
    - blade: `E = radius + 2`, gated across by the blade length;
    - disc: `E` = the chord `sqrt(R^2 - q^2)`, `R = 0.85 radius`, normalised by R.
  - The shares add up to exactly 1 per full pass. So there is no banding at the stamp spacing, and the result does not depend
    on frame rate or speed. One pass counts as `SET_CONTACT_S` seconds of contact.
  - A hold stamp (speed 0) counts as its `dtS` under the plain footprint and does not bend (no heading).
  - History: capturing per frame was too weak (the blade covers a cell for 1-2 frames). Capturing per stamp banded
    (a 30 mm blade with stamps every 15 mm gives 2 or 3 hits). `travel = speed*dtS` banded at the frame period.
- Pressing tools: `cap = 1 - exp(-SET_RATE * mp * gain * tc)`, `gain = wetSet` (squeegee: `max(wetSet, SQ_SET_DRY)`, section 7).
  - `bSet = lerp(bSet, aimB * dir, cap * mechB)`, `fSet = lerp(fSet, aimF * mp, cap)`.
  - The aims are the same targets `K_Stamp` imposes on `b` / `f`.
- Fluffing tools: `bSet, fSet -= x * (1 - exp(-fluffK * mp * tc))`.

| tool (ToolId) | aimB | aimF | mechB | fluffK |
|---|---|---|---|---|
| 0 scraper | 0.8 | 0.9 | mech | - |
| 1 dry vacuum | pull into the nozzle, section 6 | - | - | `SET_FLUFF_VAC` under the head (wet or dry) |
| 2 hose | through the water it lays down, section 5 | | | |
| 4 brush | 0.8 | 0.5 | mech | `SET_FLUFF_BRUSH * mech * dryGate` (wet brushing sets, dry brushing lifts) |
| 5 squeegee | 0.6 | 0.85 | mech | - (wet: it leaves the pile at `SQ_W_DAMP` 0.3, `SQUEEGEE.md`; dry: gain `SQ_SET_DRY`, section 7) |
| 6 wet vacuum | 0.6 | 0.4 | max(mech, 0.3) | - |
| 7 dryer | - | - | - | none: it evaporates `w` (`DRYER_BOOST`), and the drying relax straightens the pile |

The foam sprayer (3) has no term of its own. Moving, the little water it adds stays under the flow thresholds (a zigzag leaves
|bSet| 2e-8); held 2 s in one spot, its ~3 mm puddle spreads at 25-50 mm/s and lays the pile around it like any flow (0.27 at
40-80 mm). Wet slump without a tool (`0.7 w`) is not remembered.

## 3. Wet vacuum leaves the pile damp (`K_Stamp`, tool 6)
- Free water `h` and foam are removed as before.
- Pile moisture only down to `WETVAC_W_MIN`: `dw = min(max(w - WETVAC_W_MIN, 0), 12 mp dtS)`.
- The air pulled through the damp pile keeps flushing dissolved dirt and soap:
  `frac = max(removedWater / totalWater, 1 - exp(-WETVAC_FLUSH mp dtS))`.
  Without this, slurry trapped in the damp pile could never be removed, and the HUD would keep asking for the wet vac.
- The HUD hint switches from "Wet-vac" to "Dry it" when Wetness <= 0.35, so `WETVAC_W_MIN` must stay below 0.35.
  The win condition (Wetness <= 0.05) now needs the dryer.

## 4. Drying: dry pile stands back up
- `bSet, fSet -= x * (1 - exp(-SET_DRY_RELAX * relaxGate * dt))`. This is the rule that makes dry pile upright. It works with
  any drying: dryer or evaporation.
- `SET_DRY_RELAX` 4 -> **0.6** /s (2026-09-27). At 4 a lay on dry pile was gone in ~0.3 s, i.e. while the squeegee blade still
  covered it, so a dry press could never be seen. At 0.6 the lay on dry pile halves in ~1.2 s and is gone after ~8 s. Drying
  after wet work still ends upright (verified, section 11); the pile now rises over a few seconds once dry instead of at once.
- Fext from the water velocity (`0.0025 (u, v)`) is still in the spring but is negligible (|b| 0.02 at 300 mm/s); section 5 is the real coupling.

## 5. Flowing water lays the pile (`K_Fiber`, before the stamps)
Per cell and frame, from the free water after this frame's substeps (`_FluidOut`: h, u, v):
- `drive = smoothstep(FLOW_V_LO, FLOW_V_HI, |u,v|) * smoothstep(FLOW_H_LO, FLOW_H_HI, h)`;
- `capW = 1 - exp(-FLOW_SET_RATE * drive * dt)`, `fdir = (u, v) / |u, v|`;
- `bSet = lerp(bSet, fdir * max(FLOW_SET_B * drive, dot(bSet, fdir)), capW)`: the water lays the pile its way and never lifts
  pile that already lies its way (a weakening flow does not shrink the lay; a cross flow turns it);
- `fSet = lerp(fSet, max(fSet, FLOW_SET_F * drive), capW)`: it presses the pile down a little, never lifts it.
- No `wetSet` factor: the depth gate already needs water, and a flowing sheet over a dry pile is exactly the case.
- Thresholds from measured flow (seed 1, clean carpet, hose stroke 0.3 -> 0.7 in 1 s; wet cells by |u, v|):
  during the stroke 20-320 mm/s; +0.5 s mostly 10-80; +1 s 5-40; +2 s 2-20; +4 s under 10; +8 s under 5; the backflow of a
  settling puddle is 6-8 mm/s. With `FLOW_V_LO` 10 the lay forms while the water spreads and then freezes.
- Result: a held hose combs a radial starburst around the jet, a moving hose combs the pile outward from its path, and the
  pattern stays (it is the set) until the dryer dries it (section 4) or a tool / another flow re-lays it.
- The squeegee plow moves water geometrically (no u, v), the wet vac removes it: neither makes a flow lay. The brush's swirl does
  move water in arcs, which agrees with its own set.

## 6. Dry vacuum suction (tool 1)
The head is a round disc nozzle (radius `R`, 50 mm at factory tuning, grows with AREA) with the intake in the middle. It has no
3D head model, so the ring cursor shows the whole effect.
- `VacPullWeight(p, R)` (SimCommon.hlsl): `smoothstep(0.35, 0.85, d/R) * (1 - smoothstep(0.9, VAC_PULL_REACH, d/R))`.
  0 under the middle (pulled straight up), 1 around the skirt, 0 at `VAC_PULL_REACH` radii (1.8 = 90 mm).
- `K_Stamp`: for every vac stamp, before the footprint test (the pull reaches beyond the footprint):
  `kp = 1 - exp(-VAC_PULL_RATE * pull * pressure * dtS)`, `b = lerp(b, -VAC_PULL_B * p/|p|, kp)`, `vb *= 1 - kp`.
  The pile leans in at once, so the pile ahead of a moving head leans back toward it before the head arrives.
- `K_Fiber`: the same pull on the set, per real second (`dtS`; the ring changes as the head passes, so it is not a swept
  imprint): `bSet = lerp(bSet, -VAC_PULL_B * p/|p|, kp)`, `fSet -= fSet * kp` (pulled up as well as in).
  Under the head `SET_FLUFF_VAC` still lifts the set.
- A moving head: ahead the pile leans back toward it, under it the pile is lifted, behind it the rear of the ring leaves the pile
  combed the way the head went (at the sides diagonally in toward the path). Dry pile then stands up at `SET_DRY_RELAX`;
  wet pile keeps the comb until it dries (it replaces the old "a dry-vac pass erases the set").
- POWER does not scale it (upgrades never change the pile styling).

## 7. Squeegee on dry pile (tool 5)
- `K_Stamp` already pressed the pile (`f >= 0.85 mp`, `b -> 0.6 dir`), but on dry pile the spring snapped it back within
  ~0.3 s, overshooting backward (bu -0.06 behind the blade). Nothing could be seen behind the blade.
- The set capture now uses `gain = max(wetSet, SQ_SET_DRY)` (0.8) for the squeegee: one pass lays dry pile at ~0.5 along
  the stroke, flattened ~0.8, and `SET_DRY_RELAX` stands it back up over a few seconds. Damp pile (wetness 0.1-0.4, no free
  water to press in) now also gets the full press instead of a weak one; there the relax gate is closed, so it holds like wet pile.
- Only the squeegee: the other pressing tools keep their wet-only capture (not asked for).

## 8. Settings (`SimSettings`, header Fibers; the `.asset` is in sync)
| field | default | meaning |
|---|---|---|
| SET_W_LO / SET_W_HI | 0.10 / 0.40 | wetness band over which pressing starts / fully sets; the drying relax runs from SET_W_LO down to 0.2 SET_W_LO |
| SET_RATE | 10 | 1/s per unit pressure * gain |
| SET_CONTACT_S | 0.25 | s of contact that one full pass of a moving tool counts as |
| SET_FLUFF_VAC | 10 | 1/s per unit pressure under the dry-vac head (one pass removes ~93 % there) |
| SET_FLUFF_BRUSH | 4 | 1/s per unit pressure * mech, dry pile only |
| SET_DRY_RELAX | 0.6 (was 4) | 1/s at full relaxGate: dry pile springs back upright (a dry lay halves in ~1.2 s) |
| WETVAC_W_MIN | 0.25 | pile moisture the wet vacuum cannot remove (keep < 0.35) |
| WETVAC_FLUSH | 4 | 1/s per unit pressure: dirt/soap flushed out of the damp pile |
| FLOW_SET_B | 0.6 | lay a full-strength flow presses in |
| FLOW_SET_F | 0.35 | flattening a full-strength flow presses in (never lifts) |
| FLOW_SET_RATE | 4 | 1/s at full strength |
| FLOW_V_LO / FLOW_V_HI | 10 / 100 | mm/s: no lay below / full strength above (a hose sheet runs at 150-300) |
| FLOW_H_LO / FLOW_H_HI | 0.05 / 0.5 | mm of free water: no drag below / full strength above |
| VAC_PULL_B | 0.5 | lean toward the nozzle at full pull |
| VAC_PULL_RATE | 25 | 1/s at full pull |
| VAC_PULL_REACH | 1.8 | nozzle radii the pull reaches |
| SQ_SET_DRY | 0.8 | squeegee capture gain on pile too dry to set |

## 9. Rendering
`CarpetFibers.shader` draws `b` / `f` from `_FiberTex`. Laid pile reads **darker and more saturated**, never brighter:
- `laid = saturate(|bend| 1.6) (1 - crustCover)`; the albedo moves 60 %·laid toward `base^1.15`.
- The normal tilt from the bend is 0.45 (it was 1.4, which blew out the wet rim and spec into white).
- Spec, aniso and rim are scaled down by `laid`.
- Velvet is `1 - laid (0.20 + 0.08 lean)`: laid toward the viewer is ~28 % darker, laid away ~12 % darker.
- The bend is also real geometry: each shell texel reads the strand whose root is `uv - bend t^2 _BendUV`, so |b| 0.5 puts the
  tips ~12 mm over. At the game camera (2560x1440, ~1 px/mm) a combed patch reads as strands slanted its way, strands laid
  toward the camera as short dots.
- `PC_RPAsset` `m_OpaqueDownsampling` = None. The half-res opaque copy that `CarpetWater` composites under thin films
  (0.03-0.3 mm collect in the lower pressed stripes) blurred them. The cost of the full-res copy was not measured.
  That only removed the blur. The grey veil itself was `CarpetWater` drawing those held films as a flat mirror (Fresnel sky +
  rim), with its normal taken from the ~8 mm pressed-pile step (white/blue edge lines). Fixed 2026-09-25: films under 0.3 mm
  are drawn by the fibres only (0.15-0.3 mm films show at most as sparse, faint drape pools; nothing under 0.15 mm), and the water
  normal comes from the sim head, slope-limited while the sheet is shallower than the tufts (`WET_LOOK.md` 5).
- A/B: `Assets/Screenshots/sheen_before.png` / `sheen_after4.png`.

## 10. Debug
- `CarpetDebugApi.FiberProbeJson(u0,v0,u1,v1)` returns rect means of |b|, bu, bv, f, w, |bSet| and fSet.
- `SaveDebugView("fiberset", path)` writes a PNG: RG = bSet * 0.5 + 0.5, B = fSet (`Assets/Screenshots/fiberlay_setmap.png`:
  hose starburst, hose stroke, vac trail + ring, dry squeegee stripe; smooth, no speckle).
- Radial probes (lean toward / away from a point) are easy in an `eval_file`: read `sim.Fiber` / `sim.FiberSet` / `sim.Fluid`
  like `ReadFloatRT` and project b on `(p - centre) / |p - centre|`.

## 11. Verified
### 2026-09-21 (clean carpet: dirt blitted to 0; probe u,v 0.3-0.7)
| step | \|b\| | f | w | \|bSet\| | notes |
|---|---|---|---|---|---|
| hose zigzag + foam, +2 s | 0.0003 | 0.15 | 0.385 | 0 | stats: water 1.23, foam 0.21, Wetness 0.48 |
| wet vac, 5 alternating stripes, 1st round | 0.43 | 0.37 | 0.258 | 0.43 | |
| 3rd round, then +3 s | 0.566 | 0.39 | 0.247 | 0.566 | water 0.011, foam 0.0004, pileWater 0.25, Wetness 0.25 -> hint "Dry it"; `damp_after_wetvac.png` |
| dryer passes 1-7 (w 0.22 -> 0.107) | 0.567 | 0.36 | 0.107 | 0.566 | the set holds while damp |
| dryer passes 8-10 (w 0.095 -> 0.075) | 0.53 -> 0.034 | 0.13 | 0.075 | 0.52 -> 0.028 | fibres stand up as the pile dries |
| dryer passes 11-18, then +20 s idle | 4e-17 | 0.02 | 0.029 | 0 | fully dry area upright |
| 10 more dryer passes | 1.5e-5 | 0.006 | 0.009 | 0 | colour inside the dried area = fresh carpet (78,107,153 vs 78,110,157); `dried_upright2.png` |

Earlier checks that still hold:
- Wet brush sets 0.79, and a reverse pass restyles it.
- The set map is band-free (`fiberset_map3.png`).
- (Superseded 2026-09-27: "dry squeegee leaves no set" and "dry vacuum removes 93 % in 1 pass".)

### 2026-09-27 (Play, scripted `StepAll` frames, `Reset(1, 0)` = spotless seed-1 carpet unless noted)
Water (hose held 1 s at the centre; rings around the jet; bRad = lean away from the jet):

| ring mm | before: bRad 1 s / +2 s | now: bRad 1 s | +2 s | +5 s | flow at 1 s |
|---|---|---|---|---|---|
| 0-40 | 0.019 / -0.001 | 0.614 | 0.528 | 0.528 | 281 mm/s |
| 40-80 | 0.017 / -0.001 | 0.618 | 0.525 | 0.526 | 247 |
| 80-140 | 0.013 / 0 | 0.539 | 0.540 | 0.541 | 175 |
| 140-220 | 0.006 / 0 | 0.143 (set 0.29) | 0.525 | 0.525 | 141 |
| 220-320 | - | 0 | 0.243 | 0.243 | reached by the spreading water later |

\|b\| = bRad in every ring (a pure starburst). fSet 0.34, f 0.1 -> 0.28 as the pile soaks.

Squeegee, stroke u 0.2 -> 0.8 at 720 mm/s, probe just behind the blade (u 0.62-0.72):

| | +0 s | +1 s | +2 s | +4 s | +8 s |
|---|---|---|---|---|---|
| dry, before (u 0.3-0.5) | b 0.064 **backward** (bu -0.064), f 0.55 | 0.002, f 0.28 | | | |
| dry, now: b (all along +u) / f | 0.55 / 0.83 | 0.27 / 0.66 | 0.15 / 0.46 | 0.04 / 0.20 | 0.004 / 0.03 |
| wet (hose band first), now | 0.51 / 0.85 | | 0.50 held | | |

Dry vacuum:
- Held 0.5 s, rings (bRad < 0 = toward the nozzle): 0-20 mm 0; 20-40 -0.38; 40-50 -0.49; 50-65 -0.50; 65-80 -0.46; 80-95 -0.11.
  Before: 0 everywhere.
- Stroke u 0.2 -> 0.6 at 480 mm/s: trail on the path 0.40 along +u -> 0.20 (+1 s) -> 0.06 (+3 s) -> 0.003 (+8 s); a side band
  bu 0.23 / bv -0.21 (forward and in toward the path); ahead of the head bu -0.30 (leaning back toward it).
- Wet squeegee stripe (+u 0.51) then the vac dragged across it along +v: 0.39 along +v, held after 4 s (wet); fSet 0.78 -> 0.
- Wet squeegee stripe, then the hose held 105 mm beside it: the stripe turns toward the spreading water, 0.27 (bu 0.02, bv 0.24).

Regression (the 2026-09-21 workflow, spotless carpet, probe u,v 0.3-0.7): hose + foam zigzags +2 s: |b| 0.20 (the water's lay;
was 0.0003); wet vac 3 rounds of 5 stripes: 0.40 / 0.48 / 0.51, +3 s 0.505 (w 0.246, water 0.005, Wetness 0.14); dryer, 2 zigzag
passes per step: 0.51 (w 0.11) -> 0.21 (w 0.05) -> 0.04 (w 0.024, what is left is the dryer's own flutter) -> set 7e-5; +20 s
idle: |b| 9e-19, f 0.0004: upright. Full chain on the fully soiled carpet (`Reset(1)`: scraper, dry vac, hose + foam, brush, 8
squeegee strokes off the edges, wet vac, dryer): no NaN, stats sane (clean 0.09 -> 0.19, water 0.75 -> 0 after the squeegee).
Cost: whole `StepAll` chain 2.0-2.5 ms/frame (hose / vac / idle, incl. a sync readback); the added ALU is not measurable.
Compute 0 messages, recompile clean, no console errors.

Captures (2560x1440 game camera, 1:1 crops, `Assets/Screenshots/`): `fiberlay_hose_ab.png` / `fiberlay_hose_ab2.png` (flow lay
off | on, same work, 3 s after), `fiberlay_squeegee_dry.png` (dry stripe 0.25 s | 2.25 s after), `fiberlay_vac.png` (real frames:
trail + the pull under the ring cursor), `fiberlay_setmap.png`, `fiberlay_before_hose_squeegee.png` (before, hose + two wet stripes).

## 12. Open
- The user has not judged the look live yet. Knobs: `FLOW_SET_B/RATE/V_LO`, `VAC_PULL_B/REACH`, `SQ_SET_DRY`, `SET_DRY_RELAX`.
- Under standing water deeper than 0.3 mm the water surface (refraction) softens the combed pattern; it reads fully once the
  water soaks in.
- A settling puddle's first moments (a short backflow above 10 mm/s) shave ~10 % off the lay near the jet (0.58 -> 0.53).
- A sample near the lower edge of the 2026-09-21 test's dryer zone stayed a bit darker (blue 141 vs 155). This is probably
  moisture left at the zone edge. Not probed separately.
- The dryer's evaporation is relative (`E_PILE * boost * w`), so the last few percent of moisture take the longest.
