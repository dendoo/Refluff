UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.88f,0.97f, 0.88f,0.30f, 50, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.88f,0.30f, 0.88f,0.97f, 50, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.88f,0.97f, 0.88f,0.30f, 50, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(6, 0.88f, 0.30f, true);
for (int i=0;i<25;i++) UnityEditor.EditorApplication.Step();
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " wet " + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
