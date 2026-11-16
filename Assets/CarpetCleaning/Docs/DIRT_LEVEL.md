# Dirt of a new carpet: level, mix and soil mask (2026-09-24; balance update 2026-10-19)

Original user requests (RU), 2026-09-24:
1. Every new carpet gets a random dirt level and a random noise dirt mask, from absolutely clean to absolutely dirty.
2. After looking at v1: Reset (R) must create a new carpet with a new mask. They also wanted to see the in-between
   states, which v1 did not show:
   - just dusty, with the rug's real print showing through;
   - so dirty the print is gone, but with no chunks of dried crust;
   - and so on.

   v1 thresholded a single mask and switched on stains, mud and crust within a few centimetres of each other, so a
   carpet was either clean or caked.

v2 (this document) rolls a whole **dirt profile** per carpet and spends a broad soil field on four materials: dust,
grime, crust and spills. The layers of the look were tuned offline with `Tools/dirt_preview.py` (section 7).

**Balance update, 2026-10-19:** every new gameplay carpet must have dust on at least 50 % of its cells and dried-mud
crust on at least 20 %. These are separate coverage shares: dust can also lie on grime or crust. Natural rolls keep
their seed variation, with minimum mix values and a bounded initialization adjustment (section 1.1). Explicit
forced-level/debug/store profiles retain their previous behavior. The user will check the change in Unity;
no Unity checks were performed for this balance update.

Historical checks from 2026-09-24, without Play (these do not verify the 2026-10-19 balance change):
- `CarpetSim.compute` imports with 0 messages and all 15 kernels are supported on D3D12;
- `CarpetFibers.shader` compiles with 0 messages;
- the C# dry run and a real recompile are clean;
- the C# rolls equal the preview's.

## 1. One seed decides the carpet

`CarpetSimulation.RollDirt(seed)` returns a `DirtProfile`. Each field is its own full-avalanche hash of the seed
(lowbias32 plus a salt), so seeds n and n + 1 are unrelated:

| field | roll | meaning |
| --- | --- | --- |
| `level` | base roll uniform 0..1 (`DirtLevelForSeed`); gameplay initialization may raise it | how soiled overall: 0 spotless .. 1 fully soiled |
| `spread` | uniform 0.45..1.3 | how unevenly. Wide means one carpet runs from clean to filthy; narrow means clean and soiled patches. |
| `dust` | uniform 0.3..1, clamped to at least 0.5 for a natural roll | loose dust on lightly soiled ground; this mix value is not the actual coverage |
| `mud` | 0.2 + 0.8 sqrt(u) (skewed high) | ground-in grime. 1 hides the print where the soil is full. |
| `crust` | original roll: 4 carpets in 10 get 0.25..0.9 x smoothstep(0.25, 0.75, level), else 0; natural rolls clamp it to at least 0.2 | controls dried-mud cake patches; gameplay initialization may raise it to satisfy actual coverage |
| `spill` | level x 0.15..1 | share of the 12 spill stains that happened |
| `offset` | 0..211 noise cells | where the carpet reads the noise, so consecutive seeds never get shifted copies of the same blobs |

`DirtProfile.FullySoiled` (level 1, dust 0, mud 1, crust 1, spill 1) is the carpet every carpet was before
2026-09-24. `RollDirt(seed, forcedLevel)` with `forcedLevel >= 1` returns it; `0 <= forcedLevel < 1` keeps the original
seed mix at that level. Only `forcedLevel < 0` applies the new minimum dust/crust mix values.

**Who picks the seed:**
- **First carpet of a play session:** `CarpetSimulation.randomFirstSeed` (default on) rolls `Random.Range(1, 1000)`.
  Seeds stay small because K_Init adds the seed to float noise coordinates.
- **Reset button and R:** `CarpetHandover.ResetToNewCarpet()` gives the next picture and colour and `seed + seedStep`,
  and does a **full** reset (`notify: true`): the floor and the debris start over, and `SnapToIdle` runs from any state.
  R is still read by `ToolController`, which hands it to the handover component on the same object. The HUD button
  calls the same method. Restarting the same carpet is gone from the game; `CarpetDebugApi.Reset(seed)` still does it.
