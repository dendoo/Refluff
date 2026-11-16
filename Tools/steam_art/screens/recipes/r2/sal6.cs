float[,] L = { {0.49f,0.76f,0.99f},{0.46f,0.99f,0.76f},{0.53f,0.76f,0.99f},{0.315f,0.08f,0.28f},{0.33f,0.28f,0.08f},{0.86f,0.10f,0.50f},{0.88f,0.50f,0.10f},{0.03f,0.42f,0.80f},{0.02f,0.80f,0.42f} };
for (int p=0;p<2;p++) for (int i=0;i<L.GetLength(0);i++){ CarpetCleaning.CarpetDebugApi.StrokeNow(0, L[i,1],L[i,0], L[i,2],L[i,0], 20, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(100);
return "ok";
