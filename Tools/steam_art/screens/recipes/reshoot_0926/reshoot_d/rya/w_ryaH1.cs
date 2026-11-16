UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.62f,0.06f, 0.62f,0.95f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.63f,0.95f, 0.63f,0.12f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.60f,0.55f, 0.655f,0.22f, 50, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.655f, 0.22f, true);
for (int i=0;i<20;i++) UnityEditor.EditorApplication.Step();
string P = "C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_a/";
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " " + CarpetCleaning.CarpetDebugApi.StatsJson() + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
