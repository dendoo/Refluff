UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.14f,0.975f, 0.53f,0.975f, 90, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.53f,0.982f, 0.14f,0.982f, 110, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.14f,0.965f, 0.53f,0.965f, 110, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt", "C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_a/dirt6.png");
return CarpetCleaning.CarpetDebugApi.StatsJson();
