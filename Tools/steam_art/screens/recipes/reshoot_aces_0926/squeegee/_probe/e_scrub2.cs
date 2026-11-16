// one more scrub + soak: stains below 20 % of the start
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(4, 0.04f,0.04f, 0.96f,0.96f, 9, 50, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return "ok";
