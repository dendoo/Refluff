#!/bin/bash
# Slot 10 = 18_squeegee_floor (ACES, 2026-09-26). Self-contained; Play mode must be running. See README.txt.
export MSYS_NO_PATHCONV=1
cd /c/UnityProjects/CarpetCleaning
D="$(cd "$(dirname "$0")" && pwd -W)"
ev() { for s in "$@"; do echo "== $s"; unity command eval_file --file "$D/$s.cs" | grep -o '"result":.*' | cut -c1-600; done; }
ev a_take pre2 s1_wash_eu s2_brush \
   s3_foam s4_scrub s5_soak  s3_foam s4_scrub s5_soak  s3_foam s4_scrub s5_soak \
   s4_scrub s5_soak s5_soak  s4_scrub s5_soak s5_soak  s3_foam s5_soak s5_soak \
   s9_wv s10_suds s7_rinse s11_suds q_ratios t1_lanes t2_lane u_shot v_after
python Tools/steam_art/compose_screens.py 18_squeegee_floor
python -B -c "import sys; sys.path.insert(0,'Tools/steam_art'); import build_store_art as b; r=[]; b.build_screenshots(r); print(chr(10).join(r))"
python Tools/steam_art/verify_assets.py
