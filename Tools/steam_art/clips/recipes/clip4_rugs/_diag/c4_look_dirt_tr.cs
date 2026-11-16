// clip4 r2 diagnostic: dirt variants on the Transylvanian medallion (segment B's incoming rug) -> SteamStore/_work/test/c4_trdirt_<k>.png
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
var sb = new System.Text.StringBuilder();
System.Action<string> shot = k => { CarpetCleaning.CarpetDebugApi.RunFrames(3); CarpetCleaning.StoreShotApi.RenderScene("test/c4_trdirt_" + k + ".png", 1280, 720); sb.Append(k).Append(' ').Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" | "); };
int[] seeds = { 16, 15, 14, 17, 18, 19 };
foreach (var s in seeds) { CarpetCleaning.CarpetDebugApi.Reset(s, 0.95f); shot("s" + s); }
return sb.ToString();
