// EXPERIMENT: three diagonal squeegee lanes toward the near-right, far lane first; the last one stops with its upper blade end over the rim
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.614f,0.987f, 1.083f,0.739f, 55, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(20);
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.48f,0.817f, 1.083f,0.4995f, 70, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(20);
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.344f,0.651f, 0.97f,0.32f, 75, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(5, 0.97f, 0.32f, true);
CarpetCleaning.CarpetDebugApi.RunFrames(1);
return CarpetCleaning.CarpetDebugApi.PlowSpillJson() + CarpetCleaning.StoreShotApi.RenderScene("test/sq3_lanes.png", 1920, 1080);
