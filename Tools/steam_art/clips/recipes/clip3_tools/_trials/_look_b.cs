// clip3 diagnostic: dirt variant b -> SteamStore/_work/test/c3_look_b.png + crust map
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Reset(242, 0.9f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
CarpetCleaning.CarpetDebugApi.SaveDebugView("crust", "SteamStore/_work/test/c3_crust_b.png");
CarpetCleaning.StoreShotApi.RenderScene("test/c3_look_b.png", 1920, 1080);
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
