#!/bin/bash
# render.sh <lightfile.cs> <out rel path> <W> <H> px py pz rx ry rz fov
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -W 2>/dev/null || pwd)"
F=$D/_r_$$.cs
{ cat "$D/$1"; echo "var sw=System.Diagnostics.Stopwatch.StartNew(); var res = CarpetCleaning.StoreShotApi.RenderView(\"$2\", $3, $4, ${5}f, ${6}f, ${7}f, ${8}f, ${9}f, ${10}f, ${11}f);"; cat "$D/keylight_off.cs"; echo 'return res + " ms=" + sw.ElapsedMilliseconds;'; } > "$F"
unity command eval_file --file "$F" 2>&1 | tail -1 | cut -c1-400
rm -f "$F"
