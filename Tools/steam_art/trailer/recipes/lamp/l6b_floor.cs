// the washing runoff reaches ~0.5 rug widths left of the rim: mop lanes out to u -0.6, then a drying pause
float[] mu = {-0.08f, -0.19f, -0.30f, -0.41f, -0.52f, -0.63f};
for (int i = 0; i < mu.Length; i++) { if ((i & 1) == 0) CarpetCleaning.CarpetDebugApi.StrokeNow(9, mu[i],1.14f, mu[i],-0.14f, 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(9, mu[i],-0.14f, mu[i],1.14f, 70, 1f); }
for (int i = 0; i < mu.Length; i++) { if ((i & 1) == 1) CarpetCleaning.CarpetDebugApi.StrokeNow(9, mu[i],1.14f, mu[i],-0.14f, 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(9, mu[i],-0.14f, mu[i],1.14f, 70, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
