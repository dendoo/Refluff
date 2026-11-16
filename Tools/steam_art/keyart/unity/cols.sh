#!/bin/bash
# cols.sh tool u0 u1 ncols first last stamps  -> vertical strokes (alternating) at columns first..last of ncols between u0..u1
D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -W 2>/dev/null || pwd)"
$D/run.sh "var sw=System.Diagnostics.Stopwatch.StartNew(); for (int i = $5; i <= $6; i++) { float u = $2f + ($3f - $2f) * i / ($4 - 1f); if ((i & 1) == 0) CarpetCleaning.CarpetDebugApi.StrokeNow($1, u, -0.02f, u, 1.02f, $7, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow($1, u, 1.02f, u, -0.02f, $7, 1f); } UnityEngine.Debug.Log(\"cols ms \" + sw.ElapsedMilliseconds);"
