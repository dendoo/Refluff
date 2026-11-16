clip3_tools (2026-09-26, round 1): Steam "About This Game" clip 3 = "Ten tools, one right order". 1560x650 (2.4:1, like
clips 1-2), 25 fps, 8.76 s (219 frames), 3.47 MB (crf 22, preset veryslow; crf 20 slow was 4.69 MB, crf 18 5.89 MB), muted loop, one continuous take.
Final: SteamStore/description/refluff_clip3_tools.mp4 (+ _poster.png = first frame, _sheet.png).
Take frames: SteamStore/_work/clips/c3_tools_r1/ (229 PNGs 1680x700 of a 3840x2160 render, seq.json). Re-run: bash run.sh
(~1 min; a re-run gave the same sheet, 4,691,905 -> 4,691,125 bytes: the chunks and dust are random).

Staging (fast calls): c3_eco = PRE2 mid-game kit (screenshot 03's): power {3,4,2,2,3,2,3,4,1,1}, area {2,3,1,2,2,2,2,3,1,1},
$1,960, lamps owned + off, shop closed, marketing HUD, head unpinned; c3_pick = NewCarpetNow until rug_transylvanian_medallion.
c3_take itself then: SetLevel(4, 1, 6) (brush AREA 6: pad r 78 mm, head x1.32 of PRE2, so the blue bristle skirt stands clear
of the foam; at AREA 3 the pad was buried and only its grey top showed), ResetWithProfile(154, 0.6, crust -1 (= seed's 0.53),
mud 0.1, dust -1, spill -1) = seed 154's dried-mud cake over the lower-right quadrant (screenshot 03's crust) with less grime
so the revealed lane reads red (mud at the seed's 0.385 left it browner), LiveTuning(true), SetTool(0), RunFrames(3).

Take (c3_take.cs; real EditorApplication.Step frames via StoreGifApi's scripted pointer, every 2 Steps; take T, clip t = T - 0.4):
- SeqBegin("c3_tools_r1", 3840, 2160, 1560, 1088, 1680, 700): lane v 0.26 at render y ~1438 = crop middle; the uncrusted red
  field + medallion on the left third, the right rim (u 1.0, x ~2870) and ~300 px of tile on the right for the spill.
- Scraper: Hover(0.585,0.26) 0.20 s; Stroke(0.62,0.26 -> 0.98,0.26, 0.50 s); 3 more pressed passes back and forth 0.50 s each
  (v 0.262 / 0.258 wobble) = 4 passes at ~720 mm/s (the scraper chips per stamp, so 4 passes break the plates; 1 pass left a
  lace of plate); dust clouds, chunks fly onto the tile. Up 0.12 s.
- Tool(3) foam: Stroke(0.62 -> 0.97, 0.80 s, right button = half pressure) = one pink slab over the lane. Up 0.12 s.
- Tool(4) brush: pressed scrub legs 0.97,0.26 -> 0.64,0.24 -> 0.95,0.28 -> 0.66,0.26 (0.70 s each), kept INSIDE the lane (a leg
  to u 0.47 dragged a foam blob left of where the squeegee starts). Up 0.10 s.
- Tool(5) squeegee: lifted glide 0.66 -> 0.49 (0.30 s; heading already along u, so no snap), pressed push 0.49 -> 1.12 at v 0.26
  (1.40 s): the roll rides ahead and pours off the right rim onto the tile. Lifted exit to (1.42, 0.275) 0.40 s (out of crop).
- Away 1.30 s: the revealed lane (red field + border rosette, damp) next to intact crust, foam heap + chunks on the tile.
- c3_run until done (2 calls), c3_end.
Encode: make_clips --fade-loop 0.4 --crf 22 --preset veryslow (package step, 15 MB budget: 3.47 MB; it opens the page) (starts at take 0.4 s = scraper pressing at the lane's left end = poster; the last
0.4 s of the hold dissolves into it). Clip beats: 0-2.0 scraper | 2.1-3.0 foam | 3.2-5.3 brush | 5.4-7.5 squeegee + pour |
7.5-8.4 result held | 8.4-8.76 dissolve.
Diagnostics: c3_uvpx (render px of patch / rim / floor points), c3_probe (lane fluid state). _trials/ = rejected takes t1..t4
(t1: 3 v-pulls + two foam passes = foam buried everything; t2/t3: brush hidden in foam, foam blob left at the lane end) and
the staging looks (_look_a/b/c: 154@0.6, 242@0.9, 242 forced) with their SteamStore/_work/test/c3_look_*.png renders.
