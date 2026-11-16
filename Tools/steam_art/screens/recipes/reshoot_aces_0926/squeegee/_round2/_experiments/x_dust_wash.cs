// EXPERIMENT (dust path): stain-free dusty carpet (seed 22, level 0.24, dust + grime mix, no crust / spill), hosed
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(22, 0.24f, 0f, 1f, 1f, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.05f,0.05f, 0.95f,0.95f, 11, 36, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(30);
return "ok";
