// E1: Anatolian star, fully caked grime carpet (crust 1, mud 1), hose the whole rug
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(17, 1.0f, 1f, 1f, 0f, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.04f,0.04f, 0.96f,0.96f, 11, 36, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(30);
return "ok";
