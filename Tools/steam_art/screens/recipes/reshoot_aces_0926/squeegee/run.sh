#!/bin/bash
# Slot 10 = 18_squeegee_floor, ROUND 4 (ACES, 2026-09-26; round 3 + lane starts moved off the top-left rim). Self-contained; Play mode must be running. See README.txt.
export MSYS_NO_PATHCONV=1
cd /c/UnityProjects/CarpetCleaning
D="$(cd "$(dirname "$0")" && pwd -W)"
ev() { for s in "$@"; do echo "== $s"; unity command eval_file --file "$D/$s.cs" | grep -o '"result":.*' | cut -c1-700; done; }
ev a_take pre2 \
   r3_s1_wash r3_s2_foam r3_b1_boost r3_s3_scrub r3_s3_scrub r3_s2_foam r3_s3_scrub r3_s4_soak r3_s4_soak r3_s4_soak r3_s4_soak \
   r3_s5_dry r3_s5_dry r3_s5_dry r3_s5_dry r3_s5_dry r3_s5_dry r3_s5_dry \
   r3_b2_boost r3_s6_flush r3_s6_flush r3_s6_flush r3_s6_flush r3_s6_flush r3_b9_restore \
   r3_f0_boost r3_f1_det r3_f2_mop r3_f3_mop r3_f9_restore r3_f8_settle \
   r3_b2_boost r3_s6_flush r3_s6_flush r3_s6_flush r3_b9_restore \
   r3_t0_prewet r3_f0_boost r3_f6_mop r3_f9_restore r3_f8_settle \
   q_murk r3_t1_hose r3_t2_lanes r3_t3_wait r3_t4_lane q_murk r3_u_shot r3_v_after
python Tools/steam_art/compose_screens.py 18_squeegee_floor
python -B -c "import sys; sys.path.insert(0,'Tools/steam_art'); import build_store_art as b; r=[]; b.build_screenshots(r); print(chr(10).join(r))"
python Tools/steam_art/verify_assets.py
