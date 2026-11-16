// Hook: the Transylvanian rug (user's pick for #01/#02), round-4 dirt (17, level 1, crust 0, mud 1, dust 1, spill 0), hosed down.
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian_medallion"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(17, 1.0f, 0f, 1f, 1f, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.04f,0.04f, 0.96f,0.96f, 11, 36, 1f);
var h = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = h.IndexOf("picName");
return h.Substring(k, 45) + " " + CarpetCleaning.CarpetDebugApi.DirtJson();
