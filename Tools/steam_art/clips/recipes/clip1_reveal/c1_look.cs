// clip1 diagnostic: full-frame render + dirt / fluid / soap maps of the current state (SteamStore/_work/test/c1_*).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt", "SteamStore/_work/test/c1_dirt.png");
CarpetCleaning.CarpetDebugApi.SaveDebugView("crust", "SteamStore/_work/test/c1_crust.png");
CarpetCleaning.CarpetDebugApi.SaveDebugView("fluid", "SteamStore/_work/test/c1_fluid.png");
CarpetCleaning.StoreShotApi.RenderScene("test/c1_look.png", 3840, 2160);
return CarpetCleaning.CarpetDebugApi.StatsJson() + " band " + CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.2f,0.25f,0.8f,0.46f) + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
