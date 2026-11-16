// the mop pushes the left runoff around instead of lifting it: wet-vac lanes over the tile left of the rug, then mop the film
float[] vu = {-0.06f, -0.14f, -0.22f, -0.30f, -0.38f, -0.46f, -0.54f};
for (int r = 0; r < 2; r++) for (int i = 0; i < vu.Length; i++) { if (((i + r) & 1) == 0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, vu[i],1.14f, vu[i],-0.14f, 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, vu[i],-0.14f, vu[i],1.14f, 70, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
