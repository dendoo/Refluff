clip2_water (2026-09-26, round 2): Steam "About This Game" clip 2 = "Real water, real mud", above the Read More fold.
1560x650 (2.4:1), 25 fps, 6.12 s (153 frames), 2.08 MB (crf 18), muted loop. Re-run: bash run.sh (~1 min; re-run gave the
same sheet to the eye; 2.07 -> 2.08 MB, spray droplets are random).
Final: SteamStore/description/refluff_clip2_water.mp4 (+ _poster.png = first frame, _sheet.png).
Take frames: SteamStore/_work/clips/c2_water_r2/ (165 PNGs 1920x800 of a 3840x2160 render, seq.json); the encoded take is
c2_water_r2_loop/ (the rotated order, hard links + 12 blended frames). Round 1 (r1 take deleted): _rejected/README_r1.txt.

Round-2 review fixes: (1) far less water: a closed muddy patch (~0.52 x 0.34 m in the fast-scripted test) with dry rug above/below/left/right (r1: a
0.76 m lake past both frame edges); (2) no standing-water disc at the hook/poster/loop: the glassy discs only show for ~1 s
mid-clip while the bursts land; (3) no 3.3 s dead hold: the end hold is 1.6 s and the foam mound visibly slumps/spreads
in it (~1.3x wider); (4) hose -> foam gap ~0.7 s (the mud soaks in during it); (5) loop = natural (continuous frames),
the reset dissolve is state only (no tool in either end, no ghost nozzle); (6) poster = the pink mound in the ring on the
dark mud; (7) hose back to the player's starting POWER.

Staging (fast scripted calls):
- c2_eco: pre-kit tool levels (clip 1's eco_hero), $1,960, both lamps owned, shop closed, marketing HUD, head unpinned.
- r2/stage: SetLevel(2, 0, 0) = hose POWER level 0 (P 0.756; c2_eco had level 1, P 0.870); rug_finnish_rya;
  ResetWithProfile(68, 0.36f, crust 0, mud 0, dust 1, spill 0); LiveTuning(true); SetTool(2). DirtJson: seed 68, level 0.36,
  dust 1, mud 0, crust 0, spill 0; StatsJson grit 0.255, stain 0.018, water 0 (dry). Foam sprayer untouched (POWER 1, AREA 1:
  radius 76.7 mm).
- Water budget (r2/_trials/exp_*.cs, fast scripted, maps via SaveDebugView + scratchpad c2r2_ext.py): the free water spreads
  to ~1 mm, so the patch size is set by the volume, not the path. Hold 0.3 s lmb -> 0.45 m round patch; 0.5 s rmb sweep ->
  0.39 m round; 3 x 0.2 s rmb bursts 108 mm apart -> 0.52 x 0.34 m (wide, fits 2.4:1; each burst's glass disc is small and
  thin, soaks into mud in ~1 s). The crop spans v 0.148-0.555 (610 mm): r2/uvpx.cs.

Take (r2/take_final.cs = r2/take4.tpl via r2/mk4.sh; real EditorApplication.Step frames, every 2 Steps = 25 fps):
- ResetWithProfile again (fresh dry carpet), uncaptured warm-up (SeqBegin "c2_warm" every=100000): Tool(2), Hover(0.28,-0.03).
- SeqBegin("c2_water_r2", 3840, 2160, 960, 890, 1920, 800) (same crop as r1, centred on u 0.5 / v 0.332).
- T 0-0.50 Away (dry rug). Nozzle glides in from below-left (0.28,-0.03) -> (0.38,0.27) 0.40 s -> (0.41,0.35) 0.25 s, hover
  0.12 s. Right-button (half pressure) bursts of 0.20 s at (0.41,0.35), (0.50,0.338), (0.59,0.35) with lifted 0.25 s glides.
  Up 0.35 s. Out below-right (0.70,0.13) 0.20 s -> (0.78,-0.04) 0.15 s. Away 0.10 s, Tool(3).
- Ring in from below (0.56,-0.05) -> (0.52,0.25) 0.25 s -> (0.50,0.35) 0.15 s; Hold 0.90 s (mound = ring size); Hover 0.15 s;
  out below-right (0.68,0.14) 0.22 s -> (0.76,-0.04) 0.18 s; Away 1.60 s (the mound slumps, the tide line darkens). 6.60 s.
- r2/run.cs until done (2 calls), r2/end.cs.
Encode: r2/rotate_take.py c2_water_r2 c2_water_r2_loop 94 --fade 0.5 -> frames 94..153, a 12-frame linear-light dissolve
(154..165 into 1..12, make_clips' weights), 13..93; then make_clips --crf 18 (no --fade-loop). The clip starts at take
T 3.76 s (the mound heaping inside the ring); the reset dissolve is at clip 2.40-2.88 s; the dry rug shows ~0.8 s before the
first burst; the clip's last frame (take 93) leads straight into its first (94).
Diagnostics: r2/uvpx.cs (render px of a u/v grid), r2/go.sh <take script> (take + run + end), r2/_trials/ (water-budget
experiments exp*.tpl, half-res test takes t1-t4: t1 single sweep + foam circle = mound hid the mud, t2 two dabs = peanut
mound hid the mud, t3 two bursts, t4 = the final timeline at 1920x1080).
