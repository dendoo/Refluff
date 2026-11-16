#!/bin/bash
# ACES re-shoot, slot 01 = 01_hero_reveal, art-director ROUND 3: crust-over-grime seed 394 at 0.9 (cake islands on dark,
# ground-in grime pile) instead of 242's even plate mosaic; no light margin lanes (their half-scraped plates made a lace net
# of hollow rings on the band shoulders); a 3rd stain round (394 has a spill band across the far part of the cut); two rim
# wet-vac pulls take the 2 chunks resting on the far rim. Outline, wash chain, dryer, track, crop and wand pin as round 2.
# Self-contained: bash run.sh [first_script]. Needs Play mode running (nothing recompiles). Every script keeps
# EditorApplication.isPaused = true; every eval stays under ~850 scripted frames.
# Round-1 / round-2 files are in _round1/ / _round2/; _lanes.inc = the outline table pasted into every r3_* file.
# Diagnostics (not in ORDER): r3_measure (dirt layers -> SteamStore/_work/test/r3_dirt.png), r3_chunks (live chunk positions),
# r3probe/probe_a|b (raw seed renders 394/174/101/242 -> SteamStore/_work/test/r3probe_*.png).
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W)"
cd /c/UnityProjects/CarpetCleaning
ORDER="eco_hero pick r3_reset r3_scrape1 r3_scrape2 r3_scrape3 r3_scrape4 r3_dv r3_dv r3_hose r3_brush r3_stainA r3_stainB r3_stainA r3_stainB r3_stainA r3_stainB r3_wv r3_wv r3_wv r3_wv r3_dry r3_dry r3_dry r3_floor r3_rim r3_track r3_shot"
go=${1:+0}; go=${go:-1}
for s in $ORDER; do
  [ "$s" = "$1" ] && go=1
  [ "$go" = 1 ] || continue
  out=$(unity command eval_file --file "$D/$s.cs" 2>&1)
  r=$(echo "$out" | grep -o '"result":.*' | cut -c1-300)
  if [ -n "$r" ]; then echo "== $s $r"; else echo "== $s FAILED: $(echo "$out" | grep -i 'error\|timed' | head -2)"; exit 1; fi
done
python Tools/steam_art/compose_screens.py 01_hero_reveal
