// clip3_tools: economy kit = PRE2 (the mid-game kit of store screenshot 03 scraper): power {3,4,2,2,3,2,3,4,1,1},
// area {2,3,1,2,2,2,2,3,1,1}, $1,960, lamps owned + off, shop closed, marketing HUD, head unpinned, UiPaused off.
UnityEditor.EditorApplication.isPaused = true;
var E = CarpetCleaning.EconomyDebugApi.Eco;
CarpetCleaning.EconomyDebugApi.Grant(1960 - E.Data.money);
int[] pw = {3,4,2,2,3,2,3,4,1,1};
int[] ar = {2,3,1,2,2,2,2,3,1,1};
for (int t=0;t<10;t++){ CarpetCleaning.EconomyDebugApi.SetLevel(t,0,pw[t]); CarpetCleaning.EconomyDebugApi.SetLevel(t,1,ar[t]); }
CarpetCleaning.EconomyDebugApi.SetOwned(0, true);
CarpetCleaning.EconomyDebugApi.SetOwned(1, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.StoreShotApi.MarketingHud(true);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.Sim.UiPaused = false;
return CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 160) + " " + CarpetCleaning.CarpetDebugApi.HandoverJson();
