var ok = CarpetCleaning.CarpetDebugApi.HandoverNow(false);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 8; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
var r = CarpetCleaning.StoreShotApi.Shot("test_ho_a");
return ok + CarpetCleaning.CarpetDebugApi.HandoverJson() + CarpetCleaning.EconomyDebugApi.PayoutJson().Substring(0,200);
