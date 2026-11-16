UnityEditor.EditorApplication.isPaused = true;
float[] bu = {0.04f,0.095f,0.15f,0.205f,0.26f,0.315f,0.37f,0.425f,0.48f};
for (int i=0;i<bu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.97f, bu[i],0.04f, 100, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.04f, bu[i],0.97f, 100, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return CarpetCleaning.CarpetDebugApi.StatsJson();
