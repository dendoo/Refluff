#!/bin/bash
# clip4_rugs (Steam description clip 4, "A new rug every job"), round 2: two jobs back to back, every discontinuity a hard
# cut from a rug dirty to the SAME rug spotless ("job done"). Take A: the spotless Transylvanian medallion is handed over for
# real (Spotless + Bone dry burst), flies out; the dirty Anatolian star (seed 15 @0.95) flies in, lands, held. Cut. Take B:
# the same Anatolian star spotless (Reset(15, 0f)) is handed over, flies out; the Transylvanian medallion flies back in dirty
# (seed 14 @0.95, crust 0.30), lands, held; the loop cuts to take A's first frame = that rug spotless.
# Full 3840x2160 frame -> 1560x878 (16:9), scene only, no head. Needs Play mode running (nothing recompiles); every script
# keeps isPaused = true. Writes the economy save (PRE2 levels + two real hand-overs, $0 each since V = 0).
# Self-contained: bash run.sh. ~2.5 min. Output: SteamStore/description/refluff_clip4_rugs.mp4 (+ _poster.png, _sheet.png).
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W)"
cd /c/UnityProjects/CarpetCleaning
run() { for s in "$@"; do out=$(unity command eval_file --file "$D/$s.cs" 2>&1); r=$(echo "$out" | grep -o '"result":.*' | cut -c1-300)
  if [ -n "$r" ] && ! echo "$r" | grep -q ERROR; then echo "== $s $r"; else echo "== $s FAILED: $r $(echo "$out" | grep -i 'error\|timed' | head -2)"; exit 1; fi; done; }
take() {
  run "$1"
  for i in 1 2 3 4 5 6; do r=$(unity command eval_file --file "$D/c4_run.cs" 2>&1 | grep -o '"result":.*'); echo "== c4_run ${r:0:160}"; echo "$r" | grep -q '"done\\":true' && break; done
  for i in 1 2 3; do r=$(unity command eval_file --file "$D/c4_end.cs" 2>&1 | grep -o '"result":.*'); echo "== c4_end ${r:0:200}"; echo "$r" | grep -q 'seqJson\\":true' && break; done
  run c4_after
}
run c4_stage_a; take c4_take_a
run c4_stage_b; take c4_take_b
python Tools/steam_art/make_clips.py SteamStore/description/refluff_clip4_rugs.mp4 c4_rugs_r2a c4_rugs_r2b --crf 26 --preset veryslow
