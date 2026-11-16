clip1_reveal (2026-09-26, round 3): Steam "About This Game" clip 1 = the hook, above the Read More fold. 1560x650 (2.4:1),
25 fps, 9.96 s, 3.46 MB (crf 16, preset veryslow; was 3.92 MB at preset slow), muted loop.
Final: SteamStore/description/refluff_clip1_reveal.mp4 (+ _poster.png = first frame, _sheet.png).
Take frames: SteamStore/_work/clips/c1_reveal_r3/ (261 PNGs 2160x900 of a 3840x2160 render, seq.json). Re-run: bash run.sh
(~1 min; deterministic: the r3 staging printed the same numbers as r2, and frames 13/60/110 matched r2 to the pixel mean).

Staging (fast scripted StrokeNow), unchanged since r2:
- eco_hero + pick (from screens/recipes/reshoot_aces_0926/hero): pre-kit tool levels, rug_anatolian_star.
- c1_reset: Reset(394, 0.9f) = the store screenshot 01 dirt (crust-over-grime), LiveTuning(true).
- _lanes.inc: the 01 outline turned 90 degrees = 4 core + 3 offset scraper lanes ALONG u at v 0.278..0.448, ragged ends
  u 0.115-0.165 .. 0.84-0.895: a horizontal band across the lower star medallion (octagon + white arms + both side borders).
- c1_scrape1..4b (4 dry scraper passes, 7.3 mm/stamp), c1_dv x2 (dry vac of the band only), c1_hose (core lanes, p 0.6),
  c1_brush (2 rounds), c1_hose AGAIN = the flood: h ~1.9-2.5 mm of murky slurry over the band. The flood is what makes the
  single real-time pass read clean: the wet vac takes the free water with its slurry in one pass, and only the damp pile's
  share is left to WETVAC_FLUSH (4/s x ~0.2 s contact). Without it one pass left slurry 0.26 (dark, muddy reveal); with it 0.09.
- No stain rounds: foam + brush over the band leaves a pink foam slab, and any leftover soap re-foams under the brush
  (rejected: _rejected/c1_stainA/B, c1_murk, c1_wvpre, c1_spotA/B/spotvac). So seed 394's spill streaks stay (whites slightly creamy).

Take (c1_take.cs, real EditorApplication.Step frames through StoreGifApi's scripted pointer, every 2 Steps = 25 fps):
- Uncaptured warm-up in the same eval (SeqBegin "c1_warm" every=100000): Tool(6), Hover(0.06,0.30), Stroke(0.08,0.30 -> 0.40,0.30, 1.28 s).
- SeqBegin("c1_reveal_r3", 3840, 2160, 840, 802, 2160, 900): row 1 continues 0.40 -> 0.90 at v 0.30 (2.40 s, 250 mm/s).
- The turn is OFF frame (r3 fix of r2's flail): lifted swing out 0.90,0.30 -> 0.98,0.306 (0.26 s) -> 1.12,0.316 (0.24 s),
  Away 0.10 s (no head while the pointer steps across the rows), lifted swing back in 1.14,0.404 -> 0.98,0.416 (0.30 s) ->
  0.905,0.42 (0.26 s). Why: ToolController's heading is an axis, so a pure reversal does not rotate the head, but any
  travel across the rows (v) swings the bright wand 90-180 degrees in view; with the step across done by Away beyond the
  crop, the wand leaves and comes back pointing along v (~5 degrees of drift). It is off frame for ~0.6 s (clip 2.15-2.75 s),
  a beat in which the finished lower row shows unobstructed.
- Row 2 0.905 -> 0.10 at v 0.42 (3.86 s, 250 mm/s); lift ALONG the row 0.10,0.42 -> 0.085,0.422 (0.16 s; r2's lift to
  0.09,0.43 tilted the wand ~30 degrees just before its exit); exit left to (-0.35, 0.46) 0.56 s; Away 2.30 s (was 1.30).
- c1_run until done (3 calls), c1_end.
Encode: make_clips --crop 174,82,1812,755 --fade-loop 0.5 --crf 16 --preset veryslow (package step: 3.46 MB). The crop = render (1014,884,1812,755): centred on u 0.5
(render x 1920), 2.4:1; the carpet rims (render x 990 / 2850 at v 0.30, 1044 / 2796 at v 0.42; c1_uvpx) meet the crop edges
at mid height, so only two small floor wedges show at the top corners, the right-floor crumb (take px ~2122,502) is out, the
band fills ~80 % of the width and the octagon is ~170 px wide at 780. The clip starts 0.5 s into the take (poster = wand in the
middle of the octagon, lower-left of the star already out of the slurry); lower row fully out by 1.9 s; the clean band with
no wand holds ~2.1 s (7.35-9.46 s) before the last 0.5 s crossfade into the start.
Diagnostics: c1_look (4K render + dirt/crust/fluid maps -> SteamStore/_work/test/c1_*), c1_stainmap, c1_probe (band h / s / w per
row), c1_uvpx (render px of rim points for the crop / the off-frame turn). _rejected/c1_take_r2.cs = the r2 take (in-view turn).
