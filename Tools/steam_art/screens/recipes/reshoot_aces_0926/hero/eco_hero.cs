UnityEditor.EditorApplication.isPaused = true;
var E = CarpetCleaning.EconomyDebugApi.Eco;
CarpetCleaning.EconomyDebugApi.Grant(1960 - E.Data.money);
int[] pw = {1,2,1,1,2,0,1,0,0,0};
int[] ar = {1,1,0,1,1,1,1,0,0,0};
for (int t=0;t<10;t++){ CarpetCleaning.EconomyDebugApi.SetLevel(t,0,pw[t]); CarpetCleaning.EconomyDebugApi.SetLevel(t,1,ar[t]); }
CarpetCleaning.EconomyDebugApi.SetOwned(0, true);
CarpetCleaning.EconomyDebugApi.SetOwned(1, true);
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.StoreShotApi.MarketingHud(true);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
return CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 120) + " " + CarpetCleaning.CarpetDebugApi.HandoverJson();
