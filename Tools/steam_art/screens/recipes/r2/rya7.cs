CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.57f,0.95f, 0.57f,0.05f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.53f,0.05f, 0.53f,0.95f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.53f,0.95f, 0.53f,0.10f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.58f,0.10f, 0.58f,0.95f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.555f,0.95f, 0.555f,0.30f, 60, 1f);
for (int i=0;i<3;i++){ CarpetCleaning.CarpetDebugApi.StrokeNow(9, 1.06f+0.1f*i,1.12f, 1.06f+0.1f*i,-0.12f, 70, 1f); }
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.25f,-0.07f, 1.15f,-0.07f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.45f,1.07f, 1.15f,1.07f, 50, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.555f, 0.30f, true);
CarpetCleaning.StoreShotApi.RenderScene("test/rya7.png",1280,720);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return CarpetCleaning.CarpetDebugApi.StatsJson() + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
