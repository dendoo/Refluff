// clip4 diagnostic: dirt variants on the Anatolian star (the incoming rug) -> SteamStore/_work/test/c4_dirt_<k>.png (1280x720)
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
var sb = new System.Text.StringBuilder();
System.Action<string> shot = k => { CarpetCleaning.CarpetDebugApi.RunFrames(3); CarpetCleaning.StoreShotApi.RenderScene("test/c4_dirt_" + k + ".png", 1280, 720); sb.Append(k).Append(' ').Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" | "); };
CarpetCleaning.CarpetDebugApi.Reset(165, 0.9f); shot("a165");
CarpetCleaning.CarpetDebugApi.Reset(242, 0.9f); shot("b242");
CarpetCleaning.CarpetDebugApi.ResetWithProfile(242, 0.95f, 0.35f, 1f, 0.5f, 0.5f); shot("c242mix");
CarpetCleaning.CarpetDebugApi.Reset(14, 1f); shot("d_full");
CarpetCleaning.CarpetDebugApi.ResetWithProfile(165, 0.95f, 0.3f, 1f, 0.6f, 0.6f); shot("e165mix");
CarpetCleaning.CarpetDebugApi.Reset(15, 0.95f); shot("f15");
return sb.ToString();
