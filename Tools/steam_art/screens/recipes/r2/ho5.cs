var ok = CarpetCleaning.CarpetDebugApi.TakeNewCarpetNow();
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 70; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return ok + CarpetCleaning.CarpetDebugApi.HandoverJson();
