CarpetCleaning.EconomyDebugApi.PayoutResume();
var ok = CarpetCleaning.CarpetDebugApi.TakeNewCarpetNow();
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 45; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return ok + CarpetCleaning.CarpetDebugApi.HandoverJson() + CarpetCleaning.CarpetDebugApi.DirtJson();
