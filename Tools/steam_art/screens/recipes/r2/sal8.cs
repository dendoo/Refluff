float[] lu = {0.93f,0.82f,0.71f,0.60f};
for (int k=0;k<2;k++) for (int i=0;i<lu.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(1, lu[i],0.97f, lu[i],0.03f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(1, lu[i],0.03f, lu[i],0.97f, 60, 1f); }
CarpetCleaning.StoreShotApi.RenderScene("test/sal8.png",1280,720);
return CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
