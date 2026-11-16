ACES re-shoot 2026-09-26, slot 10 = 18_squeegee_floor   ** ROUND 2 (rejected: see ../README.txt for round 3) **
(steam-izdat "every mechanic on a screenshot": squeegee plow pouring onto the tiles; the HUD floor strip shows Detergent / Mop.)
Round 1 (European green, level 0.85, near-black) was rejected by the art director: its scripts + README are in _round1/.
_experiments/ = the round-2 probes (dust-only carpet, re-flood, sheen timing, picture A/B), _probe/ = round 1's first probe.

Round-2 choices (AD: lighter rug, readable blade, compact glossy spill, no violet haze, no runoff halo):
- Rug: rug_anatolian_star. The staging never depends on the picture (z_pic_ab A/B of the final state): Salor, European green
  and the rya all read dark brown / near-black when the pile is wet under ACES; the Anatolian's cream medallions and cream
  border keep the rug legible and the black blade reads on the border. (Its 3rd use in the set, after 01 and 07.)
- Dirt: ResetWithProfile(17, 0.7, crust 0, mud 1, dust 0, spill 0). The soap stage takes the grime and the stains out of the pile
  (mudR 0.0004, stainR 0.013 at the shot), so the level only browns the water (slurry 0.22), not the rug.
- Suds: 3 rinses (pile saturated, the flush collapses most suds) + slow dry-vac rows (the dry vacuum takes foam, not water)
  until carpet foam 1.3e-4: no violet anywhere (0 magenta pixels in the 2560 composite).
- Floor: the rinse runoff is cleaned with the floor tools (detergent film + two offset mop ring sets = spotless, no hatched
  film), again as a touch-up after the suds passes, then 12 s for the mop's damp streaks to fade: the only water on the tiles
  is the lane's pour.
- One lane, shot the moment it ends: the squeegee's wiped lane and the glassy roll are transient (the sheet and the roll are
  gone ~2 s after the blade stops, and the pour spreads into a flat disc), so the shot holds the sim (UiPaused) on the frame the
  stroke ends. Earlier finished lanes do not read on this rug at all and their pours needed a mop pass: dropped.

Hint logic (CarpetSimulation.UpdateHint): "Squeegee the dirty water off" needs crust/15 %, grit, mud <= 15 % of the start,
stain <= 20 % of the start (Worley stain blotches exist wherever the soil S > 0.3; they only dissolve with soap), slurry mean
> 0.02 and pooledCells (h > 0.3 mm) > 5 %. At the shot: slurry 0.22, pooled 0.21.

STAGING SHORTCUTS (the final frame is a reachable game state; only the number of passes differs):
- r2_s10_boost / r2_s12_restore: dry vacuum at POWER 6 / AREA 6 for the suds passes (~2.3x fewer passes, so the saturated pile
  still holds standing water), back to the PRE2 kit (POWER 4 / AREA 3) before anything is shot.
- r2_f0_boost / r2_f9_restore: floor detergent + mop at AREA 6 for the floor clean-up (fewer rings), back to AREA 1 / 1.
- r2_u_shot sets the job clock to 4:12 (Economy._clockAnchor by reflection): the scripted staging spends ~6 min of sim time
  on suds passes and mopping, which put the clock over par (red, 5:51 / par 5:49) at a moment a player reaches well inside it.

Order (one eval each, all keep EditorApplication.isPaused = true; `bash run.sh` does all of it, ~90 s, then compose/build/verify):
  a_take pre2                         pay-slip guard + mid-game kit ($1,960, PRE2 levels, squeegee POWER 2 / AREA 2 = 317 mm blade)
  r2_s1_wash r2_s2_brush              pick rug_anatolian_star, ResetWithProfile(17,0.7,0,1,0,0), LiveTuning(true), hose 11x36, brush 9x45
  r2_s3_foam r2_s4_scrub r2_s5_soak  x2, r2_s3_foam r2_s4_scrub r2_s4_scrub, r2_s3_foam r2_s4_scrub r2_s5_soak   (stainR 0.22)
  r2_s6_rinse x3                      pooled 1.0, wet 1.0, foam 2.4 -> 0.53
  r2_f0_boost r2_f1_det r2_f2_mop r2_f3_mop r2_f9_restore      floor clean #1 (runoff of the rinses)
  r2_s10_boost (r2_s7_suds r2_s8_suds) x4 r2_s7_suds r2_s12_restore   foam -> 3e-4, pooled ~0.23
  r2_f0_boost r2_f5_det r2_f6_mop r2_f9_restore r2_s16_settle  touch-up of the rim leak + 12 s for the damp streaks
  q_short r2_t1_lane q_short         the lane: StrokeNow(5, -0.03,0.30 -> 0.93,0.30, 80 stamps), PinToolHead(5, 0.93, 0.30, working)
  r2_u_shot r2_v_after               clock 4:12, RefreshStatsSync, UiPaused + ScriptedPointer (-20,-20), 15 real Steps, Shot, restore
Then: python Tools/steam_art/compose_screens.py 18_squeegee_floor; final_order.json (both copies) n 10 = 18_squeegee_floor;
build_screenshots(); verify_assets.py.
Read-only helpers: q_short (hint ratios + floor), q_ratios, q_floor, q_maps / q_fmaps (sim / floor maps to SteamStore/_work/test/),
z_look_r2 (1280x720 camera render).

Result (take 19:5x): HUD Clean 72 % (fill 0.716), Dry 20 % (fill 0.202), 4:12 / par 5:49, $1,960, Squeegee selected,
hint "Squeegee the dirty water off". Sim before the shot: mudR 0.0004, stainR 0.0125, slurry 0.221, pooled 0.212, foam 1.3e-4;
floor dirty cells 0.0027 (the pour only). Economy save: Grant/SetLevel/SetOwned only (pre2 + the restored boosts); no hand-over.
