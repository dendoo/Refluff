// clip2 r2 experiment: fast scripted hose spray (pressure P, S seconds, path (U0,V0)->(U1,V1)) on the staged carpet, then
// fluid/fiber maps at +0/+0.5/+1/+2/+4 s and 4K stills at +0.5/+2 -> SteamStore/_work/test/c2r2/
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.ResetWithProfile(68, 0.36f, 0f, 0f, 1f, 0f);
CarpetCleaning.CarpetDebugApi.RunFrames(2);
string d = "SteamStore/_work/test/c2r2/"; string n = "sw50";
var tools = CarpetCleaning.CarpetDebugApi.Tools; tools.SnapHeadingOnNextMove();
int N = UnityEngine.Mathf.RoundToInt(0.5f * 60f); var A = new UnityEngine.Vector2(0.43f, 0.335f); var B = new UnityEngine.Vector2(0.57f, 0.335f); var prev = A;
for (int i = 1; i <= N; i++) { var cur = UnityEngine.Vector2.Lerp(A, B, i / (float)N); tools.StrokeSegment((CarpetCleaning.ToolId)2, prev, cur, 1f/60f, 0.5f); CarpetCleaning.CarpetDebugApi.StepAll(1f/60f); prev = cur; }
var sb = new System.Text.StringBuilder();
float[] at = {0f, 0.5f, 1f, 2f, 4f, 5.5f}; float tt = 0f;
foreach (var a in at) {
  CarpetCleaning.CarpetDebugApi.RunFrames(UnityEngine.Mathf.RoundToInt((a - tt) * 60f)); tt = a;
  CarpetCleaning.CarpetDebugApi.SaveDebugView("fluid", d + n + "_fluid_" + a + ".png");
  CarpetCleaning.CarpetDebugApi.SaveDebugView("fiber", d + n + "_fiber_" + a + ".png");
  if (a == 0.5f || a == 2f) CarpetCleaning.StoreShotApi.RenderScene("test/c2r2/" + n + "_img_" + a + ".png", 3840, 2160);
}
return CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.3f, 0.15f, 0.7f, 0.55f);
