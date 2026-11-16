UnityEditor.EditorApplication.isPaused = true;
for (int k=0;k<4;k++){ if((k&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(7, 0.50f,0.96f, 0.50f,0.04f, 80, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(7, 0.50f,0.04f, 0.50f,0.96f, 80, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson();
