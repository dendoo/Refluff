UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.EconomyDebugApi.PayoutResume();
CarpetCleaning.EconomyDebugApi.SetOwned(0, true);
CarpetCleaning.EconomyDebugApi.SetOwned(1, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var E = CarpetCleaning.EconomyDebugApi.Eco;
CarpetCleaning.EconomyDebugApi.Grant(1960 - E.Data.money);
E.Data.carpetsDelivered = 6; E.Data.lifetimeEarned = 1325; E.Data.perfectClean = 6; E.Data.perfectDry = 6; E.Data.combos = 6;
int[] pw = {3,4,2,2,3,2,3,4,1,1};
int[] ar = {2,3,1,2,2,2,2,3,1,1};
for (int t=0;t<10;t++){ CarpetCleaning.EconomyDebugApi.SetLevel(t,0,pw[t]); CarpetCleaning.EconomyDebugApi.SetLevel(t,1,ar[t]); }
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
return "paused=" + UnityEditor.EditorApplication.isPaused + " shop=" + CarpetCleaning.EconomyDebugApi.ShopJson().Substring(0,15) + " " + CarpetCleaning.CarpetDebugApi.HighlightJson().Substring(0,120) + " head=" + CarpetCleaning.CarpetDebugApi.ToolHeadJson().Substring(0,60) + " eco=" + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0,420);
