float[] wu = {0.07f,0.19f,0.31f,0.42f};
for (int k=0;k<2;k++) for (int i=0;i<wu.Length;i++){ if((k&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.97f, wu[i],0.03f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.03f, wu[i],0.97f, 60, 1f); }
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
