CarpetCleaning.CarpetDebugApi.UnpinToolHead();
int[] seeds = {36, 29, 27};
float[] lv = {0.5f, 0.5f, 0.55f};
var sb = new System.Text.StringBuilder();
for (int k = 0; k < 3; k++) {
  CarpetCleaning.CarpetDebugApi.Reset(seeds[k], lv[k]);
  CarpetCleaning.CarpetDebugApi.RunFrames(2);
  CarpetCleaning.StoreShotApi.RenderScene("test/crS" + k + ".png", 960, 540);
  sb.Append(CarpetCleaning.CarpetDebugApi.DirtJson());
}
return sb.ToString();
