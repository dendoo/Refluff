#!/bin/bash
# usage: run.sh "<C# statements without return>" -> evals with return StatsJson
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -W 2>/dev/null || pwd)"
F=$D/_run_$$.cs
printf '%s\nreturn CarpetCleaning.CarpetDebugApi.StatsJson();\n' "$1" > "$F"
unity command eval_file --file "$F" 2>&1 | tail -1 | cut -c1-1500
rm -f "$F"
