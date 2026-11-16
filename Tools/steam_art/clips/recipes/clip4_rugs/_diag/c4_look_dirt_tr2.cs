// clip4 r2 diagnostic: more dirt variants on the Transylvanian medallion -> SteamStore/_work/test/c4_trdirt_<k>.png
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
var sb = new System.Text.StringBuilder();
System.Action<string> shot = k => { CarpetCleaning.CarpetDebugApi.RunFrames(3); CarpetCleaning.StoreShotApi.RenderScene("test/c4_trdirt_" + k + ".png", 1280, 720); sb.Append(k).Append(' ').Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" | "); };
CarpetCleaning.CarpetDebugApi.ResetWithProfile(14, 0.95f, 0.30f); shot("p14c30");
CarpetCleaning.CarpetDebugApi.ResetWithProfile(16, 0.95f, 0.30f, 0.45f); shot("p16c30");
int[] seeds = { 21, 22, 23, 24 };
foreach (var s in seeds) { CarpetCleaning.CarpetDebugApi.Reset(s, 0.95f); shot("s" + s); }
return sb.ToString();
