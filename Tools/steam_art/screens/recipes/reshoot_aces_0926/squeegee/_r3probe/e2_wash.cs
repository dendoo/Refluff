// E2: grime carpet level 1 (no crust), one hose pass over the whole rug
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(17, 1.0f, 0f, 1f, 0f, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.04f,0.04f, 0.96f,0.96f, 11, 36, 1f);
return "ok";
