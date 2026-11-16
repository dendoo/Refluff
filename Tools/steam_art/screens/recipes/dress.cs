// HUD dressing for a carpet shown already clean: V and clock as if it had been cleaned (reflection, no asset touched)
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var eco = CarpetCleaning.EconomyDebugApi.Eco; var sim = CarpetCleaning.CarpetDebugApi.Sim;
var fi = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", bf);
object st = fi.GetValue(sim);
st.GetType().GetField("mud").SetValue(st, 0.22f);
st.GetType().GetField("grit").SetValue(st, 0.10f);
st.GetType().GetField("stain").SetValue(st, 0.06f);
st.GetType().GetField("valid").SetValue(st, true);
fi.SetValue(sim, st);
typeof(CarpetCleaning.Economy).GetField("_clockAnchor", bf).SetValue(eco, sim.SimTime - 257.4f);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 20; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 330);
