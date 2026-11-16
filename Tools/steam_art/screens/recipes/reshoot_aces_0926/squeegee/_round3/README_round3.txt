ACES re-shoot 2026-09-26, slot 10 = 18_squeegee_floor   ** ROUND 3 (RETAKE: lane starts mid-rug; see ../README.txt for round 4) **
(steam-izdat "every mechanic on a screenshot": the squeegee plow pushing dirty water off the rug onto the tiles; the HUD
floor strip shows Detergent / Mop.) Round 2 (rejected: "nothing to wipe", pour = opaque brown dome, blade a black sliver)
is in _round2/ (README_round2.txt, run.sh), round 1 in _round1/. _r3probe/ = the round-3 experiments (not part of the take).

Round-3 fixes (AD review of round 2):
- Something to wipe: the rug stands under a translucent BROWN water film (carpet murk ~0.55, pooled 0.60, h > 0.3 mm on
  60 % of the rug) and the wiped lanes show the clean cream / red medallions through it. Murk = dirt / water, so the dirt
  must stay in little water: the round-2 rinses diluted it (C ~0.03). Round 3 keeps the dirt and swaps the water instead:
    stain stage with soap (hose, detergent, boosted scrubs, 40 s soak: the suds collapse in place, their dirt returns)
    -> 7 dryer passes: the pile dries right out and the dissolved grime settles back into the fibres as mud (REDEP)
    -> 8 boosted wet-vac passes over the DRY rug: WETVAC_FLUSH pulls the detergent out (soap 1.35 -> 0.003), mud stays
    -> light re-wet (half pressure): the mud dissolves into ~1 mm of water (mudR 0.04)
    -> one full hose pass inset from the rims right before the lanes: a fresh brown sheet, no soap left to whip into suds.
- Readable blade: lanes run diagonally toward the near-right (heading -33 deg), so the pinned head (heading taken from the
  lane by the real Steps) shows as a diagonal black blade instead of an edge-on vertical sliver.
- Pour reads as liquid: the previous lane's pour had 3 s to spread flat over the tiles (lobed edge, grout lines visible
  through it); the lane in progress stops with only its upper blade end at the rim, so its own pour is just starting
  (stopping at u 0.94 poured enough to stand as a glassy wall). Floor murk is ~2.5x the carpet's for the same water
  (floor s per floor cell), so the puddle is necessarily dark brown; it is flat, not a dome.
- Kept from round 2: this rug, no violet (3 magenta px in the 2560 composite = noise), no runoff halo (hose inset
  u 0.12..0.88 / v 0.88..0.16, the re-wet runoff mopped WITHOUT detergent: a detergent film there turned violet under the
  pours), matching hint, mid-game kit, clock staging.

Hint logic (CarpetSimulation.UpdateHint): "Squeegee the dirty water off" needs crust/grit/mud <= 15 %, stain <= 20 % of the
start, slurry mean > 0.02, pooledCells > 5 %. At the shot: mudR 0.011, stainR 0.050, slurry 0.58, pooled 0.60, foam 2e-4.

STAGING SHORTCUTS (the final frame is a reachable game state; only the number of passes differs):
- r3_b1_boost: brush + foam sprayer POWER 6 / AREA 6 for the stain stage; r3_b2_boost: wet vacuum POWER 6 / AREA 6 for the
  soap flush; r3_f0_boost: floor detergent + mop AREA 6. r3_b9_restore / r3_f9_restore put the PRE2 kit back before the shot.
- r3_u_shot sets the job clock to 5:10 (par 8:04 for this level-1 carpet): the staging spends minutes of sim time on dryer,
  flush and mop passes.

Order (one eval each, all keep EditorApplication.isPaused = true; `bash run.sh` does all of it, ~6 min, then compose/build/verify):
  a_take pre2                         pay-slip guard + mid-game kit ($1,960, PRE2 levels, squeegee POWER 2 / AREA 2 = 317 mm blade)
  r3_s1_wash                          rug_anatolian_star, ResetWithProfile(17, 1, crust 0, mud 1, dust 1, spill 0), LiveTuning(true), hose 11x36
  r3_s2_foam r3_b1_boost r3_s3_scrub r3_s3_scrub r3_s2_foam r3_s3_scrub r3_s4_soak x4     stainR 0.07, foam 0.67 -> collapsing
  r3_s5_dry x7                        w 0.003: slurry -> mud (mudR 0.90)
  r3_b2_boost r3_s6_flush x5 r3_b9_restore
  r3_f0_boost r3_f1_det r3_f2_mop r3_f3_mop r3_f9_restore r3_f8_settle      floor spotless (the stain-stage runoff)
  r3_b2_boost r3_s6_flush x3 r3_b9_restore                                   soap 0.0034
  r3_t0_prewet r3_f0_boost r3_f6_mop r3_f9_restore r3_f8_settle              w 0.18, s 0.63, mudR 0.04, floor clean
  q_murk r3_t1_hose r3_t2_lanes r3_t3_wait r3_t4_lane q_murk               hose, lane B (0.432,0.801 -> 1.092,0.452, 81 fr),
                                                                             3 s, lane C (0.349,0.612 -> 0.925,0.3076, 71 fr) + PinToolHead
  r3_u_shot r3_v_after                clock 5:10, RefreshStatsSync, UiPaused + ScriptedPointer (-20,-20), 15 real Steps, Shot, restore
Then: python Tools/steam_art/compose_screens.py 18_squeegee_floor; final_order.json (both copies) n 10 = 18_squeegee_floor;
build_screenshots(); verify_assets.py. The chain is deterministic (two full runs gave identical probes).
Read-only helpers: q_murk (per-cell CarpetWater murk / film coverage + hint ratios + floor stats), q_short, q_floor.

Result (take 20:4x): HUD Clean 47 % (fill 0.475), Dry 56 % (fill 0.556), 5:10 / par 8:04, $1,960, Squeegee selected,
hint "Squeegee the dirty water off". Economy save: Grant/SetLevel/SetOwned only (pre2 + the restored boosts); no hand-over.
