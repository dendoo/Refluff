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
// Chain S0: the Anatolian star (take A left it on the stage) with Reset(394, 0.9f) = the landed rug of #04 (same K_Init,
// clean floor); hero kit set by payoff/kit.inc; LiveTuning on (Reset switches ToolTuning to Neutral).
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
var H0 = CarpetCleaning.CarpetDebugApi.Handover;
if (H0.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
CarpetCleaning.CarpetDebugApi.Reset(394, 0.9f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(0);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
var hj = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = hj.IndexOf("picName");
return hj.Substring(k, 36) + " " + CarpetCleaning.CarpetDebugApi.DirtJson();
