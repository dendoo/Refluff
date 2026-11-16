CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 6 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(14, 0.12f);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.06f,0.06f, 0.94f,0.94f, 14, 40, 1f);
return CarpetCleaning.CarpetDebugApi.HandoverJson() + CarpetCleaning.CarpetDebugApi.StatsJson();
