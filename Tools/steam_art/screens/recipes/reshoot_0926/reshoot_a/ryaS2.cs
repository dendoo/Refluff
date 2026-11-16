UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.47f,0.05f, 0.47f,0.95f, 60, 1f);
float[] bu = {0.04f,0.09f,0.14f,0.19f,0.24f,0.29f,0.34f,0.39f,0.44f,0.48f};
for (int i=0;i<bu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.95f, bu[i],0.05f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.05f, bu[i],0.95f, 50, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(240);
CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt", "C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_a/dirt3.png");
return CarpetCleaning.CarpetDebugApi.StatsJson();
