CarpetCleaning.CarpetDebugApi.StrokeNow(1, 0.49f,0.97f, 0.49f,0.30f, 50, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(1, 0.49f, 0.30f, true);
UnityEditor.EditorApplication.isPaused = true;
UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.StoreShotApi.RenderScene("test/sal9.png",1920,1080);
return CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + CarpetCleaning.CarpetDebugApi.DebrisSuckJson();
