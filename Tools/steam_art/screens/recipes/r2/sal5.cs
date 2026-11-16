float[,] L = { {0.18f,0.99f,0.80f},{0.31f,0.99f,0.80f},{0.44f,0.99f,0.80f},{0.55f,0.99f,0.80f},
  {0.85f,0.65f,0.42f},{0.95f,0.65f,0.42f},{0.06f,0.48f,0.26f},{0.18f,0.48f,0.26f},
  {0.50f,0.27f,0.01f},{0.62f,0.27f,0.01f},{0.73f,0.27f,0.01f} };
for (int p=0;p<3;p++) for (int i=0;i<L.GetLength(0);i++){ float u=L[i,0]+(p==1?0.04f:0f); if((p&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(0, u,L[i,1], u,L[i,2], 22, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(0, u,L[i,2], u,L[i,1], 22, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(120);
return "ok";
