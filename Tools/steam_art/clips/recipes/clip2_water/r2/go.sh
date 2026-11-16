#!/bin/bash
# usage: bash go.sh <take script without .cs>   runs the take, Run until done, SeqEnd
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "$0")" && pwd -W)"
cd /c/UnityProjects/CarpetCleaning
out=$(unity command eval_file --file "$D/$1.cs" 2>&1); echo "== $1 $(echo "$out" | grep -o '"result":.*' | cut -c1-600)"
echo "$out" | grep -q '"result"' || { echo "$out" | head -5; exit 1; }
for i in 1 2 3 4 5 6 7 8; do r=$(unity command eval_file --file "$D/run.cs" 2>&1 | grep -o '"result":.*'); echo "== run ${r:0:200}"; echo "$r" | grep -q '"done\\":true' && break; done
for i in 1 2 3; do r=$(unity command eval_file --file "$D/end.cs" 2>&1 | grep -o '"result":.*'); echo "== end ${r:0:300}"; echo "$r" | grep -q 'seqJson\\":true' && break; done
rm -rf SteamStore/_work/clips/c2_warm
