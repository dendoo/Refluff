CarpetCleaning.CarpetDebugApi.StrokeNow(2, 0.220f, 0.315f, 0.780f, 0.315f, 56, 0.50f);
CarpetCleaning.CarpetDebugApi.StrokeNow(2, 0.780f, 0.360f, 0.220f, 0.360f, 56, 0.50f);
CarpetCleaning.CarpetDebugApi.StrokeNow(2, 0.220f, 0.405f, 0.780f, 0.405f, 56, 0.50f);
CarpetCleaning.CarpetDebugApi.RunFrames(30);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.240f, 0.325f, 0.760f, 0.325f, 104, 0.50f);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.760f, 0.395f, 0.240f, 0.395f, 104, 0.50f);
CarpetCleaning.EconomyDebugApi.SetLevel(4, 1, 6);
return CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.25f, 0.32f, 0.75f, 0.40f) + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
