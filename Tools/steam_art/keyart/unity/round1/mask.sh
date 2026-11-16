#!/bin/bash
# mask.sh <out prefix rel> <W> <H> px py pz rx ry rz fov   -> <prefix>_obj.png (tool head only) and <prefix>_bg.png (nothing)
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -W 2>/dev/null || pwd)"
F=$D/_m_$$.cs
cat > "$F" <<CS
var heads = CarpetCleaning.CarpetDebugApi.Heads;
var keep = new System.Collections.Generic.HashSet<UnityEngine.Renderer>(heads.GetComponentsInChildren<UnityEngine.Renderer>(true));
var off = new System.Collections.Generic.List<UnityEngine.Renderer>();
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>()) if (r.enabled && !keep.Contains(r)) { r.enabled = false; off.Add(r); }
string res = "";
try {
  res += CarpetCleaning.StoreShotApi.RenderView("$1_obj.png", $2, $3, ${4}f, ${5}f, ${6}f, ${7}f, ${8}f, ${9}f, ${10}f);
  var off2 = new System.Collections.Generic.List<UnityEngine.Renderer>();
  foreach (var r in keep) if (r.enabled) { r.enabled = false; off2.Add(r); }
  try { res += CarpetCleaning.StoreShotApi.RenderView("$1_bg.png", $2, $3, ${4}f, ${5}f, ${6}f, ${7}f, ${8}f, ${9}f, ${10}f); }
  finally { foreach (var r in off2) r.enabled = true; }
} finally { foreach (var r in off) r.enabled = true; }
return res + " keep=" + keep.Count + " off=" + off.Count;
CS
unity command eval_file --file "$F" 2>&1 | tail -1 | cut -c1-600
rm -f "$F"
