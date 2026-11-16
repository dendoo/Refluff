// ROUND 2 (lighter take). Anatolian star rug (its cream medallions and border keep the rug legible wet, and the black
// blade reads on them), mid-job carpet: no crust / dust / spill discs, grime at level 0.7 (the soap stage takes it all out of the pile: it only browns the water). Hose the whole rug.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(17, 0.7f, 0f, 1f, 0f, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.04f,0.04f, 0.96f,0.96f, 11, 36, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(30);
return CarpetCleaning.CarpetDebugApi.HandoverJson().Substring(60) + CarpetCleaning.CarpetDebugApi.DirtJson();
