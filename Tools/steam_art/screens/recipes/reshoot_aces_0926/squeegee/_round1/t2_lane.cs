// the lane in progress: the blade stops short of the right edge with its roll of brown water pouring over the rim
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(5, -0.03f,0.47f, 0.93f,0.47f, 90, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(5, 0.93f, 0.47f, true);
return CarpetCleaning.StoreShotApi.RenderScene("test/sq_look.png", 1280, 720);
