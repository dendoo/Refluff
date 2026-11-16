// floor: second, offset mop rings: the head eats the leftover film evenly (no hatched streaks), ring centres 210f, 510f, 810f mm off the rims, 55 mm per stamp
UnityEditor.EditorApplication.isPaused = true;
foreach (float d in new float[]{ 210f, 510f, 810f }) {
  float du = d / 1200f, dv = d / 1500f;
  int nu = Mathf.CeilToInt((1200f + 2f * d) / 55f), nv = Mathf.CeilToInt((1500f + 2f * d) / 55f);
  CarpetCleaning.CarpetDebugApi.StrokeNow(9, -du, -dv, 1f + du, -dv, nu, 1f);
  CarpetCleaning.CarpetDebugApi.StrokeNow(9, 1f + du, -dv, 1f + du, 1f + dv, nv, 1f);
  CarpetCleaning.CarpetDebugApi.StrokeNow(9, 1f + du, 1f + dv, -du, 1f + dv, nu, 1f);
  CarpetCleaning.CarpetDebugApi.StrokeNow(9, -du, 1f + dv, -du, -dv, nv, 1f);
}
return "ok";
