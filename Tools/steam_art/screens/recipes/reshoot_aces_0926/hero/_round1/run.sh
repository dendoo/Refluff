#!/bin/bash
# ACES re-shoot, slot 01 = 01_hero_reveal, art-director round 1. Self-contained: bash run.sh [first_script]
# Needs Play mode running (nothing recompiles). Every script keeps EditorApplication.isPaused = true; each eval < 0.3 s.
# Optional argument: start at that script (e.g. "w_h3" skips the economy kit and the picture pick).
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W)"
cd /c/UnityProjects/CarpetCleaning
ORDER="eco_hero pick w_h3 w_scrape1 w_scrape2 w_scrape3 w_scrape4 w_dv1 w_band1 w_stain1 w_stain2 w_stain3 w_stain4 w_band2 w_band2b w_band3 w_shot"
go=${1:+0}; go=${go:-1}
for s in $ORDER; do
  [ "$s" = "$1" ] && go=1
  [ "$go" = 1 ] || continue
  echo "== $s"; unity command eval_file --file "$D/$s.cs" | grep -o '"result":.*' | cut -c1-400
done
python Tools/steam_art/compose_screens.py 01_hero_reveal
