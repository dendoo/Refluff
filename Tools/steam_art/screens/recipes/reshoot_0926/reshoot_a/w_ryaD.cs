UnityEditor.EditorApplication.isPaused = true;
float[] du = {0.09f,0.24f,0.38f};
for (int k=0;k<3;k++) for (int i=0;i<du.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.96f, du[i],0.04f, 80, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.04f, du[i],0.96f, 80, 1f); }
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.10f,-0.08f, -0.10f,1.10f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.22f,1.10f, -0.22f,-0.08f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.10f,1.10f, -0.10f,-0.08f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.05f,1.07f, 0.6f,1.07f, 50, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.6f,-0.07f, -0.05f,-0.07f, 50, 1f);
CarpetCleaning.StoreShotApi.RenderScene("test/ryaD.png",1280,720);
return CarpetCleaning.CarpetDebugApi.FiberProbeJson(0.05f,0.1f,0.45f,0.9f) + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
