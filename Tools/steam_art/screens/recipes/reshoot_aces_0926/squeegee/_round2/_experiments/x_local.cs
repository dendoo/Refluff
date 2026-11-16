// EXPERIMENT: local flood of the right two thirds, then lanes right away + render
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.30f,0.06f, 0.94f,0.94f, 9, 30, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(20);
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.40f,0.12f, 1.05f,0.12f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.40f,0.30f, 1.05f,0.30f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.40f,0.48f, 0.93f,0.48f, 50, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(5, 0.93f, 0.48f, true);
return CarpetCleaning.StoreShotApi.RenderScene("test/sq2_local4k.png", 3840, 2160);
