// EXPERIMENT: hose top-up so the pile is saturated and a film stands right before the lanes
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.06f,0.06f, 0.94f,0.94f, 11, 36, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(60);
return "ok";
