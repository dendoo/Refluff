CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.Reset(14, 0.35f);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.15f,0.25f, 0.85f,0.75f, 7, 40, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.12f,0.66f, 1.06f,0.66f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.12f,0.52f, 1.06f,0.52f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.12f,0.38f, 0.62f,0.38f, 40, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(5, 0.62f, 0.38f, true);
CarpetCleaning.StoreShotApi.RenderScene("test/sq2.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.StatsJson();
