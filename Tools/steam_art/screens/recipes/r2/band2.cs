float[] wu = {0.365f,0.635f,0.50f};
for (int k=0;k<4;k++) for (int i=0;i<wu.Length;i++){ if((k&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.97f, wu[i],0.28f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.28f, wu[i],0.97f, 60, 1f); }
CarpetCleaning.StoreShotApi.RenderScene("test/band2.png",1280,720);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
