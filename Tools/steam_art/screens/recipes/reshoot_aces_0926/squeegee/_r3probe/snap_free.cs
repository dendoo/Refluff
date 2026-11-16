// DEBUG ONLY: destroy the snap_save / snap_save2 GPU copies
int n = 0;
foreach (var key in new[]{ "sq3snap", "sq3snap2" }) {
  var snap = System.AppDomain.CurrentDomain.GetData(key) as System.Collections.Generic.Dictionary<string, object>;
  if (snap == null) continue;
  foreach (var v in snap.Values) { if (v is UnityEngine.RenderTexture r1) { UnityEngine.Object.Destroy(r1); n++; } else if (v is UnityEngine.RenderTexture[] ra) foreach (var r2 in ra) if (r2 != null) { UnityEngine.Object.Destroy(r2); n++; } }
  System.AppDomain.CurrentDomain.SetData(key, null);
}
UnityEditor.EditorApplication.isPaused = true;
return "freed " + n;
