// clip3 diagnostic: dirt variant c -> SteamStore/_work/test/c3_look_c.png + crust map
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.ResetWithProfile(242, 0.8f, 0.6f, 0.7f, 0.1f, 0.2f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
CarpetCleaning.CarpetDebugApi.SaveDebugView("crust", "SteamStore/_work/test/c3_crust_c.png");
CarpetCleaning.StoreShotApi.RenderScene("test/c3_look_c.png", 1920, 1080);
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
