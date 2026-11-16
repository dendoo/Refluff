UnityEditor.EditorApplication.isPaused = true;
float[] hu = {0.05f,0.11f,0.17f,0.23f,0.29f,0.35f,0.41f};
for (int i=0;i<hu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.96f, hu[i],0.04f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.04f, hu[i],0.96f, 50, 1f); }
float[] bu = {0.05f,0.12f,0.19f,0.26f,0.33f,0.40f};
for (int k=0;k<2;k++) for (int i=0;i<bu.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.96f, bu[i],0.04f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.04f, bu[i],0.96f, 50, 1f); }
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
