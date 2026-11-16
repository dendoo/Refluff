ACES re-shoot 2026-09-26, NEW slot 10 = 18_squeegee_floor -> SteamStore/screenshots/10_squeegee_floor.jpg
(steam-izdat "every mechanic on a screenshot": squeegee plow + water on the floor; the HUD floor strip shows Detergent / Mop).
Rug: rug_european_green (a Salor take of the same staging read as one dark maroon mass under ACES + wet pile: rejected).
Dirt: ResetWithProfile(17, 0.85, crust 0, mud 1, dust 0, spill 0) = washed-down mid-job carpet, heavy grime so the water stays brown.
The picture does not enter the sim: s1_wash.cs (Salor) gives the same numbers.

Hint logic (CarpetSimulation.UpdateHint): "Squeegee the dirty water off" needs crust/15 %, grit, mud <= 15 % of the start,
stain <= 20 % of the start (the Worley stain blotches exist on every soiled carpet and only dissolve with soap), slurry mean
> 0.02 and pooledCells (h > 0.3 mm) > 5 %. Hence the soap stage, then foam removal that keeps the brown water, then a rinse.

Order (one eval each, all keep EditorApplication.isPaused = true; bash run.sh does all of it):
  a_take     only needed if a pay slip / carpet-away state is up (PayoutResume + TakeNewCarpetNow + 70 Steps)
  pre2       mid-game kit (copy of reshoot_0926/reshoot_d/pre2.cs): $1,960, PRE2 levels (squeegee POWER 2 / AREA 2 = 317 mm blade), both lamps owned
  s1_wash_eu NewCarpetNow until rug_european, ResetWithProfile(17,0.85f,0,1,0,0), LiveTuning(true), hose zigzag 9x40
  s2_brush   brush zigzag 9x45 (mud -> slurry; mudR 0.06)
  3x (s3_foam s4_scrub s5_soak)        detergent zigzag 9x50, brush 9x50, 300 frames
  s4_scrub s5_soak s5_soak   s4_scrub s5_soak s5_soak   s3_foam s5_soak s5_soak      -> stainR 0.188, foam 0.74 (pink stripes)
  s9_wv      quick wet-vac zigzag 10x36 incl. rims: foam 0.006, slurry 0.54 -> 0.29, soap 1.2 -> 0.52 (floor still clean)
  s10_suds   slow dry-vac rows between the wet-vac rows (the dry vacuum takes foam, not water): foam 0.0018
  s7_rinse   hose zigzag 9x40 inset u .08-.92 / v .07-.93: pooled 0.96, but the soap regrows a violet haze (foam 0.09)
  s11_suds   slow dry-vac 10x60 over the haze: foam 0.059, pooled 0.69, hint "Squeegee the dirty water off"
  t1_lanes   squeegee lanes v 0.105 and 0.29, u -0.03 -> 1.05 (100 stamps, ~760 mm/s), 40 frames after each: brown puddle on the right tile
  t2_lane    lane in progress v 0.47, u -0.03 -> 0.93 (90 stamps), PinToolHead(5, 0.93, 0.47, working) -> roll pouring over the right rim
  u_shot     PinToolHead again, RefreshStatsSync, UiPaused=true + ScriptedPointer (uv -20,-20: no cursor ring), 15 real Steps,
             Shot("18_squeegee_floor"), HUD readout; all restored in finally
  v_after    unpin, pointer back to the mouse, UiPaused=false, Editor stays paused
Then: python Tools/steam_art/compose_screens.py 18_squeegee_floor; final_order.json (both copies) has {"n":10,"name":"18_squeegee_floor",...};
build_screenshots(); verify_assets.py.
Read-only helpers: q_ratios (hint ratios vs _initStats by reflection), q_floor, z_look / z_look4k (debug renders to SteamStore/_work/test/).
_probe/ = the first exploration (level 0.6, Salor) + s6/s8 dry-vac defoam of the rejected Salor take: not part of the take.

Result (take 18:55): HUD Clean 75 % (fill 0.750), Dry 56 % (fill 0.559), 2:43 / par 6:29, $1,960, Squeegee selected,
hint "Squeegee the dirty water off". Sim before the shot: mudR 0.0009, stainR 0.111, slurry 0.21, pooled 0.225, foam 0.021.
Economy save: Grant/SetLevel/SetOwned only (pre2); no hand-over.
