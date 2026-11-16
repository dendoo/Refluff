CarpetCleaning.CarpetDebugApi.SetHighlight(1, false);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 6 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_finnish"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(13, 0.75f);
CarpetCleaning.CarpetDebugApi.RunFrames(2);
CarpetCleaning.StoreShotApi.RenderScene("test/fin0.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.HandoverJson() + CarpetCleaning.CarpetDebugApi.DirtJson();
