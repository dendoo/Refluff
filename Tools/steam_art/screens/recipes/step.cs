CarpetCleaning.CarpetDebugApi.PinToolHead(6, 0.70f, 0.45f, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 20; i++) UnityEditor.EditorApplication.Step();
CarpetCleaning.StoreShotApi.RenderScene("test/h3.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.ToolHeadJson();
