float[] du = {0.10f,0.24f,0.37f};
for (int k=0;k<3;k++) for (int i=0;i<du.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.97f, du[i],0.03f, 80, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.03f, du[i],0.97f, 80, 1f); }
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.StoreShotApi.RenderScene("test/st5.png",1920,1080);
return CarpetCleaning.CarpetDebugApi.FiberProbeJson(0.05f,0.1f,0.4f,0.9f) + CarpetCleaning.CarpetDebugApi.FiberProbeJson(0.6f,0.1f,0.95f,0.9f);
