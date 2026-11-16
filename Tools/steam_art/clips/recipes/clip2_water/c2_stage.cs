// clip2_water staging: the Finnish rya, a DRY carpet with a dust-only soil (seed 68's soil field at level 0.36, forced mix:
// crust 0, mud 0, dust 1, spill 0 -> grit ~0.3 over the near field, a grey film, no grime, no stains to speak of),
// pre-kit tools (c2_eco must have run), hose selected, head unpinned.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_finnish_rya"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(68, 0.36f, 0f, 0f, 1f, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(2);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.HandoverJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
