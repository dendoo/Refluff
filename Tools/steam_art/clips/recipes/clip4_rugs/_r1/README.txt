clip4_rugs (2026-09-26, round 1): Steam "About This Game" clip 4 = "A new rug every job". 1560x878 (16:9: the flight needs the
full game frame, the rug alone fills ~85 % of its height), 25 fps, 6.04 s (151 frames), 3.08 MB (crf 22; crf 18 was 4.73 MB and
looks the same at 780 px), muted loop, one continuous take, scene only (no HUD, no tool head).
Final: SteamStore/description/refluff_clip4_rugs.mp4 (+ _poster.png = first frame = the spotless rug at rest, _sheet.png).
Take frames: SteamStore/_work/clips/c4_rugs_r1/ (161 PNGs, full 3840x2160 render, seq.json). Re-run: bash run.sh (~1.5 min;
the sparkle positions depend on CelebrationFx's xorshift state, so they differ from run to run; the rest is deterministic).

Staging (c4_stage, fast calls + 10 warm-up Steps): PRE2 mid-game kit (power {3,4,2,2,3,2,3,4,1,1}, area {2,3,1,2,2,2,2,3,1,1},
LiveTuning(true)), lamps off, head unpinned, shop closed, PayoutResume, MarketingHud; HandoverSnap if not Idle; NewCarpetNow until
rug_transylvanian_medallion (the most vivid rug: reads "spotless" at 780 px); HandoverSnap; Reset(14, 0f) = clean + dry, so
CanHandOver and the payout is perfectClean + perfectDry + combo = the full gold + white burst; SetTool(7). Then by reflection
CarpetHandover._picBag gets rug_anatolian_star moved to index 0, so TakeNewCarpet brings it next.

Take (c4_take; real EditorApplication.Step frames, every 2 Steps; pointer off the play area throughout = no head; take T, clip t = T - 0.4):
- SeqBegin("c4_rugs_r1", 3840, 2160) (full frame).
- Away 1.40 s: the spotless rug at rest (T 0-0.4 is only the loop's crossfade target; clip 0-1.0 s).
- Call HandoverNow(false): a REAL hand-over (Economy pays on FlyOut; $0 here since V = 0), CelebrationFx burst, FlyOut 0.85 s:
  the rug tilts and accelerates up out of the top of the frame (gone at ~0.8 s).
- Away 1.40 s (FlyOut + 0.55 s of bare tile).
- Call { TakeNewCarpetNow(); sim.ResetCarpet(sim.Seed, false, 0.95f); } = the game's next carpet (seed 14 + 1 = 15) forced to
  dirt level 0.95, its own mix: dust 0.88, mud 0.39, crust 0.27, spill 0.35 = a brown, dust-dulled Anatolian star with dried-mud
  cobble patches (right side, bottom) and the star pattern still readable. notify false: the stage keeps flying.
- Away 3.65 s: FlyIn 0.95 s from above the top edge (no pop), lands, then held 2.7 s.
- c4_run until done (3 calls), c4_end.
Clip beats: 0-1.0 spotless at rest | 1.0-1.85 hand-over, sparkles, fly-out | 1.85-2.4 bare tile | 2.4-3.35 dirty rug flies in |
3.35-5.64 settled, held | 5.64-6.04 dissolve into the spotless rug (loop).
Encode: make_clips --fade-loop 0.4 --crf 22.
Rejected alternatives (looks only, _diag/c4_look_dirt.cs -> SteamStore/_work/test/c4_dirt_*.png): seeds 165 @0.9, 242 mixes with
mud 1, Reset(14, 1) fully soiled = solid brown or all cobble, the picture is gone; seed 15 @0.95 keeps it readable.
_diag/: c4_look_rugs (all five rugs spotless), c4_look_dirt (dirt variants on the Anatolian star), c4_frame_probe (HandoverSeek
through FlyOut / FlyIn: full frame needed; it did one extra real hand-over).
