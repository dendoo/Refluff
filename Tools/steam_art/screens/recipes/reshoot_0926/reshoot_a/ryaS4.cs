UnityEditor.EditorApplication.isPaused = true;
float[] wu = {0.08f,0.245f,0.41f};
for (int i=0;i<wu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.97f, wu[i],0.03f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.03f, wu[i],0.97f, 60, 1f); }
float[] fu = {0.06f,0.14f,0.22f,0.30f,0.38f,0.46f};
for (int i=0;i<fu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(3, fu[i],0.97f, fu[i],0.04f, 120, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(3, fu[i],0.04f, fu[i],0.97f, 120, 1f); }
CarpetCleaning.CarpetDebugApi.SaveDebugView("soap", "C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_a/soap5.png");
return CarpetCleaning.CarpetDebugApi.StatsJson();
