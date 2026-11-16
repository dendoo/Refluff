ACES re-shoot 2026-09-26, slot 01 = 01_hero_reveal (rug_anatolian_star), seed 242 at level 0.9 (seed route).
ART-DIRECTOR ROUND 2 (this folder; round 1 = the straight runner-like band, kept in _round1/). Round-1 verdict: the band read
as "a runner rug lying on a pebble mat" (ruler-straight parallel sides to the far rim with the rug's own border + fringe at
its end, rounded near corners, pile wall, pattern stopping dead), nothing showed work in progress, and the damp band was
darker than the crust (navy crushed). Kept: seed, level, wash chain, crop, wand pin (6, 0.50, 0.28).

What changed (every r2_* file carries the same outline table, source _lanes.inc):
- Outline: 5 core scraper lanes of DIFFERENT lengths and slight opposite slants (far ends v 0.90-0.97, never through the far
  rim, so no rug border / fringe at the band's end; near ends stepped v 0.36 / 0.33 / 0.28 / 0.32 / 0.38 with the wand's
  lane C leading), 4 offset lanes, 2 short diagonal "stray strokes" out into the crust (the upper star's white arms run on
  under the crust on both sides = same carpet), and 2 LIGHT margin lanes (pressure 0.5, passes 1 + 3 only) so both sides are
  a ramp of broken plates and crumbs instead of a clean-cut wall.
- r2_scrape1..4 (dry, 7.3 mm/stamp), r2_dv x2 (dry vac of the outline only), r2_hose (core + strays at 0.6), r2_brush,
  r2_stainA/B x2 (foam + brush + soak), r2_wv x4 (wet vac, every stroke PULLED toward the camera: pushed strokes laid the
  pile away from the camera and its wet sheen read as a milky veil), r2_dry x3 (NEW: dryer, everything except the near part
  of lane C: pile w 0.24 -> 0.01, upright, the print at full colour; red of the band +40 %), r2_floor (mop the far tile:
  the far-end scraper strokes fling a few chunks onto it), r2_track (3 last wet-vac pulls down C: the undried damp track
  stays pressed toward the camera = a darker striped nozzle-wide track running into the wand head = the process cue),
  r2_shot (pin, RefreshStatsSync, 33 steps, Shot; stays paused).
- Evals kept under ~850 frames: 3 dryer rounds / 4 wet-vac rounds / 2 stain rounds in ONE call timed out (5 s limit: the
  final stats readback waits for the queued GPU work). Whole take ~60 s. Deterministic (same numbers on two full runs).
Result (sim): outline core crust 0.005 / mud 0.02 / stain 0.007, w 0.01 on the dried part; track w 0.24, |b| 0.60 (set);
99 loose chunks, all on the carpet (0 on the tile); floor residue 4.5e-5, dirtyCells ~0; Clean 27 %, Dry 96 %.
HUD (hidden by the store crop): Wet vacuum, "Scrape the dry crust off".
Store crop (build_store_art SHOT_VARIANT (670, 250, 2560, 1440)) unchanged: the whole patch with crust on every side, a tile
margin above the far rim, the wand at y ~1100 of 1440.
Run: bash run.sh (Play running; ends with compose_screens.py 01_hero_reveal).
Alternative without the damp track (all dry): in r2_dry drop the `if (u > 0.33f && u < 0.67f)` line; r2_track then only lays
faint stripes (the pile is dry).
