clip2_water (2026-09-26, round 3): Steam "About This Game" clip 2 = "Real water, real mud", above the Read More fold.
1560x650 (2.4:1), 25 fps, 7.20 s (180 frames), 2.04 MB (crf 20, preset veryslow; crf 18 slow was 2.71 MB), muted loop. Re-run: bash run.sh (~35 s; a re-run gave the
same sheet to the eye, 2.714 -> 2.707 MB: the spray droplets are random).
Final: SteamStore/description/refluff_clip2_water.mp4 (+ _poster.png = first frame, _sheet.png).
Take frames: SteamStore/_work/clips/c2_water_r3/ (190 PNGs 1632x680 of a 3840x2160 render, seq.json); the encoded take is
c2_water_r3_loop/ (the rotated order: hard links + 10 blended frames). Round 2: r2/README_r2.txt, r2/run_r2.sh (take deleted);
round 1: _rejected/README_r1.txt.

Round-3 review fixes (staging, water amount and foam hold unchanged from r2):
(1) cause -> effect order: nozzle over the dry rug -> three bursts -> mud -> foam mound -> settle; no mid-clip reset;
(2) the reset dissolve (0.4 s, settled mound on mud -> dry rug, no tool in either) is at the loop wrap, clip 6.36-6.76 s,
    followed by 0.2 s of dry rug and the nozzle gliding back in; the loop point itself is two consecutive take frames;
(3) the finished mud patch with no tool in view: clip 1.92-3.52 s = 1.60 s (r2: ~0.36 s); the glassy burst discs soak in
    during its first ~0.5 s;
(4) hook: first frame = the blue nozzle arriving over the dry dusty rug (the poster), first burst at 0.32 s, all three
    bursts done by 1.4 s;
(5) crop: 1632x680 at (1104, 945) of the 3840x2160 render = the review's 1680x700 at (1080, 900) moved 24 px in and 45 px
    down (~1.18x closer than r2). The review's rectangle still showed floor slivers in both top corners (TL 23x90 px, TR
    19x81 px); this one has none (checked: no floor-coloured component touches any corner). Mud patch margins over the
    whole take (take px, darkening > 12/255 vs the dry frame): L 421, R 426, T 94, B 108 (>= 90). The glide-in waypoint
    (0.38, 0.27) is in frame; the nozzle enters from the bottom edge (take f16) and leaves at the bottom (f70).

Staging (fast scripted calls): c2_eco (pre-kit levels, $1,960, lamps owned, shop closed, marketing HUD, head unpinned), then
r2/stage (SetLevel(2, 0, 0) = hose POWER 0, P 0.756; rug_finnish_rya; ResetWithProfile(68, 0.36f, 0, 0, 1, 0); LiveTuning(true);
SetTool(2)). DirtJson: seed 68, level 0.36, dust 1, mud 0, crust 0, spill 0; StatsJson grit 0.255, stain 0.018, water 0.

Take (r3/take.cs; real EditorApplication.Step frames, every 2 Steps = 25 fps; take T, clip t = T - 0.88 s):
- ResetWithProfile again (fresh dry carpet), uncaptured warm-up c2_warm: Tool(2), Hover(0.28,-0.03) 0.30 s.
- SeqBegin("c2_water_r3", 3840, 2160, 1104, 945, 1632, 680).
- Away 0.40 s. Glide (0.28,-0.03) -> (0.38,0.27) 0.40 s -> (0.41,0.35) 0.25 s, Hover 0.12 s. Right-button (half pressure) 0.20 s
  bursts at (0.41,0.35), (0.50,0.338), (0.59,0.35) with lifted 0.25 s glides. Up 0.35 s. Out (0.70,0.13) 0.20 s -> (0.78,-0.04)
  0.15 s. Away 1.30 s (the mud soak). Tool(3). Ring in (0.56,-0.05) -> (0.52,0.25) 0.25 s -> (0.50,0.35) 0.15 s; Hold 0.90 s;
  Hover 0.15 s; out (0.68,0.14) 0.22 s -> (0.76,-0.04) 0.18 s; Away 1.50 s (the mound slumps). 7.60 s, 190 frames.
- r3/run.cs until done (2 calls), r3/end.cs.
Encode: r2/rotate_take.py c2_water_r3 c2_water_r3_loop 22 --fade 0.4 -> take frames 22..180, 10 linear-light dissolve frames
(181..190 into 1..10), 11..21; then make_clips --crf 20 --preset veryslow (no --fade-loop; package step: 2.04 MB, clip 2 sits below the fold). Poster = take f22.
Clip beats: 0.00 nozzle arriving | 0.32-1.40 three bursts, blooms merge | 1.40-1.92 nozzle rests, leaves | 1.92-3.52 mud, no
tool | 3.52 ring enters, 3.80-4.70 mound heaps | ~5.2 ring gone, mound slumps | 6.36-6.76 dissolve to dry rug | 6.96 nozzle
re-enters | 7.20 loop.
Diagnostics: r2/uvpx.cs (render px of a u/v grid), r3/go.sh <take script> (take + run + end).
