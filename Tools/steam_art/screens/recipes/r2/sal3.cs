for (int p=0;p<2;p++) for (int b=0;b<3;b++){ float v0 = 0.02f + b*0.33f, v1 = v0 + 0.33f; if (v1 > 0.98f) v1 = 0.98f;
  for (int i=0;i<8;i++){ float u = 0.07f + i*0.123f + (p==0?0.06f:0f); if (u>0.97f) u=0.97f; CarpetCleaning.CarpetDebugApi.StrokeNow(0, u,v0, u,v1, 22, 1f); } }
CarpetCleaning.CarpetDebugApi.RunFrames(150);
return "ok";
