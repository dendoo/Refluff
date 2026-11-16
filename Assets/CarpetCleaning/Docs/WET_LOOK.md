# Wet look: water murk and wet-pile darkening (2026-09-19)

Two rendering rules requested by the user: (1) water on the carpet is dirty in proportion to how dirty the carpet is and
clear over a clean carpet; (2) the pile is visibly darker where it is wet and bright where it is dry. Both are pure
shader changes; the simulation is untouched.

## 0. Colour-space note (matters for every number below)
The project renders in **Linear** colour space. Every `Color` material property (`_CarpetColor`, `_MudColor`, `_SiltColor`,
`_AbsorbClean`, `_AbsorbDirty`, ...) and the light / ambient colours are authored in sRGB and converted to linear before
they reach the shader: `_SiltColor (0.26, 0.18, 0.10)` is `(0.055, 0.027, 0.010)` in the shader, `_AbsorbDirty (0.15, 0.30, 0.60)`
is `(0.020, 0.073, 0.319)` per mm. Float properties (`_MurkDensity`, `_MurkCRef`, `_MurkCarpet`, `_WetDarken`) are not converted.
When estimating shader output in python, convert first (the first pass of this work did not, and over-estimated the silt brightness 3x).

## 1. Why the water used to look blue
`CarpetWater` (and `FloorWater`) used Beer-Lambert with `_AbsorbDirty = (0.9, 0.5, 0.25)` (linear (0.79, 0.21, 0.05) per mm):
dirty water absorbed RED 15x more than blue, so the more slurry it carried the bluer it got, on top of the blue carpet
showing through. The suspended-dirt term was an unlit `_MudColor * 0.6`, i.e. a dark hole rather than brown murk. And murk
depended only on the dissolved slurry `C`, while crust hydrates at `K_HYD = 0.01/s`, so water standing on the initial dirty
carpet stayed "clean" for a long time.

## 2. Water murk model (`CarpetWater.shader`, `FloorWater.shader`)
Per fragment, with `h` free water (mm), `w` pile moisture (fraction of `W_CAP` = 6 mm), `s` slurry (cell total, dirt units),
`C = s / waterTot`, `waterTot = h + w W_CAP`, and `di = _DirtTex` = (crust, mud, stain, grit):

```
pileDirt = (mud*1.0 + crust*0.5 + stain*0.5 + grit*0.5) * _MurkCarpet     // carpet water only
murk  = saturate((C + pileDirt / waterTot) / _MurkCRef)                    // "potential" slurry concentration, 0..1
path  = 2 h + 0.4 murk                                                     // in + out; the 0.4 (silt on the fibres) applies once the surface is drawn (section 5)
Ta    = exp(-(_AbsorbClean + _AbsorbDirty * murk) * path)                  // per channel: dirty water absorbs BLUE most -> brown transmission
Ts    = exp(-_MurkDensity * murk * path)                                   // scalar silt extinction
siltLit = _SiltColor * (mainLight.color * 0.75 + SH(up))                   // lit albedo of the suspended dirt
body  = scene * Ta * Ts + siltLit * (1 - Ts)
water = body (1 - fres) + fres sky + spec + rim + meniscus * 0.2 * body    // meniscus used to add a flat unlit +0.12 grey
```
Why one concentration scale for slurry and pile dirt: mud dissolving into slurry (`K_DIS`, 1:1) leaves `murk` unchanged,
hose inflow dilutes it, wet-vac extraction removes water and slurry in proportion (murk stays until the water is gone,
then the surface fades out). The first version screen-combined an amount-based pile term with a concentration-based slurry
term and the water got visibly *cleaner* while brushing released dirt into it (review finding, fixed).
`scene` is the refracted opaque texture (the wet fibres). Fresnel sky, specular, rim and the premultiplied composite
are unchanged. (2026-09-19, later: the foam term left the water shaders; foam is its own surface, see `FOAM_LOOK.md`,
and the water discards where F > 1.2.) `FloorWater` uses the same model with `murk = saturate(C / _MurkCRef)`, `C = s / (h + 1e-3)` (no pile term).

