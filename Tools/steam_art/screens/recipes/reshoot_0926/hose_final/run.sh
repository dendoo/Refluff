#!/bin/bash
# 03_hose_spill, final take 2026-09-26 (see SteamStore/_work/screens/reshoot_notes/03_hose_spill.md).
# usage: bash run.sh     (Editor in Play mode; pre2 + base h1..h6 + hv_K15 shot, compose, measure the HUD bars, restore)
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W 2>/dev/null || pwd)"
cd /c/UnityProjects/CarpetCleaning
for s in pre2 h1 h2 h3 h4 h5 h6 hv_K15; do echo "[$s]"; unity command eval_file --file "$D/$s.cs" | grep -o '"result":.*' | cut -c1-1200; done
python Tools/steam_art/compose_screens.py 03_hose_spill
python "$D/measure_bars.py" SteamStore/_work/screens/03_hose_spill_2560.png
unity command eval_file --file "$D/final.cs" | grep -o '"result":.*' | cut -c1-300
