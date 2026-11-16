float[] lu = {0.08f,0.24f,0.40f,0.56f,0.72f};
for (int i=0;i<lu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, lu[i],0.97f, lu[i],0.03f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, lu[i],0.03f, lu[i],0.97f, 60, 1f); }
for (int i=0;i<2;i++){ CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.08f-0.11f*i,1.12f, -0.08f-0.11f*i,-0.12f, 70, 1f); CarpetCleaning.CarpetDebugApi.StrokeNow(9, 1.08f+0.11f*i,-0.12f, 1.08f+0.11f*i,1.12f, 70, 1f); }
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.15f,1.07f, 1.15f,1.07f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 1.15f,1.16f, -0.15f,1.16f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.15f,-0.07f, 1.15f,-0.07f, 70, 1f);
CarpetCleaning.StoreShotApi.RenderScene("test/st3.png",1280,720);
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
