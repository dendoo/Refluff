// wallet continuity: #17 ends at $1,885 (1840 + 120 - 75); wet vacuum selected so the tool list matches the damp zone
var E = CarpetCleaning.EconomyDebugApi.Eco;
CarpetCleaning.EconomyDebugApi.Grant(1885 - E.Data.money);
CarpetCleaning.CarpetDebugApi.SetTool(6);
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
return "money " + E.Data.money + " " + CarpetCleaning.CarpetDebugApi.HighlightJson();
