UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 60; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return CarpetCleaning.CarpetDebugApi.HandoverJson() + CarpetCleaning.EconomyDebugApi.PayoutJson().Substring(0,300);
