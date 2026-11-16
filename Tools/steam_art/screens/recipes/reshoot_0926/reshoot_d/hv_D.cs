UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.RunFrames(90);
CarpetCleaning.CarpetDebugApi.StrokeNow(2, 0.50f,0.08f, 0.50f,0.30f, 16, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(2, 0.50f, 0.30f, true);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
sim.RefreshStatsSync();
sim.UiPaused = true;
string r;
try {
  for (int i=0;i<3;i++) UnityEditor.EditorApplication.Step();
  r = CarpetCleaning.StoreShotApi.Shot("03_hose_spill");
} finally { sim.UiPaused = false; }
return r.Substring(r.Length-40) + " hint=" + sim.Hint + " clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(100, 60);
