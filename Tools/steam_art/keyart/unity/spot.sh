#!/bin/bash
# spot.sh u v rounds
D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -W 2>/dev/null || pwd)"
$D/run.sh "for (int k=0;k<$3;k++){ CarpetCleaning.CarpetDebugApi.Hold(3, $1f, $2f, 0.6f); CarpetCleaning.CarpetDebugApi.Zigzag(4, $1f-0.06f, $2f-0.07f, $1f+0.06f, $2f+0.07f, 6, 12, 1f); CarpetCleaning.CarpetDebugApi.Hold(4, $1f, $2f, 1.5f); CarpetCleaning.CarpetDebugApi.StrokeNow(6, $1f, $2f-0.1f, $1f, $2f+0.1f, 50, 1f); CarpetCleaning.CarpetDebugApi.StrokeNow(6, $1f, $2f+0.1f, $1f, $2f-0.1f, 50, 1f);}" | grep -o '"stain[^,]*'
