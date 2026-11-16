UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.StoreShotApi.MarketingHud(true);
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var sb = new System.Text.StringBuilder();
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_european"); i++) { CarpetCleaning.CarpetDebugApi.NewCarpetNow(); var h = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = h.IndexOf("picName"); sb.Append(h.Substring(k, 40)).Append(" | "); }
return sb.ToString() + CarpetCleaning.CarpetDebugApi.HandoverJson();
