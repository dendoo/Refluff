// clip1 diagnostic: dirt map -> SteamStore/_work/test/c1_dirt.png (+ fluid, soap), and the band's surface load.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt", "SteamStore/_work/test/c1_dirt.png");
CarpetCleaning.CarpetDebugApi.SaveDebugView("soap", "SteamStore/_work/test/c1_soap.png");
return "band " + CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.2f,0.25f,0.8f,0.46f);
