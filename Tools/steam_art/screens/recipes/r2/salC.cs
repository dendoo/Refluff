CarpetCleaning.CarpetDebugApi.StrokeNow(1, 0.43f,0.97f, 0.43f,0.30f, 50, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(1, 0.43f, 0.30f, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i=0;i<3;i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.StoreShotApi.RenderScene("test/salC.png",1920,1080);
return CarpetCleaning.CarpetDebugApi.ToolHeadJson();
