// DEBUG ONLY (not part of the take): GPU copy of every carpet + floor sim RenderTexture and ping-pong index, kept in AppDomain data
UnityEditor.EditorApplication.isPaused = true;
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var snap = new System.Collections.Generic.Dictionary<string, object>();
System.Func<UnityEngine.RenderTexture, UnityEngine.RenderTexture> Clone = rt => { var c = new UnityEngine.RenderTexture(rt.descriptor); c.hideFlags = UnityEngine.HideFlags.DontUnloadUnusedAsset; c.Create(); UnityEngine.Graphics.CopyTexture(rt, c); return c; };
int n = 0;
foreach (var o in new object[]{ CarpetCleaning.CarpetDebugApi.Sim, CarpetCleaning.CarpetDebugApi.Floor }) {
  foreach (var f in o.GetType().GetFields(bf)) {
    string key = o.GetType().Name + "." + f.Name;
    if (f.FieldType == typeof(UnityEngine.RenderTexture)) { var rt = (UnityEngine.RenderTexture)f.GetValue(o); if (rt != null) { snap[key] = Clone(rt); n++; } }
    else if (f.FieldType == typeof(UnityEngine.RenderTexture[])) { var a = (UnityEngine.RenderTexture[])f.GetValue(o); if (a != null) { var c = new UnityEngine.RenderTexture[a.Length]; for (int i = 0; i < a.Length; i++) if (a[i] != null) { c[i] = Clone(a[i]); n++; } snap[key] = c; } }
    else if ((f.FieldType == typeof(int) && f.Name.StartsWith("_cur")) || f.Name == "_simTime") snap[key] = f.GetValue(o);
  }
}
var old = System.AppDomain.CurrentDomain.GetData("sq3snap2") as System.Collections.Generic.Dictionary<string, object>;
if (old != null) foreach (var v in old.Values) { if (v is UnityEngine.RenderTexture r1) UnityEngine.Object.Destroy(r1); else if (v is UnityEngine.RenderTexture[] ra) foreach (var r2 in ra) if (r2 != null) UnityEngine.Object.Destroy(r2); }
System.AppDomain.CurrentDomain.SetData("sq3snap2", snap);
return "saved " + n + " RTs, keys " + snap.Count;
