UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.RunFrames(90);
CarpetCleaning.CarpetDebugApi.StrokeNow(2, 0.62f,0.20f, 0.50f,0.28f, 14, 1f);
CarpetCleaning.CarpetDebugApi.Hold(2, 0.50f, 0.28f, 0.25f);
CarpetCleaning.CarpetDebugApi.PinToolHead(2, 0.50f, 0.28f, true);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
sim.UiPaused = true;
string r;
try {
  for (int i=0;i<3;i++) UnityEditor.EditorApplication.Step();
  r = CarpetCleaning.StoreShotApi.Shot("03_hose_spill");
} finally { sim.UiPaused = false; }
return r + " hint=" + sim.Hint + " clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 230);
