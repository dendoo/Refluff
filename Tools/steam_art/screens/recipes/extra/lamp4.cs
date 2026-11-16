int[] seeds = {30, 24, 39};
var sb = new System.Text.StringBuilder();
for (int k = 0; k < 3; k++) {
  CarpetCleaning.CarpetDebugApi.Reset(seeds[k], 0.3f);
  CarpetCleaning.CarpetDebugApi.RunFrames(3);
  CarpetCleaning.CarpetDebugApi.SetHighlight(1, true);
  UnityEditor.EditorApplication.isPaused = true;
  for (int i = 0; i < 6; i++) UnityEditor.EditorApplication.Step();
  UnityEditor.EditorApplication.isPaused = false;
  CarpetCleaning.StoreShotApi.RenderScene("test/lampS" + k + ".png", 960, 540);
  sb.Append(CarpetCleaning.CarpetDebugApi.DirtJson());
}
return sb.ToString();
