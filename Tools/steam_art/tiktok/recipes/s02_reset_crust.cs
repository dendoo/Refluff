// T7 retake: the star with crust 0.3 (scattered dried-mud slabs, the pattern still shows), same mud / dust as s01.
CarpetCleaning.CarpetDebugApi.ResetWithProfile(394, 0.75f, 0.3f, 0.45f, 0.85f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(0);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
