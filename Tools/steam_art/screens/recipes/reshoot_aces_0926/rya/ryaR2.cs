// ACES re-shoot 05 (round 2, new): extract the rinse water (w_ryaW's three lanes x4 + ryaW2's two far-rim lanes).
UnityEditor.EditorApplication.isPaused = true;
float[] wu = {0.08f,0.245f,0.41f};
for (int k=0;k<4;k++) for (int i=0;i<wu.Length;i++){ if((k&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.97f, wu[i],0.03f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.03f, wu[i],0.97f, 60, 1f); }
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.02f,0.988f, 0.53f,0.988f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.53f,0.985f, 0.02f,0.985f, 60, 1f);
return CarpetCleaning.CarpetDebugApi.StatsJson();
