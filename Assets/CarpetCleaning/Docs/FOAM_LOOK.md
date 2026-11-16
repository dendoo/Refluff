# Foam look: bright pink-violet, 3D (2026-09-19)

Request: the foam looked like TV static (a flat grey-white speckle inside the water shader). It must be bright
pink-violet and visibly volumetric. Pure rendering change; the simulation (F, Fd, Fage in `_SoapTex` / `_FSoapTex`)
is untouched, no `SimSettings` / `FloorSettings` value changed.

## 0. Why it was static
The old foam lived in `CarpetWater.shader` / `FloorWater.shader`: 2D Worley at 350 cells per 1200 mm (3.4 mm cells,
about 1.5 px on a 1280 capture), normals from `ddx/ddy` of that noise (per-pixel derivative noise), the water grid lifted
by only `0.9 F` mm (max 10.8 mm) with no silhouette, no height gradient, no shadow, and a grey-white albedo.

## 1. What it is now
- One displaced height-field surface per domain: `CarpetRoot/FoamSurface` (`ShellMeshBuilder.BuildGrid` 384x480, one
  vertex per sim cell) and `FloorRoot/FloorFoam` (384x384 over the 3.6 m floor domain, at local y -0.003), both created
  by `CarpetRenderer` / `FloorRenderer` at Awake and pre-created by `CarpetSceneBuilder` (`FoamSurface`, `FloorFoam`).
- One shader `CarpetCleaning/CarpetFoam` (`Shaders/CarpetFoam.shader`), two materials `Materials/CarpetFoam.mat` and
  `Materials/FloorFoam.mat` (keyword `_FLOOR_DOMAIN` on). Queue Transparent+1 (3001), `Blend One OneMinusSrcAlpha`
  (premultiplied), `ZWrite On`, `ZTest LEqual`, `Cull Off`, no A2C, no DepthOnly / DepthNormals pass.
- Draw order: fibres (2450, depth) -> opaque texture copy (the foam is NOT in it, so the water never refracts pink) ->
  water (3000, no depth) -> foam (3001, writes depth) -> droplets / dust (3005 / 3010: hidden behind the mound, drawn
  in front of it) -> cursor (3100, ZTest Always). The water shaders simply get overdrawn under the mound (no discard, so
  hiding the foam with `CarpetDebugApi.SetFoamVisible(false)` never leaves holes).

## 2. Height field (`FieldAt`, `FoamMmAt`, `SurfAt`; mm, carpet frame: carpet backing 0, floor top -3)
```
mask   = smoothstep(0.05, 0.6, F)                                   // steep rounded foot, 4..15 mm wide
lump   = _LumpMm * ((2 nR - 1) + 0.5 (2 nA - 1)) * sat(F / 1.5)     // _NoiseTex R at mm/200, A at mm/60: +-8 mm dollops
foamMm = mask * (_FoamHeight (1 - exp(-F / _FoamKnee)) + lump) + 2.5 smoothstep(0, 0.05, mask)   // 45, 3: F 1 -> 13, 4 -> 33, 12 -> 44 mm; 2.5 mm plinth keeps fibre tips under the foot
foamMm *= 0.6 inside the wet-vacuum blade footprint (tool 6, _ToolCursor)                         // suction dip
base   = pileTop + h (carpet)  |  min(h, 6) + 0.3 - 3 (floor)
mesh y = base + foamMm + 0.25 + _CapLiftMm * mask (+ 3 on the floor object)
```
Cross-domain field (`CARPET_EDGE.md` s.2, 2026-09-19): `FieldAt` returns the surface height as the union (smooth max,
6 mm) of the carpet mound (own textures inside; outside, the rim value running down over 45 mm with the base lifted to
the carpet top where its film shows) and the tile heap (own textures outside, sampled >= half a floor cell from the
rim; inside, the rim cell's heap faded over 24 mm, seated on the floor base so it overhangs the rim only where it is
taller than the carpet surface). `F` for the shading = `max(F_carpet * fall, F_floor * fallIn)`. So the gradient, the
shadow march and the bubble field are continuous at the edge, a mound cut by the edge runs down onto the tile, foam
heaped against the carpet's side overhangs the rim, and there is never a valley along the edge (the floor variant
discards inside the rect; the carpet grid ends at it, its rim vertices copy the coarser floor grid's height so the two
meshes share one rim edge).
Mesh bounds: `BuildGrid` gives y -0.10..0.20 m (floor foam 0.4 m tall) - keep `_FoamHeight + _LumpMm + _CapLiftMm < 90 mm`.

