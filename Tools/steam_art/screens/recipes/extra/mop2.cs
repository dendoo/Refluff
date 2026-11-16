CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.20f,-0.05f, -0.20f,0.55f, 60, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(9, -0.20f, 0.55f, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 12; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.StoreShotApi.RenderScene("test/mop2.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
