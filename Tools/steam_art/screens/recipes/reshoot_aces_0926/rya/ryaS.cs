UnityEditor.EditorApplication.isPaused = true;
float[] fu = {0.06f,0.15f,0.24f,0.33f,0.41f};
for (int i=0;i<fu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(3, fu[i],0.95f, fu[i],0.05f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(3, fu[i],0.05f, fu[i],0.95f, 60, 1f); }
float[] bu = {0.05f,0.12f,0.19f,0.26f,0.33f,0.40f};
for (int k=0;k<2;k++) for (int i=0;i<bu.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.95f, bu[i],0.05f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.05f, bu[i],0.95f, 50, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson();
