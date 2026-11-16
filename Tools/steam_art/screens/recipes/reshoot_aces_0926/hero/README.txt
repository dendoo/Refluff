ACES re-shoot 2026-09-26, slot 01 = 01_hero_reveal (rug_anatolian_star). ART-DIRECTOR ROUND 3 (this folder).
Round 1 (straight runner-like band) is in _round1/, round 2 (ragged outline, seed 242, light margin lanes) in _round2/.

Round-2 verdict: the band worked, the dirt did not. Seed 242 at 0.9 (crust 0.87) turned ~95 % of the visible surround into one
even mosaic of same-size greige plates (a pebble / cork mat, no pile anywhere, an even light-sand speckle at the 120 px thumbnail,
lighter than the rug); its 4 small grime patches read as bald holes; the light margin lanes (pressure 0.5, passes 1 + 3) left a
lace net of hollow plate rings on both upper shoulders of the band.

What changed (seed route kept: no level 1, no ResetWithProfile):
- r3_reset: Reset(394, 0.9f) = crust-over-grime (dust 0.65, mud 0.91, crust 0.61, spill 0.89; DirtJson). The cake sits in the
  middle where the band cuts it and in the foreground around the wand; dark, ground-in, print-gone grime pile fills both flanks,
  the far edge and the near-right corner. Picked from Unity probe renders of the AD's candidates 394 / 174 / 101 (+ 242):
  r3probe/probe_a|b -> SteamStore/_work/test/r3probe_*.png. Surround luma in the store crop 71 -> 62, RGB (92,67,45) -> (85,58,37).
- The kind-3 margin lanes are gone from the outline table (_lanes.inc): no half-scraped plates, so no lace rings.
- A 3rd stain round (r3_stainA + r3_stainB x3): 394 lays a spill band across the far third of the cut (stain 0.8 at v 0.76-0.88).
- r3_rim (new, after r3_floor, before r3_track): two short wet-vac pulls over the far rim at u 0.50 / 0.68 suck the 2 scraper
  chunks that came to rest exactly on the rim (v 1.000; at the game camera they stood above the rim line = crumbs on the tile).
  The wet vac takes only water / slurry / foam / chunks, so the dirt there is untouched.
- Everything else as round 2: 4 dry scraper passes (7.3 mm/stamp), dry vac of the outline only x2, hose, brush, wet vac x4
  (pulled toward the camera), dryer x3 (except the wand's track), floor mop, the damp track pulls, pin (6, 0.50, 0.28), Shot.
  No scatter pass on the dirty part: the 72 loose chunks left are by-products of scraping the cut (at its near end around the
  wand, beside both near corners, on the left grime flank from the left stray stroke, a few on the right), all on the carpet.

Result (sim, deterministic: two full runs gave identical numbers): cut core crust 0.004 / mud 0.015 / stain 0.003 / grit 0
(round 2: 0.005 / 0.02 / 0.007); ring around the cut crust 0.34 / mud 0.52 / stain 0.25; 72 chunks, 0 on the tile; floor residue
5e-5, dirtyCells 0.0004. HUD (hidden by the store crop): Clean 28 %, Dry 96 %, Wet vacuum, "Scrape the dry crust off".
Store crop (build_store_art SHOT_VARIANT (670, 250, 2560, 1440)) unchanged: the whole cut with dirt on every side, a tile
margin above the far rim, the wand at y ~1160 of 1440.
Run: bash run.sh (Play running; ~60 s; ends with compose_screens.py 01_hero_reveal). Diagnostics: r3_measure, r3_chunks.
