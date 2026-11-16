CarpetCleaning.EconomyDebugApi.PayoutResume();
var s0 = CarpetCleaning.CarpetDebugApi.HandoverJson();
var ok = CarpetCleaning.CarpetDebugApi.TakeNewCarpetNow();
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 70; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = true;
return s0 + " take=" + ok + " " + CarpetCleaning.CarpetDebugApi.HandoverJson();
