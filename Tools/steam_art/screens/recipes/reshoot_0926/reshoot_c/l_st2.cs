UnityEditor.EditorApplication.isPaused = true;
float[] lu = {0.08f,0.24f,0.40f,0.56f,0.72f};
for (int k=0;k<2;k++) for (int i=0;i<lu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, lu[i],0.97f, lu[i],0.03f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, lu[i],0.03f, lu[i],0.97f, 60, 1f); }
return "ok";
