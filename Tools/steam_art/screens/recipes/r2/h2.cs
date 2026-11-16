CarpetCleaning.CarpetDebugApi.Zigzag(4, 0.04f,0.04f,0.96f,0.96f, 15, 50, 1f);
CarpetCleaning.StoreShotApi.RenderScene("test/h2.png",1280,720);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
