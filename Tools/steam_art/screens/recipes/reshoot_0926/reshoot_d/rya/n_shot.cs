UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.765f, 0.15f, true);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
sim.RefreshStatsSync();
for (int i=0;i<3;i++) UnityEditor.EditorApplication.Step();
string r = CarpetCleaning.StoreShotApi.Shot("12_rya_half_clean");
var E = CarpetCleaning.EconomyDebugApi.EconomyJson();
return r.Substring(r.Length-50) + " hint=" + sim.Hint + " clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.ToolHeadJson() + E.Substring(100, 120) + " " + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