## 3. Fragment
1. Footprint first (`mmPerPx` = mm per pixel from the view distance and the projection, NOT from screen derivatives:
   those are per-triangle and produced a comb along quad diagonals on steep ramps; plus the noise mip levels),
   then the early-outs: `foamNear` (max F within 8 mm) and `shadowNear` (max F at 12 / 26 / 44 / 70 mm toward the
   sun) are vertex varyings; empty pixels cost one compare.
2. Macro normal `Ng` from the height field gradient in mm (4 `SurfAt` taps at +-1 sim cell, UNCLAMPED: bubbles are placed
   by extrapolating this slope, a clamp smeared every sphere into a pill on the spill ramp); the
   lump noise inside those taps is sampled at an explicit mip from the footprint (no aliasing grain at distance).
3. Bubble caps: two layers of packed spheres (cells `_BubbleMm` 16 and 7 mm; the 7 mm layer fades in when it covers
   >= 5 px, i.e. at 1440p, and while fading in it never wins over a full-size dome). Per pixel, a 3x3 cell search per
   layer: centre jittered +-0.3 cell, radius `cell * (0.45..0.6) * (1 + 0.25 sat(Fage / 25))` (coarsening with age;
   the grid never rescales, so bubbles never swim), growth `smoothstep(th, th + 0.25, F at the bubble centre)` with
   `th = 0.15 + 0.35 hash` (bubbles appear and pop one by one), centre 0.35 r below the film. Every sphere is seated
   on the REAL field height under its centre (`FieldAt(c)` + `FoamMmAt`), never on a local tangent plane: every pixel
   must place a given sphere at the same height, otherwise spheres smear into slivers along curved ramps (the spill
   over the carpet edge, ridge fronts). For the same reason the gradient used for the film plane is unclamped.
   Exact ray-sphere against the eye ray from the RASTERIZED point (`P0.y = posWS.y - surf(mm)`, not the analytic
   `capLift`: between vertices the flat triangle deviates from the curved field by several mm on the spill ramp, and
   a per-triangle ray-origin offset smeared the spheres into diagonal streaks): the larger root is the sphere point
   nearest the eye; the visible surface is the nearest of {best dome, the tilted film plane}. Coverage
   `sat(disc / (2 r mmPerPx))` is a 1-px analytic edge (no A2C); `mmPerPx` comes from the view distance and the
   projection (screen derivatives are per-triangle and produced a comb along quad diagonals on steep ramps).
   `hk` (hit height on the cap) darkens the base of every bubble, `seam` (gap to the second-best cap) darkens junctions.
4. Micro froth: mipmapped `_NoiseTex.g` (Worley), two 1-texel taps at an explicit mip, fades out below ~3 mm/px.
5. Shadow: 8 taps toward the sun at 4..74 mm with a lowered effective sun (`tanElev * 0.65`, there are no shadow maps
   and the crescent must read); horizon test `occ = max film * sat((slope to the tap - tanElev) / 0.35)` where `film`
   is the foam film at the tap (only foam occludes; the 20 mm carpet block beside bare tile is not a mound) (a wide soft
   transition, so a ramp rising at about the sun slope gives a smooth partial shadow instead of per-tap banding);
   self-shadow multiplies the wrap term by `1 - 0.7 occ`; skirt pixels (F < 0.03) output
   `(0, 0, 0, _ShadowStrength * occ)` = the cast shadow on carpet, water and tile (light from +x/-z, so the crescent
   lies on the far-left foot).
