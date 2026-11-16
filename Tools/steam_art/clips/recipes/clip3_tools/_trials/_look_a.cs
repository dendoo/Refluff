// clip3 diagnostic: dirt variant a -> SteamStore/_work/test/c3_look_a.png + crust map
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Reset(154, 0.6f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
CarpetCleaning.CarpetDebugApi.SaveDebugView("crust", "SteamStore/_work/test/c3_crust_a.png");
CarpetCleaning.StoreShotApi.RenderScene("test/c3_look_a.png", 1920, 1080);
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
