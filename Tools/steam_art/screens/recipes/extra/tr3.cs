CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.50f,0.55f, 0.50f,0.86f, 40, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(6, 0.50f, 0.86f, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 10; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return CarpetCleaning.StoreShotApi.Shot("15_medallion_swath", true);
