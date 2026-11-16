UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.RunFrames(90);
CarpetCleaning.CarpetDebugApi.StrokeNow(2, 0.30f,0.42f, 0.52f,0.27f, 36, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(2, 0.52f, 0.27f, true);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
sim.RefreshStatsSync();
sim.UiPaused = true;
string r;
try {
  for (int i=0;i<3;i++) UnityEditor.EditorApplication.Step();
  r = CarpetCleaning.StoreShotApi.Shot("03_hose_spill");
} finally { sim.UiPaused = false; }
return r + " hint=" + sim.Hint + " clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + " head=" + CarpetCleaning.CarpetDebugApi.ToolHeadJson() + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 230);