6. Silhouette exit test: 4 taps behind the pixel (away from the eye, 6 / 12 / 24 / 40 mm), min over taps: only where
   the eye ray is clear of the foam at EVERY tap (a tap without a film counts as clear) has the ray left the mound, so
   the film is not drawn there and only bubble caps survive - the outline is a row of caps, not the smooth mesh edge.
   (The first version took the max of 2 taps and punched pinholes into every back-slope steeper than ~0.4.)
7. Alpha: `alphaM = (1 - exp(-F / _AlphaKnee)) * smoothstep(0.03, 0.12, F)`, `alpha = sat(alphaM * lerp(alphaM, 1.6, cov))`
   times the exit factor: domes stay solid while the film between them thins, the mound edge dissolves into single
   bubbles, nothing shows below F ~0.05 (no wide smudge from dying foam). Output premultiplied; the cast shadow is
   composited under thin film.
8. `Cull Off` with NO back-face branch: on the grazing spill ramp the two triangles of a floor quad face opposite ways
   (a flat back-face colour showed as diagonal streaks along the seam); the shading depends only on the height field.

## 4. Colour model (hue preserving; shadows rotate to violet, never grey)
```
pinkness = max(smoothstep(0.05, 0.6, foamMm / _FoamHeight), 0.7 (1 - mask))   // pink from mid height up and on thin films
alb   = lerp(_FoamShadowColor, _FoamColor, pinkness);  alb = lerp(alb, _FoamDirtColor, 0.85 dirtF), dirtF = sat(Fd / (0.04 F)) mask
wrap  = sat((N.L + _WrapK) / (1 + _WrapK)) * shadow;   form = lerp(1 - _FormK, 1, sat(Ng.L))
col   = (alb Lc _SunScale wrap form + lerp(_FoamShadowColor, alb, wrap) SH(N) _AmbientScale) * crease * gap * lerp(0.55, 1, sat(foamMm / 12))
spec  = pow(N.H, 16) 0.35 Lc lerp(alb, 1, 0.5) cov sat(rPx / 3)  +  pow(N.H, 120) Lc cov [rPx >= _SparklePx]   // pinkish sheen + blooming sparkles
rim   = (1 - N.V)^3 (_FoamRimColor 0.35 + sat(N.rimDir) _RimColor)
```
Why it stays pink under the 3.0 sun: the albedo green is 0.13 linear, so G never reaches the tonemap knee while R and
B are only compressed (Neutral: linear 1.0 / 1.5 -> display 0.63 / 0.76); `_SunScale 0.5` keeps the lit crest near
1.3 linear, under the bloom threshold (1.2 sRGB = 1.52 linear), so only the sparkles bloom. Note: the +12 saturation
in `CarpetSceneBuilder.SetupVolume` is NOT active (the `ColorAdjustments` override is never persisted into
`SampleSceneProfile.asset`); numbers below are without it.
Since 2026-09-26 the scene uses the ACES tonemapper (user decision); the tonemap figures here were measured under Neutral.

