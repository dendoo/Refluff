#!/usr/bin/env bash
# Cut 1.1 retake of T7 (crust on the dirty half; README.md): run in Play mode after s00_setup.cs.
#   bash Tools/steam_art/tiktok/recipes/retake_t7.sh [stage|take]   (default: both)
export MSYS_NO_PATHCONV=1
T=Tools/steam_art/trailer/recipes/common
R=Tools/steam_art/tiktok/recipes
if [ "${1:-all}" != take ]; then
  for f in s02_reset_crust p6s1_scrape p6s2_scrape p6a1_vac p6a2_vac p6b1_hose p6b2_hose p6c1_wetvac p6c2_wetvac \
           p6d1_wetvac2 p6d2_wetvac2 p6e1_dry p6e2_dry p6f1_dry2 p6f2_dry2 p6g_settle p6h_stain p6i1_stain p6i2_stain; do
    r=$(bash $T/evp.sh $R/$f.cs)
    echo "${r:0:28} ... ${r: -300}"
  done
fi
if [ "${1:-all}" != stage ]; then
  bash $T/take.sh $R/t7_take.cs
  bash $T/evp.sh $R/look_off.cs
fi
