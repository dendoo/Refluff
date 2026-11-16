// the mop's damp film along the far-left lanes glowed blue under Z: the dryer over it (floor tools act on the tile too)
for (int r = 0; r < 3; r++) for (int k = 0; k < 4; k++) CarpetCleaning.CarpetDebugApi.StrokeNow(7, 0.04f, 1.08f + 0.05f * k, -1.02f, 1.08f + 0.05f * k, 90, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(7, -0.02f, 1.30f, -0.02f, 1.02f, 40, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(120);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
