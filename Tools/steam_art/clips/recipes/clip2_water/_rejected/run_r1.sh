#!/bin/bash
# clip2_water (Steam description clip 2, "Real water, real mud"): the Finnish rya with a dry dust-only soil
# (ResetWithProfile(68, 0.36, crust 0, mud 0, dust 1, spill 0)); the take: the hose soaks the centre (1.0 s), the puddle
# spreads and turns murky brown (dust -> mud), the nozzle leaves, the foam ring comes in and heaps a pink mound, 2.8 s hold.
# Needs Play mode running (nothing recompiles); every script keeps isPaused = true.
# Self-contained: bash run.sh. ~40 s. Output: SteamStore/description/refluff_clip2_water.mp4 (+ _poster.png, _sheet.png).
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W)"
cd /c/UnityProjects/CarpetCleaning
run() { for s in "$@"; do out=$(unity command eval_file --file "$D/$s.cs" 2>&1); r=$(echo "$out" | grep -o '"result":.*' | cut -c1-200)
  if [ -n "$r" ]; then echo "== $s $r"; else echo "== $s FAILED: $(echo "$out" | grep -i 'error\|timed' | head -2)"; exit 1; fi; done; }
run c2_eco c2_stage c2_take
for i in 1 2 3 4 5; do r=$(unity command eval_file --file "$D/c2_run.cs" 2>&1 | grep -o '"result":.*'); echo "== c2_run ${r:0:160}"; echo "$r" | grep -q '"done\\":true' && break; done
run c2_end
rm -rf SteamStore/_work/clips/c2_warm
python Tools/steam_art/make_clips.py SteamStore/description/refluff_clip2_water.mp4 c2_water_r1 --fade-loop 0.5
