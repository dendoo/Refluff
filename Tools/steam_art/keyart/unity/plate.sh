#!/bin/bash
# plate.sh name rawW rawH maskW maskH "px py pz rx ry rz fov" u v raise "pitch yaw int K ar ag ab" [nohead=1]
# -> keyart/raw3/<name>_full.png, _nohead.png (raw size), _obj.png, _empty.png (mask size)
D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -W 2>/dev/null || pwd)"
cd $D
N=$1; RW=$2; RH=$3; MW=$4; MH=$5; read -a c <<< "$6"; U=$7; V=$8; R=$9; LT="${10}"; NOHEAD=${11:-1}
C="${c[0]}f, ${c[1]}f, ${c[2]}f, ${c[3]}f, ${c[4]}f, ${c[5]}f, ${c[6]}f"
HEADS='var keep = new System.Collections.Generic.HashSet<UnityEngine.Renderer>(CarpetCleaning.CarpetDebugApi.Heads.GetComponentsInChildren<UnityEngine.Renderer>(false));'
{ ./head.sh $U $V $R 40; ./shells_on.sh 48; ./light.sh $LT
  echo "var sw=System.Diagnostics.Stopwatch.StartNew(); CarpetCleaning.StoreShotApi.RenderView(\"keyart/raw3/${N}_full.png\", $RW, $RH, $C);"
  echo 'return "A ms=" + sw.ElapsedMilliseconds;'; } > _pA.cs
./ev.sh _pA.cs | grep -o '"result":"[^"]*"'
{ echo "$HEADS"
  echo 'var sw=System.Diagnostics.Stopwatch.StartNew(); string res="";'
  if [ "$NOHEAD" = "1" ]; then
  echo 'foreach (var r in keep) r.enabled = false;'
  echo "try { CarpetCleaning.StoreShotApi.RenderView(\"keyart/raw3/${N}_nohead.png\", $RW, $RH, $C); } finally { foreach (var r in keep) r.enabled = true; }"
  fi
  echo 'return "B ms=" + sw.ElapsedMilliseconds + " keep=" + keep.Count;'; } > _pB.cs
./ev.sh _pB.cs | grep -o '"result":"[^"]*"'
{ echo "$HEADS"
  echo 'var off = new System.Collections.Generic.List<UnityEngine.Renderer>();'
  echo 'foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>()) if (r.enabled && !keep.Contains(r)) { r.enabled = false; off.Add(r); }'
  echo 'var sw=System.Diagnostics.Stopwatch.StartNew();'
  echo "try { CarpetCleaning.StoreShotApi.RenderView(\"keyart/raw3/${N}_obj.png\", $MW, $MH, $C);"
  echo "  foreach (var r in keep) r.enabled = false; CarpetCleaning.StoreShotApi.RenderView(\"keyart/raw3/${N}_empty.png\", $MW, $MH, $C); }"
  echo 'finally { foreach (var r in keep) r.enabled = true; foreach (var r in off) r.enabled = true; }'
  cat shells_off.cs lt_base.cs
  echo 'return "C ms=" + sw.ElapsedMilliseconds + " off=" + off.Count;'; } > _pC.cs
./ev.sh _pC.cs | grep -o '"result":"[^"]*"'
