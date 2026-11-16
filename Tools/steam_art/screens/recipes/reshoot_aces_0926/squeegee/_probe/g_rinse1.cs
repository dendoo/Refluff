// hose the soap and foam down: the foam collapses into brown slurry, water stands on the pile again
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.04f,0.96f, 0.96f,0.04f, 9, 40, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(30);
return "ok";
