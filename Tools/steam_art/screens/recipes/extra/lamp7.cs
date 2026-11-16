CarpetCleaning.CarpetDebugApi.SetHighlight(1, false);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.Reset(39, 0.3f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
var fi = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", bf);
object st = fi.GetValue(sim);
foreach (var n in new[]{"crust","mud","stain","grit"}) { var f = st.GetType().GetField(n); f.SetValue(st, (float)f.GetValue(st) * 8f); }
fi.SetValue(sim, st);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 20; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.StoreShotApi.RenderScene("test/lamp7.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.StatsJson() + CarpetCleaning.CarpetDebugApi.HighlightJson();
