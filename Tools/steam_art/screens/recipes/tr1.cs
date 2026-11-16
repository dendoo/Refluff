CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 6 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(14, 0.85f);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.30f,0.10f, 0.70f,0.90f, 13, 28, 1f);
CarpetCleaning.CarpetDebugApi.Zigzag(4, 0.30f,0.10f, 0.70f,0.90f, 14, 32, 1f);
return CarpetCleaning.CarpetDebugApi.HandoverJson() + CarpetCleaning.CarpetDebugApi.StatsJson();
