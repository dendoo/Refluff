// clip1: wet vac over the streaks (lifts the foam, soap and its dirt), then a hose rinse and a second wet vac: no soap left to foam under the later brush. Run twice.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
// the three spill streaks seed 394 lays inside the band (dirt map after c1_dv): {u0, u1, v}
float[][] SP = {
  new[]{0.13f,0.33f,0.24f}, new[]{0.13f,0.33f,0.29f}, new[]{0.13f,0.33f,0.34f}, new[]{0.13f,0.33f,0.39f}, new[]{0.13f,0.33f,0.44f},
  new[]{0.52f,0.68f,0.24f}, new[]{0.52f,0.68f,0.29f}, new[]{0.52f,0.68f,0.33f},
  new[]{0.66f,0.78f,0.38f}, new[]{0.66f,0.78f,0.43f},
};
void Seg(int tool, float[] s, bool rev, float mmPerStamp, float p) {
  int n = UnityEngine.Mathf.Max(8, UnityEngine.Mathf.RoundToInt((s[1]-s[0])*1200f / mmPerStamp));
  if (!rev) CarpetCleaning.CarpetDebugApi.StrokeNow(tool, s[0], s[2], s[1], s[2], n, p);
  else      CarpetCleaning.CarpetDebugApi.StrokeNow(tool, s[1], s[2], s[0], s[2], n, p);
}
int i = 0;
foreach (var s in SP) Seg(6, new[]{s[0]-0.03f, s[1]+0.03f, s[2]}, (i++ & 1) == 1, 12f, 1f);
foreach (var s in SP) Seg(2, s, (i++ & 1) == 1, 22f, 0.6f);
foreach (var s in SP) Seg(6, new[]{s[0]-0.03f, s[1]+0.03f, s[2]}, (i++ & 1) == 1, 12f, 1f);
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " band " + CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.12f,0.22f,0.8f,0.46f);
