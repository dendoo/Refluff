CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.200f, 0.300f, 0.800f, 0.300f, 111, 1.00f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.800f, 0.360f, 0.200f, 0.360f, 111, 1.00f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.200f, 0.420f, 0.800f, 0.420f, 111, 1.00f);
CarpetCleaning.EconomyDebugApi.SetLevel(7, 0, 6);
return CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.25f, 0.32f, 0.75f, 0.40f) + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
