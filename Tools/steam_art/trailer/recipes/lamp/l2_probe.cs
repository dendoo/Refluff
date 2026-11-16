// X / Z probes: instant lamp, 6 real Steps, a 1280x720 render, lamp off
foreach (var w in new[]{1, 0}) {
  CarpetCleaning.CarpetDebugApi.SetHighlight(w, true, true);
  for (int i = 0; i < 6; i++) UnityEditor.EditorApplication.Step();
  CarpetCleaning.StoreShotApi.RenderScene("trailer/probe/l18j_" + (w == 1 ? "x" : "z") + ".png", 1280, 720);
  CarpetCleaning.CarpetDebugApi.SetHighlight(w, false, true);
}
UnityEditor.EditorApplication.isPaused = true;
return CarpetCleaning.CarpetDebugApi.HighlightJson();
