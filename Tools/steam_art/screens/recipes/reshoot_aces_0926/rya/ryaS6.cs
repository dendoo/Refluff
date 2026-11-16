UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.14f,0.975f, 0.53f,0.975f, 90, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.53f,0.982f, 0.14f,0.982f, 110, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.14f,0.965f, 0.53f,0.965f, 110, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return CarpetCleaning.CarpetDebugApi.StatsJson();
