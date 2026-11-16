// clip1_reveal: the 01 hero dirt = Reset(394, 0.9f) (crust-over-grime), pre-kit tools (eco_hero), then a look at the raw rug.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Reset(394, 0.9f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(6);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt", "SteamStore/_work/test/c1_dirt_0.png");
CarpetCleaning.StoreShotApi.RenderScene("test/c1_raw.png", 1920, 1080);
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.HandoverJson();
