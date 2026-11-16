CarpetCleaning.CarpetDebugApi.Reset(165, 0.9f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
var g0 = CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync().grit;
CarpetCleaning.CarpetDebugApi.Zigzag(1, 0.03f,0.03f,0.97f,0.97f, 17, 50, 1f);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return g0 + " " + sim.Hint + " " + CarpetCleaning.CarpetDebugApi.StatsJson() + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0,200);
