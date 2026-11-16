var A = typeof(CarpetCleaning.CarpetDebugApi);
float[] hu = {0.34f,0.40f,0.46f,0.52f,0.58f,0.64f};
for (int i=0;i<hu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.96f, hu[i],0.33f, 45, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.33f, hu[i],0.96f, 45, 1f); }
float[] bu = {0.345f,0.42f,0.50f,0.58f,0.655f};
for (int k=0;k<2;k++) for (int i=0;i<bu.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.96f, bu[i],0.33f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.33f, bu[i],0.96f, 50, 1f); }
CarpetCleaning.StoreShotApi.RenderScene("test/band1.png",1280,720);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
