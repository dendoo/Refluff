UnityEditor.EditorApplication.isPaused = true;
float[] bu = {0.20f,0.25f,0.30f,0.35f,0.40f,0.45f,0.49f};
for (int i=0;i<bu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.97f, bu[i],0.70f, 100, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.70f, bu[i],0.97f, 100, 1f); }
CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt", "C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_a/dirt4.png");
CarpetCleaning.CarpetDebugApi.SaveDebugView("soap", "C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_a/soap4.png");
return CarpetCleaning.CarpetDebugApi.StatsJson();