Material knobs (set by `CarpetSceneBuilder`, stored in `CarpetWater.mat` / `FloorWater.mat`; colours are sRGB):
| property | default | meaning |
|---|---|---|
| `_AbsorbClean` | (0.02, 0.03, 0.05) | clean water absorption per mm (unchanged; ~0 after linear conversion) |
| `_AbsorbDirty` | (0.15, 0.30, 0.60) | extra absorption per mm at full murk (was (0.9, 0.5, 0.25) = blue cast) |
| `_SiltColor` | (0.26, 0.18, 0.10) | albedo of the suspended dirt: brown, about as light as the wet crust around a puddle |
| `_MurkDensity` | 0.8 | silt extinction per mm at full murk: Ts = 0.45 / 0.15 / 0.02 for a 0.3 / 1 / 3 mm fully dirty film |
| `_MurkCRef` | 0.4 | dirt units per mm of water that count as fully dirty |
| `_MurkCarpet` | 1.0 | weight of the pile-dirt term (carpet water only; 0 = slurry only) |

Expected display values (sRGB, after linear lighting; light 3.0, flat ambient (0.55, 0.58, 0.66), camera 35 deg off normal):
| case | film | murk | composite (display) |
|---|---|---|---|
| clean carpet, clean water | 0.3 .. 3 mm | 0 | (0.13, 0.28, 0.55) = the wet blue fibres, no cast (dry fibres (0.29, 0.51, 0.85)) |
| initial carpet (crust 0.9, mud 0.8) | 0.3 mm | 1.00 | (0.38, 0.29, 0.19) brown, fibres 45 % visible |
| initial carpet | 1 mm | 1.00 | (0.40, 0.28, 0.17) brown, fibres 15 % visible |
| scraped, unwashed (mud 0.7) | 1 mm | 1.00 | same brown |
| half clean (mud 0.25) | 0.3 / 1 mm | 0.69 / 0.39 | (0.26, 0.28, 0.44) / (0.30, 0.28, 0.37) greyish, slightly murky |
| mud dissolved (clean pile, s = 0.7) | 1 mm | 1.00 | (0.38, 0.28, 0.22) brown: the dirt moved into the water and still shows |
| light slurry (s = 0.1) over clean pile | 1 mm | 0.16 | (0.23, 0.28, 0.47) faint haze |

Since 2026-09-25 (section 5) films under ~0.3 mm are drawn by `CarpetFibers` only (a 0.15-0.3 mm film shows in sparse, faint drape pools;
nothing under 0.15 mm), so the 0.3 mm rows describe the onset of the fade-in (a 0.3 mm film is ~40 % covered at mean alpha ~0.07), not a
drawn brown film.

## 3. Wet-pile darkening (`CarpetFibers.shader`)
```
wetV = saturate(1 - exp(-3 w) + saturate(h / 0.3))         // pile moisture (soft curve) or a free-water film both read as wet
base = lerp(base, MudColor*0.7, saturate(C/0.5) * wetV * 0.5)              // dirty water tints the fibres
base = lerp(base, pow(base, 1.25), wetV) * lerp(1, _WetDarken, wetV)      // darker AND more saturated
spec / aniso keyed on wetV (was w); rim keyed on wetV * (1 - crustCover) (the bluish back light must not hit the caked plate);
the hard "if (h > 0.2) col *= 0.85" step is gone.
```
Why the curve: infiltration is `K_ABS = 0.25 mm/s`, so after a hose pass `w` is only 0.1 .. 0.2; the old `saturate(w * 1.2)`
left the damp pile at 88 .. 94 % brightness. `1 - exp(-3w)` gives w 0.05 -> 0.14, 0.10 -> 0.26, 0.20 -> 0.45, 0.40 -> 0.70,
0.70 -> 0.88, so the dryer / wet vac / evaporation show progress over the whole moisture range (a linear `w * 2.5` plateaued at 0.4).
Display luminance of the blue carpet: dry 0.22, w 0.1 -> 0.17, w 0.2 -> 0.14, w 0.4 -> 0.11, under a film -> 0.07
(display (0.29, 0.51, 0.85) -> (0.13, 0.28, 0.55)); print 0.97 -> 0.40; crust plate 0.37 -> 0.12; mud 0.05 -> 0.01.
`_WetDarken` (0.5) still sets the floor; the dry look is unchanged.

## 4. Verification and review
- All three shaders import with 0 messages (`ShaderUtil.GetShaderMessages`, D3D12), `CarpetSceneBuilder` dry-run compiles,
  no console errors. No Play-mode check this session at the user's request; the tables above come from evaluating the
  fragment maths in python in linear space.
- Adversarial review (workflow: 3 lenses HLSL/URP, numeric visual, sim consistency; 2 refuters per finding; 27 agents):
  4 confirmed and fixed (amount-vs-concentration murk inversion; pile term saturating over the whole initial carpet;
  silt in-scatter too light against the wet pile; flat grey meniscus), 8 refuted (e.g. floor runoff murk units are consistent,
  clean-water warm cast is invisible after linear conversion, deep dirty pools being opaque is correct). Two refuted-but-cheap
  points were still applied as insurance: rim light off the crust plate, soft wetness curve for the dryer.
