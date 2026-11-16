ACES re-shoot 2026-09-26, slot 01 = 01_hero_reveal (rug_anatolian_star). Art-director ROUND 1: crusty seed 242 at level 0.9
(seed route; no level 1, no ResetWithProfile), no extra loose chunks on the dirty rim.
Base = reshoot_0926/reshoot_a (eco_hero, pick, w_h3, w_dvpass*, w_band1/2/3shot) + scout_dirt.md 5.1. New / changed files:
- eco_hero, pick: copies (pick also sets isPaused). Pre-kit levels: scraper blade 144 mm, dry-vac r 55, hose r 40, brush r 49,
  wet-vac bar 192 mm, mop 320 mm (w_h3 prints ToolTuning.Effective).
- w_h3: Reset(242, 0.9f) + LiveTuning(true); NO whole-carpet dry-vac passes (w_dvpass/w_dvpass2 dropped).
- w_scrape1..4: 4 slow DRY scraper passes (150 stamps per lane, v 1.01 <-> 0.28) on lanes u 0.31..0.69 (2 and 4 offset):
  band crust 0.80 -> 0.014, crust edge at u ~0.25 / ~0.75, near end v 0.28. ~2850 chunks flung (most land in the band).
- w_dv1: dry vac of the band only (u 0.29..0.71, v 1.02 <-> 0.30, x2): grit 0.30 -> 0.08, chunks in the band sucked.
- w_band1: hose 7 lanes u 0.32..0.68 at PRESSURE 0.6 (1.0 pooled 36 % of the carpet and left a wide dark wet halo on the
  crust) + 2x5 brush lanes u 0.33..0.67.
- w_stain1..4: foam + brush + 4 s soaks: seed 242 leaves stain in ~18 % of the band (near end under the wand, top-left
  corner, a line at u 0.52, the (0.71, 0.48) spill disc, the side strips). Interior stain 0.063 -> 0.005.
- w_band2: wet vac u 0.30 / 0.70 (new edge lanes) + 0.365 / 0.635 / 0.50, x4 (the edge strips stayed soaked without them).
- w_band2b: 2 horizontal wet-vac lanes at v 0.26 (foam blobs pushed over the near end onto the crust).
- w_band3: mop the far tile v 1.10 / 1.06 / 1.20 (chunks flung off the far rim land up to ~0.2 v out) + 5 final wet-vac lanes.
- w_shot: PinToolHead(6, 0.50, 0.28, true), RefreshStatsSync, 33 steps, Shot("01_hero_reveal"); stays paused.
Result (sim, before the shot): band interior (u 0.29-0.71, v > 0.285) crust 0.003 / mud 0.0002 / stain 0.005; side strips
stain <= 0.16; rim crust 0.82 / 0.76 (untouched), margins beside the band 0.75 / 0.73 (was 0.83 / 0.79); 35 loose chunks left
on the carpet, 0 on the tile; floor dirtyCells 0.002.
HUD (hidden by the store crop): Clean 33 %, Dry 89 %, 3:19 / par 10:00, $1,960, Wet vacuum, "Scrape the dry crust off".
Store crop (build_store_art SHOT_VARIANT (670, 250, 2560, 1440), unchanged) still frames it: band centred, far rim with a tile
margin, wand ~y 1100 of 1440.
Run: bash run.sh (Play running; ends with compose_screens.py 01_hero_reveal).
