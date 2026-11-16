// clip2 diagnostic: every rug with the same forced dusty profile, full-frame 1920 renders -> SteamStore/_work/test/c2_rug_<name>.png
UnityEditor.EditorApplication.isPaused = true;
var sb = new System.Text.StringBuilder();
for (int i = 0; i < 5; i++) {
  CarpetCleaning.CarpetDebugApi.NewCarpetNow();
  var h = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = h.IndexOf("picName") + 10; string nm = h.Substring(k, h.IndexOf('"', k) - k);
  CarpetCleaning.CarpetDebugApi.ResetWithProfile(SEED, LEVEL, 0f, MUD, 1f, 0f);
  CarpetCleaning.CarpetDebugApi.RunFrames(3);
  CarpetCleaning.StoreShotApi.RenderScene("test/c2_rug_" + nm + ".png", 1920, 1080);
  sb.Append(nm).Append(" | ");
}
return sb.ToString() + CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
