#!/bin/bash
# ACES re-shoot, slot 05 = 12_rya_half_clean, art-director round 2. Self-contained: bash run.sh [first_script]
# Needs Play mode running (nothing recompiles). Every script keeps EditorApplication.isPaused = true; each eval < 0.4 s.
# Optional argument: start at that script (e.g. "w_ryaA" skips the economy kit and the picture pick).
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W)"
cd /c/UnityProjects/CarpetCleaning
ORDER="pre2 pick_rya w_ryaA ryaScrape ryaScrape2 ryaScrape3 ryaScrape4 ryaDv ryaDv2 w_rya2 ryaS ryaS2 ryaS3 ryaS4 ryaS5 ryaS6 ryaS7 w_ryaW ryaW2 ryaR ryaR2 ryaW3 w_ryaD w_ryaF ryaFloor1 ryaFloor2 n_dry n_dry2 n_foam1 n_foam2 n_shot"
go=${1:+0}; go=${go:-1}
for s in $ORDER; do
  [ "$s" = "$1" ] && go=1
  [ "$go" = 1 ] || continue
  echo "== $s"; unity command eval_file --file "$D/$s.cs" | grep -o '"result":.*' | cut -c1-400
done
python Tools/steam_art/compose_screens.py 12_rya_half_clean
