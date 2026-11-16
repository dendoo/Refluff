var sb = new System.Text.StringBuilder();
for (int i = 0; i < 5; i++) {
  CarpetCleaning.CarpetDebugApi.NewCarpetNow();
  CarpetCleaning.CarpetDebugApi.Reset(14, 0f);
  CarpetCleaning.CarpetDebugApi.RunFrames(2);
  var hj = CarpetCleaning.CarpetDebugApi.HandoverJson();
  CarpetCleaning.StoreShotApi.RenderScene("test/rug" + i + ".png", 960, 540);
  sb.Append(hj).Append(" ");
}
return sb.ToString();
