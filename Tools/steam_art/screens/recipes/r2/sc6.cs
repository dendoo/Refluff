CarpetCleaning.CarpetDebugApi.StrokeNow(0, 0.10f,0.30f, 0.10f,0.06f, 18, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(40);
CarpetCleaning.CarpetDebugApi.StrokeNow(0, 0.24f,0.30f, 0.24f,0.08f, 14, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(0, 0.24f, 0.08f, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i=0;i<2;i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.StoreShotApi.RenderScene("test/sc6.png",1280,720);
return CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
