#!/bin/bash
# clip5_paid (Steam description clip 5, "Get paid, upgrade your kit"), round 2 (round 1 take, new camera keys). One continuous HUD take at the full 2560x1440
# Game view size: the spotless European green rug is handed over (a real hand-over: Economy pays $120), the pay slip prints
# line by line (base, speed, spotless, bone dry, COMBO stamp, total, coins), the card is clicked away, the upgrade shop opens and
# two upgrades are bought (Brush POWER, Wet vacuum AREA). c5_frames.py then applies the virtual camera (full frame -> push in
# on the card -> pull out to the wallet while the coins fly and it counts up -> full frame for the shop) and make_clips.py encodes 1560x878, 25 fps, --fade-loop 0.4 (9.68 s).
# Needs Play mode running (nothing recompiles); every script keeps isPaused = true. Writes the economy save (pre-kit levels,
# $1,840, carpetsDelivered 6, one real hand-over +$120, two purchases -$140).
# Self-contained: bash run.sh. ~1.5 min. Output: SteamStore/description/refluff_clip5_paid.mp4 (+ _poster.png, _sheet.png).
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W)"
cd /c/UnityProjects/CarpetCleaning
TAKE=${1:-c5_paid_r1}
run() { for s in "$@"; do out=$(unity command eval_file --file "$D/$s.cs" 2>&1); r=$(echo "$out" | grep -o '"result":.*' | cut -c1-300)
  if [ -n "$r" ]; then echo "== $s $r"; else echo "== $s FAILED: $(echo "$out" | grep -i 'error\|timed' | head -2)"; exit 1; fi; done; }
python "$D/build_take.py" "$TAKE"
run c5_stage c5_take
for i in $(seq 1 10); do r=$(unity command eval_file --file "$D/c5_run.cs" 2>&1 | grep -o '"result":.*'); echo "== c5_run ${r:0:160}"; echo "$r" | grep -q '"done\\":true' && break; done
for i in 1 2 3; do r=$(unity command eval_file --file "$D/c5_end.cs" 2>&1 | grep -o '"result":.*'); echo "== c5_end ${r:0:200}"; echo "$r" | grep -q 'seqJson\\":true' && break; done
run c5_after
python "$D/c5_frames.py" "$TAKE"
python Tools/steam_art/make_clips.py SteamStore/description/refluff_clip5_paid.mp4 "${TAKE}_cam" --fade-loop 0.4 --crf 24 --preset veryslow