- Open: the user judges the look; knobs are `_MurkDensity`, `_SiltColor`, `_MurkCRef`, `_MurkCarpet`, `_WetDarken`.

## 5. Film vs free surface: no more glass lanes (2026-09-25)
**Problem.** After squeegee / wet-vac lanes the damp pressed pile rendered as translucent dark-grey glass panes with hard
straight edges and a thin white (far edge) or bluish (near edge) line, most visible on the far half at the game camera
(store shot `05_squeegee_plow`, also `12_rya`). With `CarpetWater` hidden the lanes looked right: damp, darker, pressed pile.

**Root cause** (`CarpetWater.shader`, the fibres were fine):
- Every film above 0.0015 mm was drawn as a free water surface (`alpha = 1 - exp(-h/0.15)`: 0.3 mm = 86 %). A film held in the
  pile is already drawn by `CarpetFibers` (wetV has `saturate(h/0.3)`), so it was counted twice. Films under 0.5 mm were also
  forced flat (a perfect planar mirror), and their Fresnel sky and fake rim term added a constant grey-blue
  (~(0.011, 0.010, 0.011) linear at v 0.67) over the dark wet pile: sRGB (35,17,14) -> (47,33,33), saturation 0.59 -> 0.31.
- The normal came from `pileTop + h`. A pressed lane's pile top is ~8 mm lower (fSet 0.85: 9.85 vs ~18 mm), and that step
  across one sim texel read as a ~50-degree water facet. The far edge faces the sun half-vector -> white line; the near edge gets
  Fresnel ~0.7 + rim -> bluish line.
- The lanes really do hold water: a pressed lane's K_Flux head is `BED_PILE (pileTop - PILE_H)` = 2.5 mm lower, so the hose-wet
  neighbour seeps in (SQUEEGEE.md 9). Probe of the squeegee recipe below, top lane (u 0.3-0.8, v 0.57-0.76 in 0.01 strips):
  h 0.33-0.37 mm on the near side, ~0 in the middle, 0.43 -> 0.99 mm toward the far wall; the film-free middle lane has h 0.016.
  So a threshold alone cannot hide it: 0.6-1 mm of real standing water also has to stop looking like a pane.

**Fix** (render only; h, w, the fibre set, seepage, pooledCells, Wetness, HUD and pay slip are untouched):
1. **Film handover.** `freeW = smoothstep(_FilmLo, _FilmHi, h + drape*tuft) * smoothstep(0.5 _FilmLo, _FilmLo, h)`,
   `alpha = (1 - exp(-h/0.15)) * freeW`, early `discard` below 0.01 (saves the 12 normal taps for held films). `_FilmLo` 0.3 mm =
   K_Reduce's pooledCells threshold (what the HUD calls standing water), `_FilmHi` 0.9 mm (a fresh hose sheet is full water). Below
   0.15 mm the fibres alone show the water: wet darkening, the slurry tint by C and the per-strand wet spec; a 0.15-0.3 mm film shows
   only in sparse, faint drape pools (the second factor, added 2026-09-26: without it the tuft jitter drew films down to ~0.1 mm in
   patches, with full murk). The meniscus is a contact band, `4 freeW (1 - freeW)` (where the coverage fades in), plus the glow of a
   sheet that has just closed over the tuft tips, `smoothstep(Hi+0.2, Hi+0.6, h) (1 - smoothstep(Hi, Hi+1, h))` (peak ~0.5 at 1.45 mm).
   The 2026-09-25 version, `freeW (1 - smoothstep(Hi, Hi+1, h))`, brightened the whole body of every sheet under 1.9 mm by 20 %.
   Held films keep none of the water layer, murk included: an attempt that kept the Beer-Lambert murk for held films (it5)
   drew brown panes at the lane edges (slurry 0.1-0.15 there), which double-counts the fibres' C tint.
