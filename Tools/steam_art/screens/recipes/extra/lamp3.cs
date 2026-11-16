CarpetCleaning.CarpetDebugApi.Reset(14, 0.15f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
var s0 = CarpetCleaning.CarpetDebugApi.StatsJson();
CarpetCleaning.CarpetDebugApi.SetHighlight(1, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 15; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.StoreShotApi.RenderScene("test/lamp3.png", 1280, 720);
return s0 + CarpetCleaning.CarpetDebugApi.HighlightJson();
