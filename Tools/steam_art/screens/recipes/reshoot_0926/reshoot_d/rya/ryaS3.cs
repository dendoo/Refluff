UnityEditor.EditorApplication.isPaused = true;
float[] bu = {0.20f,0.25f,0.30f,0.35f,0.40f,0.45f,0.49f};
for (int i=0;i<bu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.97f, bu[i],0.70f, 100, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.70f, bu[i],0.97f, 100, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson();
