UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 6 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(14, 0f);
CarpetCleaning.CarpetDebugApi.SetTool(7);
for (int i = 0; i < 30; i++) UnityEditor.EditorApplication.Step();
return CarpetCleaning.CarpetDebugApi.HandoverJson() + " hint=" + CarpetCleaning.CarpetDebugApi.Sim.Hint + " " + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0,300);
