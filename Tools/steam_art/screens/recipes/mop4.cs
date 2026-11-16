CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.36f,0.95f, -0.36f,0.05f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.16f,-0.05f, -0.16f,0.55f, 60, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(9, -0.16f, 0.55f, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 15; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return CarpetCleaning.StoreShotApi.Shot("13_floor_mop");
