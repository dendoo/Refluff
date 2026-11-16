clip4_rugs (2026-09-26, round 3 = round 2's edit and scripts unchanged + the fly-out floor fix). Steam "About This Game" clip 4 =
"A new rug every job". 1560x878 (16:9), 25 fps, 6.56 s (164 frames), 2.87 MB (crf 27, preset veryslow; crf 26 was 3.28 MB, which
put the description total over 15 MB; now 14.66 / 15 MB with all five clips after the package step, verify_assets ALL OK), muted loop, scene only (no HUD, no tool head).
Final: SteamStore/description/refluff_clip4_rugs.mp4 (+ _poster.png = first frame = the spotless Transylvanian medallion at
rest, _sheet.png). Takes: SteamStore/_work/clips/c4_rugs_r3a/ + c4_rugs_r3b/ (82 PNGs each, 3840x2160, seq.json).
Re-run: bash run.sh (~2.5 min; sparkle positions come from CelebrationFx's xorshift state and differ run to run).

ROUND 3 FIX (art director, round 2): in every fly-out the near-left corner of the outgoing rug sank through the tiles.
Cause, CarpetHandover.ApplyPose: FlyOut position eases InQuad(t) while the tilt eases Smooth(t/0.70) around the rug centre, so
outEuler(-18, 4, 6) drops the near-left corner before the lift catches up. Measured with real Steps (_diag/c4_clamp_probe.cs):
lowest corner -77.9 mm on the way out (t 0.05-0.65), -5.6 mm on landing. The game's C# could NOT be edited in this capture
session (hard rule: no C# edits / recompiles in Play), so c4_clamp_on.cs installs the fix at runtime: a
RenderPipelineManager.beginCameraRendering hook (runs after CarpetHandover.LateUpdate, before every render) that, while the stage
flies, lifts it just enough that its lowest corner stays at its resting height. Probe with the hook: worst 0.0 mm out and in.
c4_clamp_off.cs removes it (run.sh does both). THE GAME STILL NEEDS THE SAME FIX, at the end of ApplyPose (before st.localPosition):
    // the tilt runs ahead of the lift around the rug centre: keep every corner at or above its resting height
    var half = CarpetSimulation.CarpetSizeM * 0.5f;
    float minY = 0f;
    for (int i = 0; i < 4; i++)
        minY = Mathf.Min(minY, (rot * new Vector3((i & 1) == 0 ? -half.x : half.x, 0f, (i & 2) == 0 ? -half.y : half.y)).y);
    pos.y = Mathf.Max(pos.y, -minY);
With it the game's hand-over matches this clip exactly (then c4_clamp_on becomes a no-op and can be dropped).
2026-09-26: the fix is in CarpetHandover.ApplyPose (pos.y = Mathf.Max(pos.y, CornerLift(rot)), HANDOVER.md 10), pending Play
verification with Tools/steam_art/trailer/fix_probes/v1_corner_lift.cs. Once verified, c4_clamp_on is a no-op.

Clip beats (each job 3.28 s; job 2 = the same with the rugs swapped):
  0.00-0.60  Transylvanian medallion spotless at rest (poster)
  0.60       HandoverNow(false): real hand-over (Economy pays, $0 here since V = 0), Spotless + Bone dry gold/white burst, FlyOut
             0.85 s (the near-left corner now stays on the floor while the far edge lifts, then the whole rug leaves)
  ~1.40      rug out of the top edge; 3 frames of bare tile
  ~1.46      TakeNewCarpetNow on the very Step the stage turns Away (Each hook) + dirt forced in the same hook; FlyIn 0.95 s
  ~2.2-3.28  dirty Anatolian star (seed 15 @0.95, its own mix: dust 0.88, mud 0.39, crust 0.27, spill 0.35) landed, held ~1 s
  3.28       HARD CUT -> the same Anatolian star spotless (Reset(15, 0f)); job 2 hands it over; the Transylvanian medallion flies
             back in dirty: seed 14's own mix @0.95 (dust 0.50, mud 0.48, spill 0.19) with crust forced 0.30
  6.56       LOOP = hard cut -> the same Transylvanian medallion spotless (seed 14, level 0)

Scripts: c4_clamp_on -> c4_stage_a (PRE2 kit, lamps off, shop closed, NewCarpetNow until rug_transylvanian, HandoverSnap,
Reset(14, 0f), SetTool(7), _picBag: anatolian to front, 10 warm-up Steps) -> c4_take_a (SeqBegin c4_rugs_r3a 3840x2160; Away 0.6;
Call HandoverNow(false); Each 0.9 { if State == Away: TakeNewCarpetNow; sim.ResetCarpet(sim.Seed, false, 0.95f) }; Away 1.8) ->
c4_run until done -> c4_end -> c4_stage_b (PayoutResume, Reset(15, 0f) on the Anatolian star, SetTool(7), _picBag: transylvanian
to front, 10 warm-up Steps) -> c4_take_b (as A; hook = TakeNewCarpetNow; d = RollDirt(14, 0.95f); d.crust = 0.30f;
sim.ResetCarpet(14, false, d)) -> run/end -> c4_clamp_off -> make_clips c4_rugs_r3a c4_rugs_r3b --crf 27 --preset veryslow.
Measured: steps 11-14 ms, capture ~70-77 ms per 4K frame; MP4 (780 px) frame diffs: still start 0.0, fly-out 11-26, bare-tile gap
3 frames, fly-in ramps 14 -> 1 -> 0, mid cut 13.9, loop seam 19.0 (hard cuts, same pose).
_r2/: round 2's scripts (corner clipped; mp4 was 2.99 MB). _r1/: round 1. _diag/: c4_clamp_probe (corner depth per Step, with and
without the hook), dirt-look probes.
