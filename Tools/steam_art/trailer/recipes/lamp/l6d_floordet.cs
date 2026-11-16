// dried runoff residue on the tile left of the rug still glowed amber under X: detergent, mop, wet vac (the game's floor chain)
float[] vu = {-0.04f, -0.12f, -0.20f, -0.28f, -0.36f, -0.44f, -0.52f};
for (int i = 0; i < vu.Length; i++) CarpetCleaning.CarpetDebugApi.StrokeNow(8, vu[i], (i & 1) == 0 ? 1.14f : -0.14f, vu[i], (i & 1) == 0 ? -0.14f : 1.14f, 70, 1f);
for (int i = 0; i < vu.Length; i++) CarpetCleaning.CarpetDebugApi.StrokeNow(9, vu[i], (i & 1) == 1 ? 1.14f : -0.14f, vu[i], (i & 1) == 1 ? -0.14f : 1.14f, 70, 1f);
for (int i = 0; i < vu.Length; i++) CarpetCleaning.CarpetDebugApi.StrokeNow(9, vu[i], (i & 1) == 0 ? 1.14f : -0.14f, vu[i], (i & 1) == 0 ? -0.14f : 1.14f, 70, 1f);
for (int i = 0; i < vu.Length; i++) CarpetCleaning.CarpetDebugApi.StrokeNow(6, vu[i], (i & 1) == 1 ? 1.14f : -0.14f, vu[i], (i & 1) == 1 ? -0.14f : 1.14f, 70, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
