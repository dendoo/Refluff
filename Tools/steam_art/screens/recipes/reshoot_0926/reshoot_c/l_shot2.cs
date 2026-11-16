UnityEditor.EditorApplication.isPaused = true;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
float t0 = sim.SimTime;
string r;
sim.UiPaused = true;
try {
  for (int i=0;i<12;i++) UnityEditor.EditorApplication.Step();
  var cr = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
  cr.SetCursor(new UnityEngine.Vector2(0.50f, 0.36f), new UnityEngine.Vector2(0f, -1f), 178f, 0f, (CarpetCleaning.ToolId)7, true);
  r = CarpetCleaning.StoreShotApi.Shot("17_lamp_moisture");
} finally { sim.UiPaused = false; }
return r + " simTimeDelta=" + (sim.SimTime - t0) + " hint=" + sim.Hint + " clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.HighlightJson() + " " + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 230);
