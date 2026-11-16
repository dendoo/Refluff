clip2_water (2026-09-26, round 1): Steam "About This Game" clip 2 = "Real water, real mud", above the Read More fold.
1560x650 (2.4:1), 25 fps, 7.08 s, 4.05 MB (crf 18), muted loop.
Final: SteamStore/description/refluff_clip2_water.mp4 (+ _poster.png = first frame, _sheet.png).
Take frames: SteamStore/_work/clips/c2_water_r1/ (189 PNGs 1920x800 of a 3840x2160 render, seq.json). Re-run: bash run.sh (~40 s).

Staging (fast scripted calls):
- c2_eco (= clip1's eco_hero): pre-kit tool levels, $1,960, both lamps owned, shop closed, marketing HUD, head unpinned.
- c2_stage: NewCarpetNow until rug_finnish_rya (blue field + ochre lozenges: the grey dust film and the brown mud both read
  on it, the pink foam pops on the blue), ResetWithProfile(68, 0.36f, crust 0, mud 0, dust 1, spill 0), LiveTuning(true),
  SetTool(2). DirtJson: seed 68, level 0.36, spread 1.037, dust 1, mud 0, crust 0, spill 0. StatsJson: grit 0.255, stain 0.018,
  mud 0, crust 0 = a DRY carpet whose only soil is dust. Seed 68 was picked offline (scratchpad seeds.py over dirt_preview.py:
  seeds 1-399 x levels 0.30-0.50, mix forced as above): its soil S sits in the dust sweet spot (S 0.35-0.55, grit ~0.3 = the
  maximum DIRT_DUST x dust) over 81 % of u 0.12-0.88 / v 0.12-0.55, with almost no Worley stain (0.028).
- Why mud 0: the brown in the clip is then only the dust: the hose wets the pile, K_G2M turns grit into mud (0.5/s x wet),
  mud dissolves into the water as slurry, so the puddle goes murky brown and the wicked front stays dark brown.

Take (c2_take.cs, real EditorApplication.Step frames through StoreGifApi's scripted pointer, every 2 Steps = 25 fps):
- Uncaptured warm-up in the same eval (SeqBegin "c2_warm" every=100000): Tool(2), Hover(0.46,0.335, 0.3 s).
- SeqBegin("c2_water_r1", 3840, 2160, 960, 890, 1920, 800): centred on u 0.5 / v 0.335 (render x 1920, y 1290; crop spans
  v ~0.17-0.53, two small tile wedges at the top corners).
- Hose 1.0 s: Hold(0.46,0.335) 0.40 s, drag to (0.505,0.345) 0.30 s and (0.535,0.332) 0.30 s. Up 0.20 s. The nozzle drifts
  out right (0.80,0.36) 0.35 s -> (1.12,0.38) 0.25 s, Away 0.10 s, Tool(3) (the foam sprayer has no model: the ring cursor).
  The ring glides back (1.12,0.40) -> (0.75,0.36) 0.35 s -> (0.49,0.34) 0.35 s; foam Hold 0.70 s (the mound heaps), drag to
  (0.53,0.35) 0.60 s, Hold 0.25 s; the ring leaves (0.80,0.40) 0.35 s -> (1.15,0.43) 0.25 s; Away 2.80 s (the wet front keeps
  wicking out as a dark ring; the mound sits). 7.56 s, 189 frames. c2_run until done (2 calls), c2_end.
- Why 1.0 s of water: 2.8 s (t1) and 1.5 s (t2, t4 S-curve drag) flooded the whole frame height with a brown lake; any
  hose path gives a round puddle (the free water evens out faster than the nozzle moves). 1.0 s ends at ~55 % of the frame
  width with dry dusty rug on both sides. The nozzle leaves the frame before the switch so the spreading puddle shows
  unobstructed and the head never pops from nozzle to ring in view.
Encode: make_clips --fade-loop 0.5 (crf 18). The clip starts 0.5 s into the take: poster = nozzle spraying with a ~0.2 m
murky puddle and droplets on the dry rug; last 0.5 s crossfades the mound back into that start.
Diagnostics: c2_look2 (4K render of the current rug with the profile), c2_uvpx (render px of a u/v grid),
c2_rugtest.tpl (rug survey template: RUG / SEED / LEVEL placeholders). _rejected/: trial takes t1-t4, c2_survey.tpl.
