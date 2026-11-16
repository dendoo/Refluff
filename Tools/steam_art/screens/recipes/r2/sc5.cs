CarpetCleaning.CarpetDebugApi.RunFrames(60);
CarpetCleaning.CarpetDebugApi.StrokeNow(0, 0.36f,0.42f, 0.36f,0.16f, 16, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(0, 0.36f, 0.16f, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i=0;i<2;i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.StoreShotApi.RenderScene("test/sc5.png",1280,720);
return CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
