var ok = CarpetCleaning.CarpetDebugApi.HandoverNow(false);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 12; i++) UnityEditor.EditorApplication.Step();
CarpetCleaning.EconomyDebugApi.CaptureHud("test/fly_a.png", 2560, 1440);
UnityEditor.EditorApplication.isPaused = false;
return ok + CarpetCleaning.CarpetDebugApi.HandoverJson() + CarpetCleaning.EconomyDebugApi.PayoutJson();
