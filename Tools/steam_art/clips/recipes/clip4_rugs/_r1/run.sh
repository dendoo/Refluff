#!/bin/bash
# clip4_rugs (Steam description clip 4, "A new rug every job"), round 1: the spotless Transylvanian medallion (Reset(14, 0f))
# is handed over for real (Spotless + Bone dry sparkle burst) and flies out of the top of the frame; after 0.55 s of bare
# tile the Anatolian star (bag forced, seed 15 forced to dirt level 0.95) flies in and settles, held 2.7 s. Full 3840x2160
# frame -> 1560x878 (16:9), scene only, no head. Needs Play mode running (nothing recompiles); every script keeps
# isPaused = true. Writes the economy save (PRE2 levels + one real hand-over, which pays $0 here).
# Self-contained: bash run.sh. ~1.5 min. Output: SteamStore/description/refluff_clip4_rugs.mp4 (+ _poster.png, _sheet.png).
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W)"
cd /c/UnityProjects/CarpetCleaning
run() { for s in "$@"; do out=$(unity command eval_file --file "$D/$s.cs" 2>&1); r=$(echo "$out" | grep -o '"result":.*' | cut -c1-200)
  if [ -n "$r" ]; then echo "== $s $r"; else echo "== $s FAILED: $(echo "$out" | grep -i 'error\|timed' | head -2)"; exit 1; fi; done; }
run c4_stage c4_take
for i in 1 2 3 4 5 6; do r=$(unity command eval_file --file "$D/c4_run.cs" 2>&1 | grep -o '"result":.*'); echo "== c4_run ${r:0:160}"; echo "$r" | grep -q '"done\\":true' && break; done
for i in 1 2 3; do r=$(unity command eval_file --file "$D/c4_end.cs" 2>&1 | grep -o '"result":.*'); echo "== c4_end ${r:0:200}"; echo "$r" | grep -q 'seqJson\\":true' && break; done
run c4_after
python Tools/steam_art/make_clips.py SteamStore/description/refluff_clip4_rugs.mp4 c4_rugs_r1 --fade-loop 0.4 --crf 22
