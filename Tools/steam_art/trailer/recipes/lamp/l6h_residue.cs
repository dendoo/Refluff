// the far-left streak is dried residue where the mop lanes ended (v ~1.17, u -0.58..0.01, from the camera ray): detergent
// along it, then mop lanes that run AWAY from the rug to the floor domain's left edge (u -1.0), then wet-vac the film
for (int k = 0; k < 3; k++) CarpetCleaning.CarpetDebugApi.StrokeNow(8, 0.04f, 1.12f + 0.05f * k, -0.70f, 1.12f + 0.05f * k, 60, 1f);
for (int k = 0; k < 4; k++) CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.04f, 1.10f + 0.045f * k, -1.02f, 1.10f + 0.045f * k, 70, 1f);
for (int k = 0; k < 4; k++) CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.04f, 1.10f + 0.045f * k, -1.02f, 1.10f + 0.045f * k, 70, 1f);
for (int k = 0; k < 3; k++) CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.04f, 1.11f + 0.05f * k, -1.02f, 1.11f + 0.05f * k, 60, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(300);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
