#!/bin/bash
# ACES re-shoot, slot 01 = 01_hero_reveal, art-director ROUND 2 (seed 242 at 0.9; ragged scraped outline instead of a
# rectangle, dried vivid band, the wand's damp track). Self-contained: bash run.sh [first_script]
# Needs Play mode running (nothing recompiles). Every script keeps EditorApplication.isPaused = true. Each eval stays under
# ~850 scripted frames: bigger ones (3 dryer rounds, 4 wet-vac rounds, 2 stain rounds in one call) passed the 5 s eval limit
# because the queued GPU work is waited for by the final stats readback.
# Optional argument: start at that script (e.g. "w_h3" skips the economy kit and the picture pick).
# Round-1 files (the straight runner-like band) are in _round1/. _lanes.inc = the outline table pasted into every r2_* file.
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W)"
cd /c/UnityProjects/CarpetCleaning
ORDER="eco_hero pick w_h3 r2_scrape1 r2_scrape2 r2_scrape3 r2_scrape4 r2_dv r2_dv r2_hose r2_brush r2_stainA r2_stainB r2_stainA r2_stainB r2_wv r2_wv r2_wv r2_wv r2_dry r2_dry r2_dry r2_floor r2_track r2_shot"
go=${1:+0}; go=${go:-1}
for s in $ORDER; do
  [ "$s" = "$1" ] && go=1
  [ "$go" = 1 ] || continue
  out=$(unity command eval_file --file "$D/$s.cs" 2>&1)
  r=$(echo "$out" | grep -o '"result":.*' | cut -c1-300)
  if [ -n "$r" ]; then echo "== $s $r"; else echo "== $s FAILED: $(echo "$out" | grep -i 'error\|timed' | head -2)"; exit 1; fi
done
python Tools/steam_art/compose_screens.py 01_hero_reveal
