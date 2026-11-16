var s = CarpetCleaning.CarpetDebugApi.StatsJson();
CarpetCleaning.StoreShotApi.RenderScene("trailer/probe/c_foam.png", 1920, 1080);
return "band " + CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.12f, 0.28f, 0.89f, 0.45f) + " | " + s;
