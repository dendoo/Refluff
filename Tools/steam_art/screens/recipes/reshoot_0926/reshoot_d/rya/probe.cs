UnityEditor.EditorApplication.isPaused = true;
var sb = new System.Text.StringBuilder();
for (float u = 0.40f; u < 0.70f; u += 0.02f) {
  var f = CarpetCleaning.CarpetDebugApi.FiberProbeJson(u, 0.08f, u + 0.02f, 0.60f);
  var s = CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(u, 0.08f, u + 0.02f, 0.60f);
  sb.Append(u.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).Append(" ").Append(f).Append(" ").Append(s).Append("\n");
}
return sb.ToString();
