CarpetCleaning.CarpetDebugApi.Zigzag(3, 0.50f,0.20f, 0.64f,0.80f, 6, 18, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.52f,0.78f, 0.60f,0.55f, 40, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.60f,0.55f, 0.54f,0.40f, 35, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.54f,0.40f, 0.60f,0.30f, 30, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.60f, 0.30f, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 15; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return CarpetCleaning.StoreShotApi.Shot("12_rya_half_clean");
