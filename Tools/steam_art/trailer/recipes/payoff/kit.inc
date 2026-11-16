// Payoff/chain kit (brief_payoff 5 = clip 1's eco_hero): hero pre-kit levels, both lamps owned + off, marketing HUD, $1,840,
// 6 carpets delivered, so #04 / #15-#17 agree (wallet $1,840 -> +$120 in #16 -> -$75 in #17). Writes the economy save.
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.EconomyDebugApi.PayoutResume();
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
{ int[] pwK = {1,2,1,1,2,0,1,0,0,0}; int[] arK = {1,1,0,1,1,1,1,0,0,0};
  for (int t=0;t<10;t++){ CarpetCleaning.EconomyDebugApi.SetLevel(t,0,pwK[t]); CarpetCleaning.EconomyDebugApi.SetLevel(t,1,arK[t]); } }
CarpetCleaning.EconomyDebugApi.SetOwned(0, true);
CarpetCleaning.EconomyDebugApi.SetOwned(1, true);
CarpetCleaning.StoreShotApi.MarketingHud(true);
CarpetCleaning.CarpetDebugApi.Sim.UiPaused = false;
