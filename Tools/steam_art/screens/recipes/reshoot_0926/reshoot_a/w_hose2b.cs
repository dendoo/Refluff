UnityEditor.EditorApplication.isPaused = true;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
sim.UiPaused = false;
for (int i=0;i<3;i++) UnityEditor.EditorApplication.Step();
string P = "C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_a/";
CarpetCleaning.StoreShotApi.RenderScene(P + "hoseA.png",1920,1080);
return sim.Hint + CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + " wet " + sim.Wetness;
