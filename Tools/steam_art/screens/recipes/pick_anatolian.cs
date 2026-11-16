for (int i = 0; i < 6 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(14, 0.75f);
CarpetCleaning.CarpetDebugApi.RunFrames(2);
CarpetCleaning.StoreShotApi.RenderScene("test/h0.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.HandoverJson() + CarpetCleaning.CarpetDebugApi.DirtJson() + CarpetCleaning.CarpetDebugApi.StatsJson();
