CarpetCleaning.CarpetDebugApi.Zigzag(7, 0.06f,0.08f, 0.50f,0.92f, 7, 60, 1f);
CarpetCleaning.CarpetDebugApi.SetHighlight(0, true);
CarpetCleaning.CarpetDebugApi.SetTool(7);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 12; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.StoreShotApi.RenderScene("test/dry4.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.StatsJson() + CarpetCleaning.CarpetDebugApi.HighlightJson();
