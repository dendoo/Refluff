UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_finnish_rya"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
return CarpetCleaning.CarpetDebugApi.HandoverJson();
