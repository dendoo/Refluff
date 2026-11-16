// a last amber streak on the far-left tile (beyond v 1.17): wet-vac zigzag over u -0.75..0.05, v 1.10..1.55
CarpetCleaning.CarpetDebugApi.Zigzag(6, -0.75f,1.10f, 0.05f,1.55f, 8, 60, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
