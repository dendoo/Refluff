float[] wu = {0.08f,0.245f,0.41f};
for (int k=0;k<4;k++) for (int i=0;i<wu.Length;i++){ if((k&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.97f, wu[i],0.03f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.03f, wu[i],0.97f, 60, 1f); }
return "ok";
