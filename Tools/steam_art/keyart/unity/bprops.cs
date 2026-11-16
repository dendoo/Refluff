var hv = CarpetCleaning.CarpetDebugApi.Heads.transform;
UnityEngine.Transform bt = null; foreach (UnityEngine.Transform c in hv) if (c.gameObject.activeSelf) bt = c;
var r = bt.GetComponentInChildren<UnityEngine.Renderer>();
var m = r.sharedMaterial; var sh = m.shader; var sb = new System.Text.StringBuilder();
for (int i = 0; i < sh.GetPropertyCount(); i++) sb.Append(sh.GetPropertyName(i) + ":" + sh.GetPropertyType(i) + " ");
sb.Append("\nhier: ");
foreach (UnityEngine.Transform c in bt) { sb.Append(c.name + "[" + c.childCount + "] "); foreach (UnityEngine.Transform d in c) sb.Append(" >" + d.name + "[" + d.childCount + "]"); }
return sb.ToString();
