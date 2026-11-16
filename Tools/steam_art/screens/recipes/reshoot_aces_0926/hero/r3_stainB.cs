// ACES re-shoot 01, ROUND 3: stain extraction part B: a second brush round along every lane (directions reversed), 4 s soak.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
// ---- the cleaned outline (round 3), identical in every r3_* script: {u_far, v_far, u_near, v_near, kind}
// kind 0 = core lane (scraped on passes 1-4), 1 = offset lane between two core lanes (passes 2 + 4),
//      2 = stray stroke (short diagonal stroke out into the dirt, passes 1-4). Round 3 dropped the kind-3 light margin lanes:
//      their half-scraped plates read as a lace net of hollow rings on the band shoulders (art director, round 2).
// Lanes of different lengths and slight slants: a ragged, stepped outline instead of a rectangle. The centre lane C is the
// longest at the near end (v 0.28 = where the wet-vac wand is pinned) = the tongue the wand is pulling out right now.
float[][] LN = {
  new[]{0.345f,0.900f, 0.322f,0.360f, 0},   // A (slanted: the left edge runs from u ~0.285 far to ~0.26 near)
  new[]{0.420f,0.950f, 0.412f,0.330f, 0},   // B
  new[]{0.500f,0.930f, 0.500f,0.280f, 0},   // C (wand lane: the tongue at the near end)
  new[]{0.580f,0.970f, 0.585f,0.320f, 0},   // D
  new[]{0.650f,0.910f, 0.668f,0.380f, 0},   // E (slanted the other way: the sides are not parallel)
  new[]{0.382f,0.900f, 0.367f,0.360f, 1},   // AB
  new[]{0.460f,0.930f, 0.456f,0.330f, 1},   // BC
  new[]{0.540f,0.930f, 0.543f,0.320f, 1},   // CD
  new[]{0.615f,0.910f, 0.627f,0.380f, 1},   // DE
  new[]{0.235f,0.700f, 0.320f,0.570f, 2},   // left stray stroke, diagonal: the upper star's left white arm runs on under the crust
  new[]{0.770f,0.805f, 0.670f,0.745f, 2},   // right stray stroke, flatter: the upper star's right white arm
};
// one straight stroke along lane l (far -> near, or near -> far when rev), stamps from its length in mm
void Lane(int tool, float[] l, bool rev, float mmPerStamp, float p, float du = 0f) {
  float lenMm = UnityEngine.Mathf.Sqrt(UnityEngine.Mathf.Pow((l[0]-l[2])*1200f,2) + UnityEngine.Mathf.Pow((l[1]-l[3])*1500f,2));
  int n = UnityEngine.Mathf.Max(8, UnityEngine.Mathf.RoundToInt(lenMm / mmPerStamp));
  if (!rev) CarpetCleaning.CarpetDebugApi.StrokeNow(tool, l[0]+du,l[1], l[2]+du,l[3], n, p);
  else      CarpetCleaning.CarpetDebugApi.StrokeNow(tool, l[2]+du,l[3], l[0]+du,l[1], n, p);
}
// v extent [near, far] of the outline at u (union of every lane whose centre lies within `reach` of u); false = none
bool Cover(float u, float reach, out float vn, out float vf) {
  vn = 9f; vf = -9f;
  foreach (var l in LN) { float uc = 0.5f*(l[0]+l[2]); if (UnityEngine.Mathf.Abs(u-uc) <= reach) { vn = UnityEngine.Mathf.Min(vn, l[3]); vf = UnityEngine.Mathf.Max(vf, l[1]); } }
  return vf > vn;
}
// ---- end of the outline block
int i = 1;
foreach (var l in LN) Lane(4, l, (i++ & 1) == 1, 19.8f, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson();
