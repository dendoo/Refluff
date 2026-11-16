CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 6 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_finnish"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(2, 0.6f);
CarpetCleaning.CarpetDebugApi.RunFrames(2);
CarpetCleaning.StoreShotApi.RenderScene("test/du0.png", 1280, 720);
CarpetCleaning.CarpetDebugApi.StrokeNow(1, 0.15f,0.25f, 0.85f,0.25f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(1, 0.85f,0.33f, 0.15f,0.33f, 60, 1f);
CarpetCleaning.StoreShotApi.RenderScene("test/du1.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.HandoverJson()+CarpetCleaning.CarpetDebugApi.DirtJson() + CarpetCleaning.CarpetDebugApi.StatsJson();
