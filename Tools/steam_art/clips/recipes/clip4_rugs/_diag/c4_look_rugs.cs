// clip4 diagnostic: every rug spotless (Reset(14, 0f)) -> SteamStore/_work/test/c4_clean_<rug>.png (1280x720)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
string[] rugs = { "rug_anatolian_star", "rug_european_green", "rug_finnish_rya", "rug_salor_turkmen", "rug_transylvanian_medallion" };
var sb = new System.Text.StringBuilder();
foreach (var r in rugs)
{
    for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains(r); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
    CarpetCleaning.CarpetDebugApi.Reset(14, 0f);
    CarpetCleaning.CarpetDebugApi.RunFrames(3);
    sb.Append(r).Append(": ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/c4_clean_" + r + ".png", 1280, 720).Substring(0, 60)).Append(" | ");
}
return sb.ToString();
