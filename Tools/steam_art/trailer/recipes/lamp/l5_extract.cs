float[] wu = {0.10f,0.22f,0.34f};
for (int r = 0; r < 4; r++) for (int i = 0; i < wu.Length; i++) { if (((i + r) & 1) == 0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.95f, wu[i],0.28f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.28f, wu[i],0.95f, 60, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson();
