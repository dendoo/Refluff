// clip2 r2 experiment: three right-button hose bursts (S s each at (0.5-D, V), (0.5, V), (0.5+D, V)) with G s dry glides, then
// maps at +0/+1/+2/+4 s and 4K stills at +0.5/+1.5/+3 -> SteamStore/_work/test/c2r2/
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.ResetWithProfile(68, 0.36f, 0f, 0f, 1f, 0f);
CarpetCleaning.CarpetDebugApi.RunFrames(2);
string d = "SteamStore/_work/test/c2r2/"; string n = "three20";
var tools = CarpetCleaning.CarpetDebugApi.Tools; tools.SnapHeadingOnNextMove();
int N = UnityEngine.Mathf.RoundToInt(0.2f * 60f);
float[] us = {0.5f - 0.09f, 0.5f, 0.5f + 0.09f};
for (int k = 0; k < 3; k++) {
  var A = new UnityEngine.Vector2(us[k], 0.345f);
  for (int i = 1; i <= N; i++) { tools.StrokeSegment((CarpetCleaning.ToolId)2, A, A, 1f/60f, 0.5f); CarpetCleaning.CarpetDebugApi.StepAll(1f/60f); }
  if (k < 2) CarpetCleaning.CarpetDebugApi.RunFrames(UnityEngine.Mathf.RoundToInt(0.3f * 60f));
}
float[] at = {0f, 0.5f, 1f, 1.5f, 2f, 3f, 4f}; float tt = 0f;
foreach (var a in at) {
  CarpetCleaning.CarpetDebugApi.RunFrames(UnityEngine.Mathf.RoundToInt((a - tt) * 60f)); tt = a;
  CarpetCleaning.CarpetDebugApi.SaveDebugView("fluid", d + n + "_fluid_" + a.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".png");
  CarpetCleaning.CarpetDebugApi.SaveDebugView("fiber", d + n + "_fiber_" + a.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".png");
  if (a == 0.5f || a == 1.5f || a == 3f) CarpetCleaning.StoreShotApi.RenderScene("test/c2r2/" + n + "_img_" + a.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".png", 3840, 2160);
}
return CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.2f, 0.15f, 0.8f, 0.55f);
