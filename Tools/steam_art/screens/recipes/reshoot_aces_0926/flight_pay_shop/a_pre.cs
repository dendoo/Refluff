UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
int[] pw = {3,4,2,2,3,2,3,4,1,1};
int[] ar = {2,3,1,2,2,2,2,3,1,1};
for (int t=0;t<10;t++){ CarpetCleaning.EconomyDebugApi.SetLevel(t,0,pw[t]); CarpetCleaning.EconomyDebugApi.SetLevel(t,1,ar[t]); }
CarpetCleaning.EconomyDebugApi.SetOwned(0, true);
CarpetCleaning.EconomyDebugApi.SetOwned(1, true);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.StoreShotApi.MarketingHud(true);
var E = CarpetCleaning.EconomyDebugApi.Eco;
CarpetCleaning.EconomyDebugApi.Grant(2040 - E.Data.money);
for (int i = 0; i < 6 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
return CarpetCleaning.CarpetDebugApi.HandoverJson() + " money=" + E.Data.money + " carpets=" + E.Data.carpetsDelivered;
