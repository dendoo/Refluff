UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.14f,0.992f, 0.53f,0.992f, 90, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.53f,0.995f, 0.14f,0.995f, 130, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.14f,0.99f, 0.53f,0.99f, 130, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt", "C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_a/dirt7.png");
return CarpetCleaning.CarpetDebugApi.StatsJson();