- **Hand-over "Take new carpet":** `seed + seedStep` too, but carpet-only (`notify: false`, the floor is kept,
  `HANDOVER.md` 4).

Sim API: `ResetCarpet(seed, notify, dirtLevel)`, where a negative level takes the natural roll and enforces gameplay
coverage (section 1.1). The explicit `ResetCarpet(seed, notify, DirtProfile)` overload takes any profile without that
coverage adjustment (store staging, section 8). The seed still places
the crust plates, the stain blotches and the spill discs. Properties: `CarpetDirt` (the profile) and `DirtLevel`. Uniforms, set in `DispatchInit`: `_DirtMask = (level, offset.xy, spread)` and
`_DirtMix = (dust, mud, crust, spill)`.

### 1.1 Minimum gameplay coverage (2026-10-19)

The mix minima alone cannot ensure coverage: soil intensity can still be too low, and the crust mask can occupy
less area than its parameter suggests. A natural gameplay reset initializes normally, reads the actual fields,
and measures visible dust (`grit > 0.04`) and crust (`crust > 0.1`). `K_Reduce` stores their cell counts in spare
stats slots 12 and 13, using the same x256 fixed point as the other totals. C# exposes `dustCells` and `crustCells`
as carpet fractions. The stats buffer remains 256 uints and the inspection-lamp histogram offsets stay unchanged.

If either measured fraction is below 0.5 / 0.2, at most eight retries interpolate the **original** rolled profile's
`level` and `crust` toward 1 with `t = i / 8`, `i = 1..8`. Each retry reinitializes and reads the stats again; it stops
as soon as both minima pass. Dust, mud, spill, spread and the noise offset stay fixed during these retries. The
accepted adjusted profile becomes `CarpetDirt`, so `DirtLevel` reports the level that was actually initialized.

The final retry has `level = crust = 1`. With the shipped `DIRT_DUST = 0.35` and `dust >= 0.5`, every cell then has
`grit >= 0.07` (above the dust dead zone) and crust at least 0.75, ensuring both coverage minima. This fallback
assumes the shipped dust tuning; reducing `DIRT_DUST` far enough can prevent visible dust even at full soil.
Only negative-level gameplay resets run this adjustment. Forced levels, explicit profiles and `FullySoiled`
retain their historical results, including deliberately spotless or dust-only staging.

## 2. Soil intensity S (`SoilMask`, `CarpetSim.compute`)

```
n = SoilNoise(p, DIRT_MASK_MM, offset)     # domain-warped fbm, smoothstep(0.25, 0.75) -> ~uniform 0..1
S = smoothstep(a, a + spread, n),  a = 1 - level (1 + spread)          # level 0: S = 0; level 1: S = 1 (early out)
S = sat(S (1 + DIRT_GRAIN (fbm(p / DIRT_GRAIN_MM) * 2 - 1)))           # mottling: soil is never an even film
```

With spread about 1 and level 0.5, S covers nearly 0..1 across one carpet. That broad ramp, instead of v1's narrow
0.3, is what puts every state on screen.

## 3. What S turns into (`K_Init`)

```
grit  = dust * DIRT_DUST * smoothstep(0, 0.35, S) * lerp(1, 0.4, smoothstep(0.55, 1, S));  grit *= smoothstep(0.04, 0.05, grit)
mud   = (0.5 + 0.5 fbm) * mud * S^2 (0.8 + 0.2 S);                                          mud  *= smoothstep(0.02, 0.03, mud)
crust = (0.75 + 0.25 fbm) * smoothstep(0.45, 0.9, CrustPatch + 0.15 (plateShade - 0.5)) * smoothstep(0.35, 0.75, S)
stain = 0.8 Worley blotches * smoothstep(0.3, 0.8, S)  +  0.6 per spill disc present (hash < spill), wherever it landed
f0    = min(0.55, 0.6 (1 - exp(-10 crust)))           # pile matted only under crust
```

