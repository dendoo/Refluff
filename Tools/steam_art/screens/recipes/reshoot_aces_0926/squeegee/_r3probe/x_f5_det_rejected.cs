// REJECTED (round 3): a detergent touch-up right before the final water left soap on the tiles that the pours whipped into violet suds; r3_f6_mop alone takes a wet spill
// floor: touch-up detergent rings (the re-wet leaked a little at the rims), ring centres 100f, 340f mm off the rims, 55 mm per stamp
UnityEditor.EditorApplication.isPaused = true;
foreach (float d in new float[]{ 100f, 340f }) {
  float du = d / 1200f, dv = d / 1500f;
  int nu = Mathf.CeilToInt((1200f + 2f * d) / 55f), nv = Mathf.CeilToInt((1500f + 2f * d) / 55f);
  CarpetCleaning.CarpetDebugApi.StrokeNow(8, -du, -dv, 1f + du, -dv, nu, 1f);
  CarpetCleaning.CarpetDebugApi.StrokeNow(8, 1f + du, -dv, 1f + du, 1f + dv, nv, 1f);
  CarpetCleaning.CarpetDebugApi.StrokeNow(8, 1f + du, 1f + dv, -du, 1f + dv, nu, 1f);
  CarpetCleaning.CarpetDebugApi.StrokeNow(8, -du, 1f + dv, -du, -dv, nv, 1f);
}
return "ok";
