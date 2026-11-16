CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
float[] du = {0.53f,0.66f,0.10f,0.24f,0.37f};
for (int k=0;k<2;k++) for (int i=0;i<du.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.97f, du[i],0.03f, 80, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.03f, du[i],0.97f, 80, 1f); }
return CarpetCleaning.CarpetDebugApi.FloorStatsJson() + " wet " + CarpetCleaning.CarpetDebugApi.Sim.Wetness;
