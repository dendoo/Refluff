// let the detergent work (5 s)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.RunFrames(300);
return "ok";
