#!/bin/bash
# Slot 10 = 18_squeegee_floor, ROUND 2 (ACES, 2026-09-26; rejected by the art director, replaced by round 3 in ..). See README_round2.txt.
export MSYS_NO_PATHCONV=1
cd /c/UnityProjects/CarpetCleaning
D="$(cd "$(dirname "$0")" && pwd -W)"
ev() { for s in "$@"; do echo "== $s"; unity command eval_file --file "$D/$s.cs" | grep -o '"result":.*' | cut -c1-700; done; }
ev a_take pre2 r2_s1_wash r2_s2_brush \
   r2_s3_foam r2_s4_scrub r2_s5_soak  r2_s3_foam r2_s4_scrub r2_s5_soak  r2_s3_foam r2_s4_scrub r2_s4_scrub  r2_s3_foam r2_s4_scrub r2_s5_soak \
   r2_s6_rinse r2_s6_rinse r2_s6_rinse \
   r2_f0_boost r2_f1_det r2_f2_mop r2_f3_mop r2_f9_restore \
   r2_s10_boost r2_s7_suds r2_s8_suds r2_s7_suds r2_s8_suds r2_s7_suds r2_s8_suds r2_s7_suds r2_s8_suds r2_s7_suds r2_s12_restore \
   r2_f0_boost r2_f5_det r2_f6_mop r2_f9_restore r2_s16_settle \
   q_short r2_t1_lane q_short r2_u_shot r2_v_after
