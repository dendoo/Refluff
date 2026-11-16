CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.240f, 0.320f, 0.760f, 0.320f, 62, 1.00f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.760f, 0.360f, 0.240f, 0.360f, 62, 1.00f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.240f, 0.400f, 0.760f, 0.400f, 62, 1.00f);
CarpetCleaning.CarpetDebugApi.RunFrames(30);
return CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.25f, 0.32f, 0.75f, 0.40f) + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
