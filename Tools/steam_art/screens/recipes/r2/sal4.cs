float[] lu = {0.12f,0.235f,0.35f};
for (int k=0;k<2;k++) for (int i=0;i<lu.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(1, lu[i],0.96f, lu[i],0.04f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(1, lu[i],0.04f, lu[i],0.96f, 60, 1f); }
CarpetCleaning.CarpetDebugApi.StrokeNow(1, 0.465f,0.96f, 0.465f,0.34f, 45, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(1, 0.465f, 0.34f, true);
CarpetCleaning.StoreShotApi.RenderScene("test/sal4.png",1280,720);
return CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
