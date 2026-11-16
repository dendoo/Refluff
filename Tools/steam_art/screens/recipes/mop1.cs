CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 6 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_salor"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(20, 0.6f);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.03f,0.25f, 0.35f,0.75f, 6, 30, 1f);
CarpetCleaning.CarpetDebugApi.Hold(2, 0.04f, 0.40f, 1.5f);
CarpetCleaning.CarpetDebugApi.Hold(2, 0.04f, 0.60f, 1.5f);
CarpetCleaning.CarpetDebugApi.RunFrames(120);

return CarpetCleaning.CarpetDebugApi.HandoverJson() + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
