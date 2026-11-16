#!/bin/bash
# clip1_reveal (Steam description clip 1, the hook): Anatolian star rug, 01-hero dirt Reset(394, 0.9f), a HORIZONTAL band
# scraped + dry-vacuumed + hosed + brushed + flooded with a second hose pass (murky slurry puddle), then the take (r3): two
# wet-vac pulls along u with real Steps, the turn between them OFF frame (no wand flail), a 2.3 s hold of the clean band.
# Needs Play mode running (nothing recompiles); every script keeps isPaused = true.
# Self-contained: bash run.sh. ~1.5 min. Output: SteamStore/description/refluff_clip1_reveal.mp4 (+ _poster.png, _sheet.png).
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W)"
cd /c/UnityProjects/CarpetCleaning
run() { for s in "$@"; do out=$(unity command eval_file --file "$D/$s.cs" 2>&1); r=$(echo "$out" | grep -o '"result":.*' | cut -c1-200)
  if [ -n "$r" ]; then echo "== $s $r"; else echo "== $s FAILED: $(echo "$out" | grep -i 'error\|timed' | head -2)"; exit 1; fi; done; }
run eco_hero pick c1_reset c1_scrape1 c1_scrape2 c1_scrape3 c1_scrape4a c1_scrape4b c1_dv c1_dv c1_hose c1_brush c1_hose c1_take
for i in 1 2 3 4 5; do r=$(unity command eval_file --file "$D/c1_run.cs" 2>&1 | grep -o '"result":.*'); echo "== c1_run ${r:0:160}"; echo "$r" | grep -q '"done\\":true' && break; done
run c1_end
rm -rf SteamStore/_work/clips/c1_warm
python Tools/steam_art/make_clips.py SteamStore/description/refluff_clip1_reveal.mp4 c1_reveal_r3 --crop 174,82,1812,755 --fade-loop 0.5 --crf 16 --preset veryslow
