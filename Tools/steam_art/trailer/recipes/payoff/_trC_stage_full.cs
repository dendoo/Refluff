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
// Take C staging (#15 hand-over, #16 pay slip, #17 shop; one continuous HUD take): payoff kit ($1,840, 6 delivered, hero
// levels: brush POWER 2 so the #17 buy costs $75), the spotless dry Anatolian star at rest, dryer selected, LiveTuning off
// (factory tools: nothing acts in this take). Prepend payoff/kit.inc.
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.HandoverSnap();
CarpetCleaning.CarpetDebugApi.Reset(394, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(false);
CarpetCleaning.CarpetDebugApi.SetTool(7);
var E = CarpetCleaning.EconomyDebugApi.Eco;
CarpetCleaning.EconomyDebugApi.Grant(1840 - E.Data.money);
E.Data.carpetsDelivered = 6;
for (int i = 0; i < 30; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = true;
var hj = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = hj.IndexOf("picName");
return hj.Substring(k, 36) + " money " + E.Data.money + " delivered " + E.Data.carpetsDelivered;
