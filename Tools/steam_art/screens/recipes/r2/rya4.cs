float[] du = {0.08f,0.22f,0.36f};
for (int k=0;k<2;k++) for (int i=0;i<du.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.96f, du[i],0.04f, 90, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.04f, du[i],0.96f, 90, 1f); }
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.10f,-0.08f, -0.10f,1.10f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.22f,1.10f, -0.22f,-0.08f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.10f,1.10f, -0.10f,-0.08f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.0f,1.07f, 0.6f,1.07f, 50, 1f);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
CarpetCleaning.StoreShotApi.RenderScene("test/rya4.png",1280,720);
return sim.Hint + " " + CarpetCleaning.CarpetDebugApi.StatsJson() + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
