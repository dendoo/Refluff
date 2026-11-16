CarpetCleaning.CarpetDebugApi.StrokeNow(7, 0.040f, 0.170f, 0.960f, 0.170f, 184, 1.00f);
CarpetCleaning.CarpetDebugApi.StrokeNow(7, 0.960f, 0.133f, 0.040f, 0.133f, 184, 1.00f);
CarpetCleaning.CarpetDebugApi.StrokeNow(7, 0.040f, 0.095f, 0.960f, 0.095f, 184, 1.00f);
CarpetCleaning.CarpetDebugApi.StrokeNow(7, 0.960f, 0.057f, 0.040f, 0.057f, 184, 1.00f);
CarpetCleaning.CarpetDebugApi.StrokeNow(7, 0.040f, 0.020f, 0.960f, 0.020f, 184, 1.00f);
CarpetCleaning.CarpetDebugApi.RunFrames(180);
return CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.25f, 0.32f, 0.75f, 0.40f) + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
