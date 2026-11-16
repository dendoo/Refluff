UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.45f,0.60f, 0.62f,0.40f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.62f,0.40f, 0.66f,0.33f, 20, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.66f, 0.33f, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 10; i++) UnityEditor.EditorApplication.Step();
return CarpetCleaning.StoreShotApi.Shot("02_foam_brush");
