UnityEditor.EditorApplication.isPaused = false;
for (float u = 0.26f; u <= 0.74f; u += 0.045f) CarpetCleaning.CarpetDebugApi.StrokeNow(0, u,0.05f, u,0.42f, 40, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(60);
CarpetCleaning.StoreShotApi.RenderScene("test/va0.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.StatsJson() + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
