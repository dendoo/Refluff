UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.585f,0.95f, 0.585f,0.06f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.585f,0.95f, 0.585f,0.10f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.60f,0.10f, 0.60f,0.80f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.58f,0.80f, 0.58f,0.30f, 50, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.58f, 0.30f, true);
for (int i=0;i<20;i++) UnityEditor.EditorApplication.Step();
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
