var r = "";
foreach (var sd in new int[]{118, 148, 130}) {
  CarpetCleaning.CarpetDebugApi.Reset(sd, 0.8f);
  CarpetCleaning.CarpetDebugApi.RunFrames(2);
  r += CarpetCleaning.StoreShotApi.RenderView("keyart/test3/ov_" + sd + ".png", 800, 600, 0f, 1.75f, -1.05f, 60f, 0f, 0f, 42f);
}
return r;
