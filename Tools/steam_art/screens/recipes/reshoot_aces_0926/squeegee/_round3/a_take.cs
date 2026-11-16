// dismiss a pay slip / carpet-away state left by an earlier take (d1_take pattern), stays paused
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.PayoutResume();
var s0 = CarpetCleaning.CarpetDebugApi.HandoverJson();
bool ok = false;
if (s0.Contains("\"Away\"")) ok = CarpetCleaning.CarpetDebugApi.TakeNewCarpetNow();
for (int i = 0; i < 70; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = true;
return s0 + " take=" + ok + " " + CarpetCleaning.CarpetDebugApi.HandoverJson();
