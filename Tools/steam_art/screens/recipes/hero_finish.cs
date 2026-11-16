CarpetCleaning.CarpetDebugApi.Zigzag(6, 0.12f,0.29f, 0.88f,0.51f, 9, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.40f,0.50f, 0.66f,0.50f, 40, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(6, 0.66f, 0.50f, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 25; i++) UnityEditor.EditorApplication.Step();
return CarpetCleaning.CarpetDebugApi.StatsJson();
