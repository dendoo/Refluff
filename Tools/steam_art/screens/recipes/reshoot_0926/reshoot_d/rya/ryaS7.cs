UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.14f,0.992f, 0.53f,0.992f, 90, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.53f,0.995f, 0.14f,0.995f, 130, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.14f,0.99f, 0.53f,0.99f, 130, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return CarpetCleaning.CarpetDebugApi.StatsJson();