| soil S | what you see (typical mix) |
| --- | --- |
| < ~0.05 | the clean rug |
| 0.05 - 0.35 | **dust**: a grey film on the tips, the print reads through (stronger on dark colours) |
| 0.35 - 0.6 | dust + a dull brown grime, print dimmed |
| 0.6 - 0.85 | grime dominates, print faint |
| > 0.85 | **print gone**. Without crust this is "very dirty but no dried chunks". |
| crust patch and S > ~0.5 | dried-mud plates with cracks. They end along the plate cracks; every new gameplay carpet now has some. |
| spills | a whole disc of tea / wine / oil / grass colour, even on clean ground |

At level 1 with `FullySoiled`, S = 1 makes every factor exactly 1 and grit 0, so the result is the old carpet. Only the
spill loop changed from `[unroll]` to `[loop]` since the original, which can move the last bit of a float.
`CarpetDebugApi.Reset(seed)` therefore still reproduces every recorded number.

### 3.1 Dried-mud slabs (2026-09-26)

The crust used to be one ~16 mm Voronoi cell per plate, all one tone and height, each 2.5 mm strand cell shaded as a little
dome: at trailer size it read as a pebble / cork mat (`TRAILER.md` 8.3 item 7, `STORE_CLIPS.md` 9.6). Now:
- `K_Init`: a domain-warped ~40 mm Voronoi gives the slabs (main cracks ~1.5-3 mm, width wandering with an fbm); 60 % of
  the slabs are crazed by thin partial sub-cracks along a warped ~16 mm Voronoi. `_Crust.a` = the crack field in 8 mm
  units (the shader's cut `cw = 0.26 + 0.5 D` = 2.1 mm undamaged), so scraper damage opens the crazing first and a
  scraped slab breaks into its small pieces. `_Crust.g` = the slab's hash (0.82 big + 0.18 small cell).
- `CarpetFibers`: slab thickness `crustTop = 0.5 cover (0.72 + 0.56 plateShade)` (neighbours stand at different heights);
  a flat slab normal tilted per slab and bevelled into the cracks along the crack field's gradient (no strand domes);
  tone 0.76-1.24 x with a slight yellow / grey shift per slab, a lighter curled rim just inside a crack, sandy grain.
- `Tools/dirt_preview.py` follows `K_Init` (the plate tone range too).
- Numbers: the crust / mud / stain / grit fields are unchanged (only `_Crust.a` / `.g` changed), so DirtScore, the Clean
  bar stay comparable. The scraper's damage rate reads the crack field (`0.3 + 0.7 (1 - e)`); the slab field sits farther
  from a crack on average, so `K_Stamp` uses `e * 0.84`: A/B, two passes, crust removed old vs new: seed 1 0.0359 / 0.0379
  (+5.5 %), `Reset(394, 0.9f)` 0.0327 / 0.0333 (+1.9 %). Scraper numbers in older SESSION_LOG entries are within that.
Checked 2026-09-26 on `Reset(394, 0.9f)` (Anatolian, 4K probe, 2x crop): irregular cracked slabs instead of pebbles.

## 4. Dust is the grit channel

Dust uses the existing loose-soil channel (`_Dirt.w`), so the tools already treat it right:
- the dry vacuum lifts it fast (`6/s x pressure x dryness`), leaving clean stripes through a dusty rug;
- water turns it into mud (`K_G2M`), so hosing a dusty carpet first makes it muddier (real carpet-cleaning order);
- it counts in the Clean bar (`DirtScore` weight 0.9);
- in the water murk it counts at weight 0.5.

The hint now asks for the vacuum on `grit > 0.04` or when more than 20 % of the starting dust is left: "Vacuum up the
dust and crumbs".

`CarpetFibers.shader`:
- The **dust film** is `_DustVeil (0.45) x (1 - exp(-3.5 grit)) x smoothstep(0.1, 0.8, looseSoilHeight)`, applied on top of
  everything and varied per strand by `0.55 + 0.9 hs.y`, so it reads as grainy dust, not fog. Its colour is
  `_DustColor` (0.60, 0.56, 0.50).
- **Crumb specks** start at grit 0.2 now, `saturate((grit - 0.2) * 2.5)`. Light dust shows no specks; scraped crust
  (grit 0.3+) still does.
- Since 2026-10-21, `looseSoilHeight = t <= 0 ? 1 : t` for both the film and crumb height masks: shell-0 backing
  shares the local dusty/clean state of the tips instead of showing a clean picture under dirty pile. Actual
  fibre shells retain their original height profiles; both backing contributions vanish at zero grit.
- Both new properties are also written by `CarpetSceneBuilder`.

## 5. Spotless staging and the Clean bar

`Cleanliness = 1 - dirt / initial dirt`, so dirt nobody can see must not exist. It would hold the bar below 97 %:
- **Dead zones:** grit below ~0.04 (a film under ~6 %) and grime below ~0.02 are cut to 0 in K_Init.
- **Trace rule** (`ResetCarpet`): a carpet whose first stats read gives `DirtScore < 2e-4`, less than the smallest
  spill stain (~4e-4), is re-inited at level 0 with no spills. A failed readback never triggers it.
- A spotless carpet is won at once. Its hand-over button appears right after landing, and the hint says
  "Sparkling clean!".
- This remains useful for explicit forced-level/profile staging. Since 2026-10-19, new gameplay carpets satisfy
  the minimum dust/crust coverage instead of ending spotless. Seeds 5, 17 and 18 were spotless under the old rolls.

## 6. Tunables

| where | field | default | effect |
| --- | --- | --- | --- |
| `SimSettings` | `DIRT_MASK_MM` | 500 | soil noise cell (2.4 x 3 cells per carpet) |
| | `DIRT_MASK_WARP` | 1.5 | domain warp: swirled vs round regions |
| | `DIRT_GRAIN` / `DIRT_GRAIN_MM` | 0.3 / 50 | mottling strength (relative) and blotch size |
| | `DIRT_CRUST_MM` | 220 | crust patch noise cell |
| | `DIRT_DUST` | 0.35 | grit of the dustiest ground |
| `CarpetFibers.mat` | `_DustVeil`, `_DustColor` | 0.45, (0.60, 0.56, 0.50) | how opaque and what colour the dust film is |
| `RollDirt` (C#) | ranges in section 1 | | how the carpets vary |

The `SimSettings` values take effect on the next new carpet (R). The C# defaults and the `.asset` match.
`DIRT_MASK_SOFT` from v1 is gone: the per-carpet `spread` replaced it.

## 7. Offline preview: `Tools/dirt_preview.py`

`python Tools/dirt_preview.py out.png Assets/CarpetCleaning/Textures/<rug>.jpg [seed ...]` renders a contact sheet.
Seed -1 renders the fully soiled reference.

The script ports `RollDirt`, the noise, `K_Init` and the fibre shader's albedo compositing to numpy, and runs in a
couple of seconds without Unity. It prints each roll and its layer means, the same means `StatsJson` reports. Keep
it in step with those files; its `TUNE` holds the `SimSettings` values. It shows albedo only (no lighting, no
shells), so use it for the spread of states and the colours, not for shading.

Since 2026-10-19, natural previews also perform the same bounded coverage adjustment as gameplay initialization:
measure `grit > 0.04` / `crust > 0.1`, then raise the original profile's level and crust in eight steps if needed.
This is an offline check of the generation math; it does not verify Unity shader compilation or rendering.

## 8. Testing

- `CarpetDebugApi.Reset(seed)`: the fully soiled carpet on the same picture, as every recipe before this date assumes.
- `Reset(seed, -1)`: the seed's natural roll with the gameplay coverage adjustment. `Reset(seed, 0.3f)`: the original
  seed's mix at level 0.3, without minimum-coverage adjustment.
- `NewCarpetNow()`: exactly what R does.
- `ResetWithProfile(seed, level, crust, mud = -1, dust = -1, spill = -1)` (2026-09-26): the seed's own mix and noise
  offset at `level`, with the mix fields you pass overridden. A negative field keeps the seed's roll; the values are
  clamped to 0..1.
  - Unlike `Reset(seed, 1f)`, level 1 keeps the seed's mix instead of `FullySoiled`: the roll is taken at 0.999, then
    the level is set.
  - Like `Reset`, it is a full reset (the floor and the debris start over) and switches `ToolTuning` to the factory
    tools. The trace rule of section 5 still applies.
  - Use it to stage a look the rolls do not give on a given rug. The store clip 2 (a dry, dust-only rya) is
    `ResetWithProfile(68, 0.36f, 0, 0, 1, 0)`: no crust, no grime, full dust, no spills.
  - For a carpet-only reset (floor kept, e.g. the hand-over clip 4), build the profile yourself and call the sim:
    `var d = CarpetSimulation.RollDirt(14, 0.95f); d.crust = 0.30f; sim.ResetCarpet(14, false, d);`.
- `DirtJson()`: the profile. `StatsJson()` and `HandoverJson()` add `seed` and `dirtLevel`.
- **Historical 2026-09-24 rolls, before the minimum-coverage balance update.** This table is retained as the original
  look reference; it does not describe current new gameplay carpets. The last four columns are % of the carpet: clean = S < 0.05;
  dusty only = dust with mud < 0.1 and no crust; heavy = mud > 0.4 (print gone); crust = crust > 0.1.

| seed | level | spread | dust | mud | crust | clean | dusty only | heavy | crust | DirtScore |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 0.49 | 0.96 | 0.72 | 0.87 | 0.21 | 10 | 21 | 33 | 6 | 0.443 |
| 2 | 0.90 | 0.80 | 0.80 | 0.40 | 0 | 0 | 0 | 0 | 0 | 0.389 |
| 4 | 0.46 | 0.86 | 0.60 | 0.63 | 0 | 35 | 20 | 14 | 0 | 0.219 |
| 5 | 0.04 | 1.10 | 0.78 | 0.61 | 0 | 100 | 0 | 0 | 0 | 0 (spotless) |
| 6 | 0.99 | 1.28 | 0.43 | 0.50 | 0.30 | 0 | 0 | 12 | 30 | 0.655 |
| 7 | 0.21 | 0.57 | 0.76 | 0.80 | 0 | 82 | 8 | 0 | 0 | 0.047 |
| 9 | 0.18 | 0.50 | 0.97 | 0.65 | 0 | 75 | 17 | 0 | 0 | 0.058 |
| 10 | 0.50 | 0.78 | 0.92 | 0.83 | 0.42 | 57 | 18 | 10 | 12 | 0.241 |
| 11 | 0.30 | 0.93 | 0.58 | 0.52 | 0 | 53 | 31 | 0 | 0 | 0.089 |
| 12 | 0.45 | 0.90 | 0.99 | 0.55 | 0 | 47 | 33 | 3 | 0 | 0.168 |
| 13 | 0.78 | 1.03 | 0.69 | 0.91 | 0 | 0 | 0 | 85 | 0 | 0.637 |
| 16 | 0.29 | 1.23 | 0.50 | 0.66 | 0 | 74 | 18 | 0 | 0 | 0.026 |
| 19 | 0.77 | 0.97 | 0.55 | 0.93 | 0.36 | 0 | 0 | 78 | 36 | 0.907 |
| 20 | 0.87 | 0.70 | 0.47 | 0.58 | 0 | 0 | 0 | 51 | 0 | 0.463 |

- Historical seeds for each state: 12 (dust clouds over a clean rug), 13 (print gone, no crust), 19 (crust over grime),
  5 (spotless). Use explicit forced-level/profile staging to recreate such states now.
- The base `DirtLevelForSeed` over seeds 1..999 remains uniform: 93 / 110 / 100 / 100 / 92 / 114 / 93 / 98 / 102 / 97
  per decile. Gameplay's accepted levels can be higher after enforcing coverage.

## 9. Open

- Not yet looked at in Play (the user checks it). The things only a real render shows:
  - the grain of the dust film under the 3x sun;
  - whether the grime reads as dirt rather than shade on the darkest rugs;
  - the crust edge at pile scale.
- The old uniform-level distribution no longer describes the final gameplay profiles: coverage enforcement can
  raise their level/crust. Visual balance and any initialization hitch from repeated synchronous stats reads
  remain for the user's Unity check.
- The HUD does not show the dirt profile.
