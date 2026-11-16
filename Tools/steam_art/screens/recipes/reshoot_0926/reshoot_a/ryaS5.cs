UnityEditor.EditorApplication.isPaused = true;
float[] bu = {0.04f,0.095f,0.15f,0.205f,0.26f,0.315f,0.37f,0.425f,0.48f};
for (int i=0;i<bu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.97f, bu[i],0.04f, 100, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.04f, bu[i],0.97f, 100, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(240);
CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt", "C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_a/dirt5.png");
return CarpetCleaning.CarpetDebugApi.StatsJson();
