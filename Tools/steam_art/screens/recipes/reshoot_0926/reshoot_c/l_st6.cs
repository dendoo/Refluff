UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(7, 0.53f,0.97f, 0.53f,0.36f, 80, 1f);
CarpetCleaning.CarpetDebugApi.SetTool(7);
for (int i=0;i<20;i++) UnityEditor.EditorApplication.Step();
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " wet " + sim.Wetness;
