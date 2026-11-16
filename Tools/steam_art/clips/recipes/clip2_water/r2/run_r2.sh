#!/bin/bash
# clip2_water (Steam description clip 2, "Real water, real mud"), round 2: the Finnish rya with a dry dust-only soil
# (ResetWithProfile(68, 0.36, crust 0, mud 0, dust 1, spill 0)), hose at the player's starting POWER (level 0).
# Take: three half-pressure hose bursts -> one closed muddy patch, the foam ring heaps one mound on it, the mound settles.
# The clip is the take ROTATED to start on the foam frame (poster = pink mound on mud; natural loop there), with the
# end -> start dissolve (0.48 s) moved inside (r2/rotate_take.py).
# Needs Play mode running (nothing recompiles); every script keeps isPaused = true.
# Self-contained: bash run.sh. ~1 min. Output: SteamStore/description/refluff_clip2_water.mp4 (+ _poster.png, _sheet.png).
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")/.." && pwd -W)"   # the clip2_water folder (this script was run.sh in round 2)
cd /c/UnityProjects/CarpetCleaning
run() { for s in "$@"; do out=$(unity command eval_file --file "$D/$s.cs" 2>&1); r=$(echo "$out" | grep -o '"result":.*' | cut -c1-200)
  if [ -n "$r" ]; then echo "== $s $r"; else echo "== $s FAILED: $(echo "$out" | grep -i 'error\|timed' | head -2)"; exit 1; fi; done; }
run c2_eco r2/stage r2/take_final
for i in 1 2 3 4 5; do r=$(unity command eval_file --file "$D/r2/run.cs" 2>&1 | grep -o '"result":.*'); echo "== run ${r:0:160}"; echo "$r" | grep -q '"done\\":true' && break; done
for i in 1 2 3; do r=$(unity command eval_file --file "$D/r2/end.cs" 2>&1 | grep -o '"result":.*'); echo "== end ${r:0:200}"; echo "$r" | grep -q 'seqJson\\":true' && break; done
rm -rf SteamStore/_work/clips/c2_warm
python Tools/steam_art/clips/recipes/clip2_water/r2/rotate_take.py c2_water_r2 c2_water_r2_loop 94 --fade 0.5 || exit 1
python Tools/steam_art/make_clips.py SteamStore/description/refluff_clip2_water.mp4 c2_water_r2_loop --crf 18