2. **Head normals.** The normal is the gradient of `bed + BED_PILE (pileTop - PILE_H) + h`, exactly K_Flux's `BedHeadAt + h`,
   via the new global `_SimConsts2 = (BED_PILE, 0, 0, 0)` (`CarpetSimulation.SetGlobals`). The vertex height stays pileTop + h
   (puddles still sit on the pile top). This head is not level at a lane wall, though: the pressed lane keeps drawing seep water in
   because the heads differ, so at the far wall it still steps `BED_PILE x dPileTop - dh` = 0.3 x 7.6 - (0.99 - 0.77) ~ 2.2 mm (up to
   3 mm next to a fresh 1.3 mm sheet) over the 2-texel central difference (6.25 mm): a slope of 0.35-0.48, a 19-26 degree facet that
   faces the camera, and a shallow wall still holds ~1 mm, so `saturate(h/0.5)` does not flatten it. So (2026-09-26) the slope is limited
   while the sheet is shallower than the tufts: `|g| <= lerp(0.08, 4, smoothstep(_FilmHi, 2.5, h))` (~5 degrees up to 0.9 mm, free from
   2.5 mm, a real puddle's rim). Flowing water still has a head gradient, so ripples and the suction look remain.
3. **Tuft drape.** Water below ~2.5 mm drapes over the tuft tips: a 2-octave value noise `tuft` at `_DrapeMm` 6 mm scale,
   with `_Drape` 0.5 mm of relief fading out between `_FilmHi` and 2.5 mm, (a) jitters the coverage threshold, so a 0.5-1 mm
   sheet shows as water pooled between tufts and its edge is organic rather than an h-isoline; (b) tilts the normal
   (`_DrapeTilt` 1.5 x the relief slope: mean 6 degrees, p95 12, max ~23 with both octaves; 4.3 / 9.1 / 15 with the first alone),
   which breaks the reflection up into a mottled sheen. Since 2026-09-26 the relief is band-limited by the pixel footprint
   (`fw` = drape cells per pixel): octave 1 x `saturate(2 - 4 fw)`, octave 2 x `saturate(2 - 4 x 2.03 fw)`, so an octave whose cell
   is under ~2 px fades out instead of aliasing (at the 1080p game camera the far half has fw ~0.35: octave 2 is off, the tilt
   x0.6). And the refraction offset uses the normal from before the drape: the tilt breaks up the sheen, it no longer displaces the
   pile under the water (that scrambled the medallions into a resolution-dependent grain that crawled when the carpet moved).
4. **Shallow specular occlusion.** Under 2.5 mm the tufts catch most reflected rays: sky Fresnel and rim are scaled by
   `envOcc = lerp(shallowSky, 1, smoothstep(_FilmHi, 2.5, h)) x freeW^2`, with `shallowSky = _ShallowSky (0.35) x
   lerp(0.15, 1, smoothstep(_FilmHi, _FilmHi + 0.6, h))` (2026-09-26: seep water just over `_FilmHi` sits lower still between the
   tufts, and a partly covered pool keeps freeW^2 of the sheen). Key-light glints stay. Without this, the drape tilt turned the
   Fresnel and rim terms into white "frost" speckle on the tall wet pile, and the 0.6-1 mm seep water along the top lane's far wall
   (freeW ~1) kept a grey-blue veil.

Material values (`CarpetSceneBuilder.BuildWaterMaterial`, `CarpetWater.mat`): `_FilmLo` 0.3, `_FilmHi` 0.9, `_Drape` 0.5,
`_DrapeMm` 6, `_DrapeTilt` 1.5, `_ShallowSky` 0.35. Not changed: CarpetFibers, FloorWater (a film on tile really is a mirror),
foam, the sim.

**Verified** (Play, HDR `StoreShotApi` 1920x1080 game camera plus a free view (0.1, 0.45, -0.2) / (48, 0, 0) / fov 35; sRGB means):
- Squeegee recipe: `Reset(14, 0.35f)` on rug_salor_turkmen; `Zigzag(2, 0.15,0.25, 0.85,0.75, 7, 40)`; `StrokeNow(5, 0.12,0.66,
  1.06,0.66, 70)`; the same at v 0.52; `StrokeNow(5, 0.12,0.38, 0.62,0.38, 40)`; `PinToolHead(5, 0.62, 0.38, true)`; `RunFrames(3)`;
  one real frame. Pane box (800-1000, 340-420): (50,35,34), saturation 0.31 -> (42,20,18), 0.57. Water on minus water off
  (+13,+17,+20) -> (+2,+3,+3). Mean |on - off| over the lanes 11.6 -> 3.8. Far-edge luminance profile at x 900: peak/median
  2.33 -> 1.30 (the bare pile alone gives 1.46). The white line right of the blade is gone, and the water the blade pushes
  still reads as dirty water. Unplowed hose sheet (h 0.77): (83,53,43) -> (80,45,32), still clearly water over (59,27,20) dry.
- Wet-vac lanes (4 strokes at v 0.68/0.58/0.48/0.38 + 3 s): lane 0.68 (67,45,45), saturation 0.33 -> (59,30,28), 0.53; no-water
  render (58,28,26); lanes 0.58/0.48 = the no-water render; all darker than the dried lane (101,51,37).
- Dried control (dryer 16 x 40 stamps + 10 s: h 0, w 0.005, bSet 0): identical with and without the water layer.
- Hose recipe (`hose3.cs`, puddle 5.3 mm): still water at +0 / +2 / +5 s (h 5.3 / 2.2 / 1.45 mm); the rim is now soft and
  irregular. At +10 s (0.66 mm) it reads as dark soaked pile with a few pools instead of a purple-grey haze.
- Fully soiled carpet (`Reset(1)` + hose zigzag + 7 s, h 1.0): the centre stays brown murky water, while the thin rim reads as
  darker wet crust instead of a brown veil ((100,71,50) -> (85,61,41)). Accepted: the murk returns with the surface from 0.3 mm.
- Moisture lamp (Z): wet-vac lanes glow; the dried lane stays dark. FoamScene(3): no visible change (mean 0.02 levels).
- Probes are identical before and after; no NaN in StatsJson, 0 shader messages, no console errors.
- Captures: `scratchpad/fx/glass/{before,after}/`, sheet `compare.png`.
- Open: the store shots (05, 12) need re-capturing; Lo/Hi and the drape are the knobs if the soak-in after a hose pass
  (the sheet fades between ~5 and 10 s) should last longer.

**Review fixes (2026-09-26)** (the four changes marked 2026-09-26 above: slope limit, drape band limit + refraction without the drape,
veil / meniscus, film gate). Same recipes and cameras, HDR `StoreShotApi` 1920x1080; "round 1" = the 2026-09-25 fix:
- Top-lane far wall (squeegee recipe, rows 320-342, water on minus water off, luma): baseline +23.9, round 1 +13.2 (x700-1000) /
  +15.3 (x800-1000), now +5.5 / +7.3 (x1000-1250: +3.3). Saturation 0.25 -> 0.43 -> 0.58 (no water 0.63). What is left is sparse
  key-light glints (median lift ~1-4) and one patch of genuinely murky seep water. Pane box (41,19,16), sat 0.61; lanes |on - off|
  3.8 -> 2.4; far-edge profile at x 900 peak/median 1.30 -> 1.24 (bare pile 1.46).
- Near-half lanes (hose `Zigzag(2, 0.15,0.04, 0.85,0.42, 7, 40)`, squeegee at v 0.30 / 0.20 / 0.12, one real frame; profile across each
  lane's far wall at u 0.7-0.9, 7 px strips): no bright line before or after, peak/median 1.47 -> 1.47 against the bare pile's 1.83
  (walls 0.27 / 0.19 hold no free water). The lane-0.30 wall's lift is +9.2 -> +6.5: its 0.4-0.8 mm seep water is dirty (murk), real.
- Drape grain (clean carpet `Reset(14, 0)` + the same hose zigzag + 5 s, h 0.7-1.5 mm; high-passed luma of the render vs the render without
  water, three uv boxes): correlation 0.37 / 0.38 / 0.69 -> 0.99 / 1.00 / 1.00 at 1920x1080 and 0.33 / 0.35 / 0.67 -> 0.99 / 1.00 / 1.00
  at 2560x1440; on - off std 15.6 -> 4.7 (far box; 3.5 with the drape off entirely). The medallions stay whole under the sheet.
- Hose puddle (lift over the no-water render, luma): +0 s 14.7 and +2 s 13.8 unchanged; +5 s (1.45 mm) 10.9 -> 9.9; +10 s (0.66 mm)
  6.2 -> 2.4 (soaked pile with a few pools).
- Wet-vac lanes: identical to round 1 within 2 levels; dried control identical with and without water; dirty carpet (`Reset(1)`, h 1.0)
  still brown murky water, ~5 % darker (centre (111,71,40) -> (105,67,36)) since the whole-sheet meniscus glow is gone; moisture lamp
  after wet vac / dryer: the same glow (mean difference 2.0 / 1.4 levels); FoamScene(3) identical (mean 0.00).
- 0 shader messages, no console errors; the sim is untouched (probes identical).
- Captures: scratchpad `fx/glass/after_review/` (plus `pre_review/` for round 1 in the new near-half / clean-sheet scenes, scripts in
  `fx/glass/rv2/`).
