// Slot 10 (18_squeegee_floor), Salor rug. Mid-job carpet: no crust / dust / spill discs (their steps are behind us), heavy grime
// (level 0.85, mud share 1) so the water stays brown after the stain work. Hose the whole rug.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_salor"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(17, 0.85f, 0f, 1f, 0f, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.04f,0.04f, 0.96f,0.96f, 9, 40, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(30);
return CarpetCleaning.CarpetDebugApi.HandoverJson().Substring(90) + CarpetCleaning.CarpetDebugApi.DirtJson();
