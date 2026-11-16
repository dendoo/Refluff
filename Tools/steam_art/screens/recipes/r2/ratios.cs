var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
object st = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", bf).GetValue(sim);
var s = sim.RefreshStatsSync();
System.Func<string,float> g = n => (float)st.GetType().GetField(n).GetValue(st);
return "crustR " + (s.crust/g("crust")) + " gritR " + (s.grit/g("grit")) + " grit " + s.grit + " mudR " + (s.mud/g("mud")) + " stainR " + (s.stain/g("stain")) + " wet " + sim.Wetness + " soapLeft " + sim.SoapLeft + " slurry " + s.slurry + " pooled " + s.pooledCells + " hint " + sim.Hint;
