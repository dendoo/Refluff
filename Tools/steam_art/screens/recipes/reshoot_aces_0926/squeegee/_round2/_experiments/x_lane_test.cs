// EXPERIMENT: two lanes on the flooded rug + render
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.40f,0.70f, 1.05f,0.70f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.40f,0.88f, 0.93f,0.88f, 50, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(5, 0.93f, 0.88f, true);
return CarpetCleaning.StoreShotApi.RenderScene("test/sq2_flood4k.png", 3840, 2160);
