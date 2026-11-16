CarpetCleaning.CarpetDebugApi.Zigzag(6, 0.38f,0.27f, 0.87f,0.47f, 8, 50, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(6, 0.60f, 0.47f, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 20; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.StoreShotApi.RenderScene("test/lamp6.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.StatsJson() + CarpetCleaning.CarpetDebugApi.HighlightJson();
