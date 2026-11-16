for (int b=0;b<3;b++){ float v0 = 0.98f - b*0.33f, v1 = v0 - 0.33f; if (v1 < 0.02f) v1 = 0.02f;
  for (int i=0;i<8;i++){ float u = 0.07f + i*0.123f; CarpetCleaning.CarpetDebugApi.StrokeNow(0, u,v0, u,v1, 22, 1f); } }
CarpetCleaning.CarpetDebugApi.RunFrames(150);
CarpetCleaning.StoreShotApi.RenderScene("test/sal2.png",1280,720);
return "ok";
