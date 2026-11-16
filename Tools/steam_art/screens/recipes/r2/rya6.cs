float[] hu = {0.62f,0.68f,0.74f,0.80f,0.86f,0.92f};
for (int i=0;i<hu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.95f, hu[i],0.05f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.05f, hu[i],0.95f, 50, 1f); }
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.55f,0.95f, 0.55f,0.05f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.55f,0.05f, 0.55f,0.95f, 60, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(20);
CarpetCleaning.StoreShotApi.RenderScene("test/rya6.png",1280,720);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " wet " + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
