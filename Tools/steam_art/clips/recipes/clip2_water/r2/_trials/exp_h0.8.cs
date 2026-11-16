// clip2 r2 experiment: fast scripted hose hold of T s at (0.49, 0.335) on the staged carpet, then fluid/fiber maps at +0/+1/+2/+4 s
// and 4K stills at +0 and +2 s -> SteamStore/_work/test/c2r2/
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.ResetWithProfile(68, 0.36f, 0f, 0f, 1f, 0f);
CarpetCleaning.CarpetDebugApi.RunFrames(2);
string d = "SteamStore/_work/test/c2r2/"; string n = "h0.8";
CarpetCleaning.CarpetDebugApi.Hold(2, 0.49f, 0.335f, 0.8f);
var sb = new System.Text.StringBuilder();
float[] at = {0f, 1f, 2f, 4f}; float tt = 0f;
foreach (var a in at) {
  CarpetCleaning.CarpetDebugApi.RunFrames(UnityEngine.Mathf.RoundToInt((a - tt) * 60f)); tt = a;
  CarpetCleaning.CarpetDebugApi.SaveDebugView("fluid", d + n + "_fluid_" + a + ".png");
  CarpetCleaning.CarpetDebugApi.SaveDebugView("fiber", d + n + "_fiber_" + a + ".png");
  if (a == 0f || a == 2f) CarpetCleaning.StoreShotApi.RenderScene("test/c2r2/" + n + "_img_" + a + ".png", 3840, 2160);
  sb.Append(a).Append(": ").Append(CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.3f, 0.15f, 0.7f, 0.55f)).Append(" | ");
}
return sb.ToString();
