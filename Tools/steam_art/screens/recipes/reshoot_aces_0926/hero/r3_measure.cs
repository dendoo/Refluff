// ACES re-shoot 01, ROUND 3 (diagnostic, not part of the take): dirt layers of the carpet into SteamStore/_work/test/r3_dirt.png
// (R crust, G mud, B stain, A grit; 384x480) for the band means in hero3 measure.py; fibre state of band / flank.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt", "SteamStore/_work/test/r3_dirt.png");
return CarpetCleaning.CarpetDebugApi.StatsJson() + " band " + CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.36f,0.40f,0.64f,0.88f) + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
