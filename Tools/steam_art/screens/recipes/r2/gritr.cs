var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
object st = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", bf).GetValue(sim);
var s = sim.RefreshStatsSync();
float g0 = (float)st.GetType().GetField("grit").GetValue(st);
float m0 = (float)st.GetType().GetField("mud").GetValue(st);
float t0 = (float)st.GetType().GetField("stain").GetValue(st);
return "gritR " + (s.grit/g0) + " mudR " + (s.mud/m0) + " stainR " + (s.stain/t0) + " wet " + sim.Wetness + " hint " + sim.Hint;
