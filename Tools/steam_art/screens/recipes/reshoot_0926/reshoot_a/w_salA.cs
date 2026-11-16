UnityEditor.EditorApplication.isPaused = true;
float[,] L = { {0.33f,0.995f,0.82f},{0.44f,0.995f,0.82f},{0.60f,0.19f,0.005f},{0.71f,0.19f,0.005f} };
for (int p=0;p<3;p++) for (int i=0;i<L.GetLength(0);i++){ float u=L[i,0]+(p==1?0.05f:0f); if((p&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(0, u,L[i,1], u,L[i,2], 22, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(0, u,L[i,2], u,L[i,1], 22, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(120);
CarpetCleaning.StoreShotApi.RenderScene("test/salA.png",1280,720);
return "ok";
