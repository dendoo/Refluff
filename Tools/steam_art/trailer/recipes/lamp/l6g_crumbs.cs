// the streak on the far-left tile is a line of crumbs the mop lanes pushed to their ends (the lamp lights chunks too): dry vac
var d0 = CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
CarpetCleaning.CarpetDebugApi.Zigzag(1, -0.75f,1.10f, 0.05f,1.55f, 10, 60, 1f);
CarpetCleaning.CarpetDebugApi.Zigzag(1, -0.75f,-0.25f, 0.0f,1.10f, 12, 60, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(60);
return d0 + " -> " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
