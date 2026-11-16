CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false);
CarpetCleaning.CarpetDebugApi.Reset(14, 0.9f);
CarpetCleaning.CarpetDebugApi.RunFrames(2);
CarpetCleaning.StoreShotApi.RenderView("keyart/test/td90.png", 600, 900, 0f, 2.8f, 0f, 90f, 0f, 0f, 30f);
CarpetCleaning.StoreShotApi.RenderView("keyart/test/td78.png", 600, 900, 0f, 2.75f, -0.6f, 78f, 0f, 0f, 30f);
return CarpetCleaning.CarpetDebugApi.DirtJson() + CarpetCleaning.CarpetDebugApi.StatsJson();
