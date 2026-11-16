// EXPERIMENT: initial dirt of the level-1 grime carpet (seed 17)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(17, 1.0f, 0f, 1f, 0f, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.RunFrames(2);
return CarpetCleaning.CarpetDebugApi.StatsJson() + CarpetCleaning.CarpetDebugApi.DirtJson();
