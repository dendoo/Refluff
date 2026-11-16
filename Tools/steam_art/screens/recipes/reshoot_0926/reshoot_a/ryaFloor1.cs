UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var A = typeof(CarpetCleaning.CarpetDebugApi);
float[] tv = {1.03f,1.10f,1.17f,1.24f};
for (int i=0;i<tv.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(8, -0.22f,tv[i], 0.86f,tv[i], 55, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(8, 0.86f,tv[i], -0.22f,tv[i], 55, 1f); }
float[] bv = {-0.03f,-0.10f,-0.17f,-0.23f};
for (int i=0;i<bv.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(8, -0.26f,bv[i], 0.62f,bv[i], 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(8, 0.62f,bv[i], -0.26f,bv[i], 50, 1f); }
float[] lu = {-0.05f,-0.12f,-0.19f,-0.26f,-0.33f};
for (int i=0;i<lu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(8, lu[i],-0.25f, lu[i],1.26f, 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(8, lu[i],1.26f, lu[i],-0.25f, 70, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(30);
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
