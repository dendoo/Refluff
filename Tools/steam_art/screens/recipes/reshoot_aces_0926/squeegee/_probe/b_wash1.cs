// Salor rug, washed-down mid-job carpet: no crust / dust / spills (the scrape, dry-vac and stain steps are done), grime only.
// Hose the whole rug.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_salor"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(17, 0.6f, 0f, -1f, 0f, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.04f,0.04f, 0.96f,0.96f, 9, 40, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(30);
return CarpetCleaning.CarpetDebugApi.HandoverJson().Substring(90) + CarpetCleaning.CarpetDebugApi.DirtJson() + CarpetCleaning.CarpetDebugApi.StatsJson() + " hint=" + CarpetCleaning.CarpetDebugApi.Sim.Hint;
