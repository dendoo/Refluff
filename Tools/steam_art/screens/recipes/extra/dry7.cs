var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sim = CarpetCleaning.CarpetDebugApi.Sim; var eco = CarpetCleaning.EconomyDebugApi.Eco;
var fi = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", bf);
object st = fi.GetValue(sim);
foreach (var n in new[]{"crust","mud","stain","grit"}) { var f = st.GetType().GetField(n); f.SetValue(st, (float)f.GetValue(st) * 12f); }
fi.SetValue(sim, st);
typeof(CarpetCleaning.Economy).GetField("_clockAnchor", bf).SetValue(eco, sim.SimTime - 131.3f);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 15; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
var cr = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
cr.SetCursor(new UnityEngine.Vector2(0.46f, 0.50f), new UnityEngine.Vector2(0f, 1f), 135f, 0f, (CarpetCleaning.ToolId)7, true);
var r = CarpetCleaning.StoreShotApi.Shot("07_dryer_finish");
return CarpetCleaning.CarpetDebugApi.StatsJson() + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 260);
