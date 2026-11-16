// clip3_tools staging: fresh carpet of seed 154 at level 0.6 (crust 0.53: dried-mud cake over the lower right quadrant, as in
// store screenshot 03), live tool tuning (c3_eco kit), scraper selected. Needs c3_eco + c3_pick first.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.Reset(154, 0.6f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(0);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.HandoverJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
