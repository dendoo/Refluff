var E = CarpetCleaning.EconomyDebugApi.Eco;
int money = E.Data.money;
CarpetCleaning.EconomyDebugApi.Grant(1960 - money);
int[] pw = {3,4,2,2,3,2,3,4,1,1};
int[] ar = {2,3,1,2,2,2,2,3,1,1};
for (int t=0;t<10;t++){ CarpetCleaning.EconomyDebugApi.SetLevel(t,0,pw[t]); CarpetCleaning.EconomyDebugApi.SetLevel(t,1,ar[t]); }
CarpetCleaning.EconomyDebugApi.SetOwned(0, true);
CarpetCleaning.EconomyDebugApi.SetOwned(1, true);
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.StoreShotApi.MarketingHud(true);
return CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 400);
