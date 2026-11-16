// clip2 diagnostic: current rug, forced dusty profile, 4K render -> SteamStore/_work/test/c2_look2_<name>.png
UnityEditor.EditorApplication.isPaused = true;
var h = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = h.IndexOf("picName") + 10; string nm = h.Substring(k, h.IndexOf('"', k) - k);
CarpetCleaning.CarpetDebugApi.ResetWithProfile(12, 0.38f, 0f, 0f, 1f, 0f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
CarpetCleaning.StoreShotApi.RenderScene("test/c2_look2_" + nm + ".png", 3840, 2160);
CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt", "SteamStore/_work/test/c2_dirt.png");
return nm + " " + CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
