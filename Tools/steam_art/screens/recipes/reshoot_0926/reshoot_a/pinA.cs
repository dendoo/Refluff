UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.78f, 0.15f, true);
for (int i=0;i<20;i++) UnityEditor.EditorApplication.Step();
string P = "C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_a/";
CarpetCleaning.StoreShotApi.RenderScene(P + "pinA.png",1920,1080);
return CarpetCleaning.CarpetDebugApi.ToolHeadJson();
