// EXPERIMENT: 5 s idle
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.RunFrames(300);
return "ok";
