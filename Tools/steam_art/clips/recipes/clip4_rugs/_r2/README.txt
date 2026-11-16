clip4_rugs (2026-09-26, round 2): Steam "About This Game" clip 4 = "A new rug every job". 1560x878 (16:9: the flight needs the
full game frame, the rug alone fills ~85 % of its height), 25 fps, 6.56 s (164 frames), 2.99 MB (crf 26, preset veryslow;
crf 22 slow was 5.29 MB, crf 26 slow 3.32 MB; description total 14.85 / 15 MB, verify_assets ALL OK), muted loop, scene only
(no HUD, no tool head). Final: SteamStore/description/refluff_clip4_rugs.mp4 (+ _poster.png = first frame = the spotless
Transylvanian medallion at rest, _sheet.png). Takes: SteamStore/_work/clips/c4_rugs_r2a/ + c4_rugs_r2b/ (82 PNGs each, full
3840x2160 render, seq.json). Re-run: bash run.sh (~2.5 min; sparkle positions come from CelebrationFx's xorshift state and
differ from run to run, the rest is deterministic).

Round 1 (in _r1/) was one hand-over with a 0.4 s --fade-loop that dissolved the dirty rug into a different spotless one (read as
a fake alpha-erase), 0.55 s of bare tile and a 2.3 s static hold. Round 2 is a two-job cycle with no dissolve: every
discontinuity is a hard cut from a rug dirty to the SAME rug spotless (same picture, same pose, same seed), i.e. a "job done" edit.

Clip beats (each job 3.28 s; job 2 = the same with the rugs swapped):
  0.00-0.60  Transylvanian medallion spotless at rest (poster)
  0.60       HandoverNow(false): real hand-over (Economy pays, $0 here since V = 0), Spotless + Bone dry gold/white burst, FlyOut
  ~1.36      rug out of the top edge; 4 frames (0.16 s) of bare tile
  ~1.46      TakeNewCarpetNow on the very Step the stage turns Away (Each hook) + dirt forced in the same hook; FlyIn 0.95 s
  ~2.2-3.28  dirty Anatolian star (seed 15 @0.95, its own mix: dust 0.88, mud 0.39, crust 0.27, spill 0.35) landed, held ~1 s
  3.28       HARD CUT -> the same Anatolian star spotless (Reset(15, 0f)); job 2 hands it over; the Transylvanian medallion flies
             back in dirty: seed 14's own mix @0.95 (dust 0.50, mud 0.48, spill 0.19) with crust forced 0.30 (its plates sit
             left / centre / bottom, so the two dirty rugs do not share seed 15's right-side plates)
  6.56       LOOP = hard cut -> the same Transylvanian medallion spotless (seed 14, level 0)

Scripts: c4_stage_a (PRE2 kit, lamps off, shop closed, NewCarpetNow until rug_transylvanian, HandoverSnap, Reset(14, 0f),
SetTool(7), _picBag: anatolian to front, 10 warm-up Steps) -> c4_take_a (SeqBegin c4_rugs_r2a 3840x2160; Away 0.6; Call
HandoverNow(false); Each 0.9 { if State == Away: TakeNewCarpetNow; sim.ResetCarpet(sim.Seed, false, 0.95f) }; Away 1.8) ->
c4_run until done -> c4_end -> c4_stage_b (PayoutResume, Reset(15, 0f) on the Anatolian star, SetTool(7), _picBag: transylvanian
to front, 10 warm-up Steps) -> c4_take_b (as A; hook = TakeNewCarpetNow; d = RollDirt(14, 0.95f); d.crust = 0.30f;
sim.ResetCarpet(14, false, d)) -> run/end -> make_clips c4_rugs_r2a c4_rugs_r2b --crf 26 --preset veryslow (no --fade-loop).
Measured: cut masks differ by 0.7-1.1 % of pixels (fibre fringe only: same pose); steps 10-14 ms, capture ~66 ms per 4K frame.
_diag/: c4_look_dirt_tr, c4_look_dirt_tr2 (dirt variants on the Transylvanian medallion -> SteamStore/_work/test/c4_trdirt_*.png;
seed 15 repeats the Anatolian's plates, 16/21/23 hide the picture, 17/18 are all cobble, 14+crust 0.30 chosen), plus round 1's.
