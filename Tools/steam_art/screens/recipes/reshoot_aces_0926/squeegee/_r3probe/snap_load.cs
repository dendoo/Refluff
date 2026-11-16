// DEBUG ONLY: restore the snap_save copy (carpet + floor RTs, ping-pong indices, sim time)
UnityEditor.EditorApplication.isPaused = true;
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var snap = System.AppDomain.CurrentDomain.GetData("sq3snap") as System.Collections.Generic.Dictionary<string, object>;
if (snap == null) return "no snapshot";
int n = 0;
foreach (var o in new object[]{ CarpetCleaning.CarpetDebugApi.Sim, CarpetCleaning.CarpetDebugApi.Floor }) {
  foreach (var f in o.GetType().GetFields(bf)) {
    string key = o.GetType().Name + "." + f.Name;
    if (!snap.TryGetValue(key, out var v)) continue;
    if (v is UnityEngine.RenderTexture src) { UnityEngine.Graphics.CopyTexture(src, (UnityEngine.RenderTexture)f.GetValue(o)); n++; }
    else if (v is UnityEngine.RenderTexture[] sa) { var da = (UnityEngine.RenderTexture[])f.GetValue(o); for (int i = 0; i < sa.Length; i++) if (sa[i] != null) { UnityEngine.Graphics.CopyTexture(sa[i], da[i]); n++; } }
    else f.SetValue(o, v);
  }
}
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.Tools.SnapHeadingOnNextMove();
CarpetCleaning.CarpetDebugApi.RunFrames(2);
return "restored " + n;
