float[] hu = {0.07f,0.13f,0.19f,0.25f,0.31f,0.37f};
for (int i = 0; i < hu.Length; i++) { if ((i & 1) == 0) CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.92f, hu[i],0.30f, 45, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.30f, hu[i],0.92f, 45, 1f); }
float[] bu = {0.075f,0.15f,0.22f,0.29f,0.365f};
for (int r = 0; r < 2; r++) for (int i = 0; i < bu.Length; i++) { if (((i + r) & 1) == 0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.92f, bu[i],0.30f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.30f, bu[i],0.92f, 50, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson();
