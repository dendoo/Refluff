// clip1: dry vac of the band ONLY (disc r 55 mm): lanes along u every 0.04 v over the u extent the outline has there. Run twice.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
// ---- clip1 outline: a HORIZONTAL scrubbed band across the lower star (01 recipe lanes turned 90 degrees, so a 2.4:1 clip
// sees the wand travel across the frame). {u_left, v_left, u_right, v_right, kind}; kind 0 = core lane (scraper passes 1-4),
// 1 = offset lane between two core lanes (passes 2 + 4). Ragged ends (lanes of different lengths, slight slants).
float[][] LN = {
  new[]{0.150f,0.278f, 0.850f,0.286f, 0},   // A (near edge of the band)
  new[]{0.115f,0.336f, 0.885f,0.338f, 0},   // B
  new[]{0.125f,0.392f, 0.895f,0.388f, 0},   // C
  new[]{0.165f,0.440f, 0.840f,0.448f, 0},   // D (far edge)
  new[]{0.130f,0.307f, 0.870f,0.312f, 1},   // AB
  new[]{0.118f,0.364f, 0.890f,0.363f, 1},   // BC
  new[]{0.140f,0.416f, 0.870f,0.418f, 1},   // CD
};
// one straight stroke along lane l (left -> right, or right -> left when rev), stamps from its length in mm
void Lane(int tool, float[] l, bool rev, float mmPerStamp, float p, float dv = 0f) {
  float lenMm = UnityEngine.Mathf.Sqrt(UnityEngine.Mathf.Pow((l[0]-l[2])*1200f,2) + UnityEngine.Mathf.Pow((l[1]-l[3])*1500f,2));
  int n = UnityEngine.Mathf.Max(8, UnityEngine.Mathf.RoundToInt(lenMm / mmPerStamp));
  if (!rev) CarpetCleaning.CarpetDebugApi.StrokeNow(tool, l[0],l[1]+dv, l[2],l[3]+dv, n, p);
  else      CarpetCleaning.CarpetDebugApi.StrokeNow(tool, l[2],l[3]+dv, l[0],l[1]+dv, n, p);
}
// u extent [left, right] of the outline at v (union of every lane whose centre lies within `reach` of v); false = none
bool Cover(float v, float reach, out float ul, out float ur) {
  ul = 9f; ur = -9f;
  foreach (var l in LN) { float vc = 0.5f*(l[1]+l[3]); if (UnityEngine.Mathf.Abs(v-vc) <= reach) { ul = UnityEngine.Mathf.Min(ul, l[0]); ur = UnityEngine.Mathf.Max(ur, l[2]); } }
  return ur > ul;
}
// ---- end of the outline block
int i = 0;
for (float v = 0.24f; v <= 0.4801f; v += 0.04f) {
  if (!Cover(v, 0.035f, out float ul, out float ur)) continue;
  Lane(1, new[]{ul - 0.01f, v, ur + 0.01f, v}, (i++ & 1) == 1, 15.4f, 1f);
}
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
