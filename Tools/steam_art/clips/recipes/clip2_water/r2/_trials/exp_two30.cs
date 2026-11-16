// clip2 r2 experiment: two right-button hose sprays (S s each, (U0,V) and (U1,V)) with a G s dry glide between, then maps at
// +0/+1/+2/+4 s and 4K stills at +1/+3 -> SteamStore/_work/test/c2r2/
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.ResetWithProfile(68, 0.36f, 0f, 0f, 1f, 0f);
CarpetCleaning.CarpetDebugApi.RunFrames(2);
string d = "SteamStore/_work/test/c2r2/"; string n = "two30";
var tools = CarpetCleaning.CarpetDebugApi.Tools; tools.SnapHeadingOnNextMove();
int N = UnityEngine.Mathf.RoundToInt(0.3f * 60f);
var A = new UnityEngine.Vector2(0.42f, 0.345f); var B = new UnityEngine.Vector2(0.58f, 0.345f);
for (int i = 1; i <= N; i++) { tools.StrokeSegment((CarpetCleaning.ToolId)2, A, A, 1f/60f, 0.5f); CarpetCleaning.CarpetDebugApi.StepAll(1f/60f); }
CarpetCleaning.CarpetDebugApi.RunFrames(UnityEngine.Mathf.RoundToInt(0.35f * 60f));
for (int i = 1; i <= N; i++) { tools.StrokeSegment((CarpetCleaning.ToolId)2, B, B, 1f/60f, 0.5f); CarpetCleaning.CarpetDebugApi.StepAll(1f/60f); }
float[] at = {0f, 1f, 2f, 3f, 4f}; float tt = 0f;
foreach (var a in at) {
  CarpetCleaning.CarpetDebugApi.RunFrames(UnityEngine.Mathf.RoundToInt((a - tt) * 60f)); tt = a;
  CarpetCleaning.CarpetDebugApi.SaveDebugView("fluid", d + n + "_fluid_" + a + ".png");
  CarpetCleaning.CarpetDebugApi.SaveDebugView("fiber", d + n + "_fiber_" + a + ".png");
  if (a == 1f || a == 3f) CarpetCleaning.StoreShotApi.RenderScene("test/c2r2/" + n + "_img_" + a + ".png", 3840, 2160);
}
return CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.2f, 0.15f, 0.8f, 0.55f);
