CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 6 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(14, 0f);
CarpetCleaning.CarpetDebugApi.SetTool(7);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 30; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
var r = CarpetCleaning.StoreShotApi.Shot("08_spotless_handover");
return CarpetCleaning.CarpetDebugApi.HandoverJson() + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0,300);