Material knobs (sRGB colours, set by `CarpetSceneBuilder.ApplyFoamLook`, stored in both .mat files):
| property | default | meaning |
|---|---|---|
| `_FoamColor` | (0.98, 0.40, 0.88) | crest / lit albedo, hot pink (linear (0.955, 0.133, 0.752)) |
| `_FoamShadowColor` | (0.45, 0.16, 0.72) | shadow fill and mound foot, violet |
| `_FoamDirtColor` | (0.55, 0.30, 0.45) | dirty foam (Fd), mauve, still magenta-hued |
| `_FoamRimColor` | (1.0, 0.75, 0.95) | fresnel rim, light pink |
| `_FoamHeight` / `_FoamKnee` | 45 mm / 3 | mound height curve `45 (1 - exp(-F/3))` |
| `_LumpMm` | 8 | dollop amplitude (mm) |
| `_BubbleMm` | (16, 7) | bubble cell sizes (mm); radius 0.45..0.6 cell |
| `_CapLiftMm` | 5 | mesh sits this much above the film so caps break the outline (0 = smooth heightfield) |
| `_SunScale` / `_AmbientScale` | 0.5 / 1.0 | direct / ambient weights |
| `_WrapK` / `_FormK` | 0.35 / 0.5 | wrap lighting width / macro form shading |
| `_ShadowStrength` | 0.55 | cast-shadow darkening on the skirt |
| `_AlphaKnee` | 0.25 | film alpha `1 - exp(-F / knee)` |
| `_SparklePx` | 6 | min projected bubble radius (px) for the pow-120 sparkle (off in 720p captures, on at 1440p) |

## 5. Verified (2026-09-19, live Editor, scripted `CarpetDebugApi` frames, captures `Assets/Screenshots/foam_*.png`)
- `CarpetFoam`, `CarpetWater`, `FloorWater` import with 0 messages; C# dry run + recompile clean; Build Scene twice ->
  exactly one `FoamSurface` and one `FloorFoam`, materials on `CarpetCleaning/CarpetFoam`, `_FLOOR_DOMAIN` only on
  `FloorFoam.mat`, queue 3001; no console errors in any test.
- Spray 2 s at the centre (`FoamScene(1)`, mean F 0.146, max 10.8): a pink-violet mound ~150 mm wide with a bumpy
  outline of caps, individual bubble caps with pinkish highlights, violet creases and foot; 1440p 8-bit probes crest
  (186, 86, 163), top (211, 113, 192), foot (145, 56, 146), left flank (132, 62, 142); 0 grey samples in 11.9k pink
  pixels; two captures without stepping differ in 0 pixels.
- Brush smear (`FoamScene(2)`, F 0.149, mean pink (192, 94, 175)); wet vac 2 s: foam mean -> 0.086 (-42 %), a clean
  blade-shaped hole with the suction dip; squeegee piles a ridge; dryer ring; floor foam via edge spray + outward
  brush (`FoamScene(4)`): floor foam 0.0115, the carpet mound spills over the edge onto the tile (continuous, no seam),
  nothing under the carpet.
- Empty carpet: 0 pink pixels; hose puddle captures differ from the pre-change baseline by 4 pixels (> 24/255) of
  230k; whole-carpet foam (`FoamScene(5)`, rows of brushed foam, mean pink (142, 70, 134) = the violet end): render
  probe 12.9..14.6 ms/frame at 2560x1440 vs 9.1..11.7 before, empty carpet 8.3..11.6 vs 11.5 (probe noise +-1.5 ms;
  the worst case costs about +2..3 ms: 18 exact field taps per foam pixel for the bubble seats, the 8-tap shadow
  march and the exit test; the single-mound gameplay case is far cheaper).
- Ageing: foam mean 0.146 -> 0.070 (10 s) -> 0.021 (20 s), bubbles coarsen and pop one by one, no swimming.

## 6. Known gaps / next
- Debris chunks resting on foamed cells are seated by `GroundAt = pile + h` (no foam) and get buried inside a mound.
- The cast shadow is a 5-tap height-field march (no shadow maps): crescents are approximate.
- The bubble field is anchored in carpet mm: pushed foam flows through a static bubble pattern (bubbles pop/grow, they
  do not slide). A sim-side displacement field could advect it later.
- Stage 2 option (only if more "3D" is wanted): GPU-instanced sphere-impostor bubbles on top of the mound (DebrisSystem
  pattern); `SV_Depth` is per pixel under MSAA 4, so intersections alias - use a low-poly icosphere if so.
- Optional free win: the SSAO renderer feature + default-profile override run a DepthNormals prepass every frame that
  no project shader uses (~0.3-0.5 ms); the builder never persists `ColorAdjustments` (saturation +12 inactive).
