#!/bin/bash
# clip3_tools (Steam description clip 3, "Ten tools, one right order"), round 1: the Transylvanian medallion rug, seed 154's
# crust (ResetWithProfile(154, 0.6, crust = seed's 0.53, mud 0.1)), PRE2 mid-game kit + brush AREA 6. One continuous take:
# scraper x4 along a lane through the crust (dust, chunks fly onto the tile) -> light foam pass -> rotary brush scrub ->
# squeegee pushes the foam roll off the right rim onto the tile -> 1.3 s hold of the revealed lane.
# Needs Play mode running (nothing recompiles); every script keeps isPaused = true. Writes the economy save (levels).
# Self-contained: bash run.sh. ~1 min. Output: SteamStore/description/refluff_clip3_tools.mp4 (+ _poster.png, _sheet.png).
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W)"
cd /c/UnityProjects/CarpetCleaning
run() { for s in "$@"; do out=$(unity command eval_file --file "$D/$s.cs" 2>&1); r=$(echo "$out" | grep -o '"result":.*' | cut -c1-200)
  if [ -n "$r" ]; then echo "== $s $r"; else echo "== $s FAILED: $(echo "$out" | grep -i 'error\|timed' | head -2)"; exit 1; fi; done; }
run c3_eco c3_pick c3_take
for i in 1 2 3 4 5; do r=$(unity command eval_file --file "$D/c3_run.cs" 2>&1 | grep -o '"result":.*'); echo "== c3_run ${r:0:160}"; echo "$r" | grep -q '"done\\":true' && break; done
for i in 1 2 3; do r=$(unity command eval_file --file "$D/c3_end.cs" 2>&1 | grep -o '"result":.*'); echo "== c3_end ${r:0:200}"; echo "$r" | grep -q 'seqJson\\":true' && break; done
python Tools/steam_art/make_clips.py SteamStore/description/refluff_clip3_tools.mp4 c3_tools_r1 --fade-loop 0.4 --crf 22 --preset veryslow
