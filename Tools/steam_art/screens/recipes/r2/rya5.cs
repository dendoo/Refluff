float[] du = {0.09f,0.24f,0.38f};
for (int k=0;k<3;k++) for (int i=0;i<du.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.96f, du[i],0.04f, 80, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.04f, du[i],0.96f, 80, 1f); }
return CarpetCleaning.CarpetDebugApi.FiberProbeJson(0.3f,0.75f,0.45f,0.95f) + CarpetCleaning.CarpetDebugApi.FiberProbeJson(0.05f,0.1f,0.25f,0.4f);
