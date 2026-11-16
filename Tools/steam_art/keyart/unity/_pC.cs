var keep = new System.Collections.Generic.HashSet<UnityEngine.Renderer>(CarpetCleaning.CarpetDebugApi.Heads.GetComponentsInChildren<UnityEngine.Renderer>(false));
var off = new System.Collections.Generic.List<UnityEngine.Renderer>();
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>()) if (r.enabled && !keep.Contains(r)) { r.enabled = false; off.Add(r); }
var sw=System.Diagnostics.Stopwatch.StartNew();
try { CarpetCleaning.StoreShotApi.RenderView("keyart/raw3/hero1v2_obj.png", 5760, 1860, 0.417f, 0.485f, -0.79f, 34f, -42f, 0f, 15f);
  foreach (var r in keep) r.enabled = false; CarpetCleaning.StoreShotApi.RenderView("keyart/raw3/hero1v2_empty.png", 5760, 1860, 0.417f, 0.485f, -0.79f, 34f, -42f, 0f, 15f); }
finally { foreach (var r in keep) r.enabled = true; foreach (var r in off) r.enabled = true; }
{
var cr = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var mf = cr.Shells.GetComponent<UnityEngine.MeshFilter>();
var orig = (UnityEngine.Mesh)typeof(CarpetCleaning.CarpetRenderer).GetField("_shellMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(cr);
if (mf.sharedMesh != orig) { var cur = mf.sharedMesh; mf.sharedMesh = orig; if (cur != null && cur.name == "StoreShotShells") UnityEngine.Object.Destroy(cur); }
}
{
var L0 = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L0.transform.rotation = UnityEngine.Quaternion.Euler(50f, 330f, 0f);
L0.intensity = 3f; L0.colorTemperature = 5000f; L0.shadows = UnityEngine.LightShadows.None;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.55f, 0.58f, 0.66f);
}
return "C ms=" + sw.ElapsedMilliseconds + " off=" + off.Count;
