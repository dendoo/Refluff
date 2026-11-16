var ok = CarpetCleaning.CarpetDebugApi.HandoverNow(false);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 3; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
var r = CarpetCleaning.StoreShotApi.Shot("14_handover_flight");
return ok + " " + r + CarpetCleaning.CarpetDebugApi.HandoverJson() + CarpetCleaning.CarpetDebugApi.Sim.Hint + CarpetCleaning.EconomyDebugApi.PayoutJson().Substring(0,300);
