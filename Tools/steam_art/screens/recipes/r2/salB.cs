CarpetCleaning.CarpetDebugApi.StrokeNow(0, 0.18f,0.875f, 0.40f,0.875f, 22, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(0, 0.40f,0.86f, 0.18f,0.86f, 22, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(0, 0.18f,0.89f, 0.40f,0.89f, 22, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(90);
float[] lu = {0.88f,0.73f,0.58f};
for (int k=0;k<2;k++) for (int i=0;i<lu.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(1, lu[i],0.97f, lu[i],0.03f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(1, lu[i],0.03f, lu[i],0.97f, 60, 1f); }
CarpetCleaning.StoreShotApi.RenderScene("test/salB.png",1280,720);
return "ok";
