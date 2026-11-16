CarpetCleaning.CarpetDebugApi.Zigzag(1, 0.04f,0.04f,0.96f,0.96f, 13, 50, 1f);
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
object st = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", bf).GetValue(sim);
var s = sim.RefreshStatsSync();
float g0 = (float)st.GetType().GetField("grit").GetValue(st);
return "gritR " + (s.grit/g0) + " grit " + s.grit;
