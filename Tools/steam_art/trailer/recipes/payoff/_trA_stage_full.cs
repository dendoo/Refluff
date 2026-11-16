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
// Take A staging (#04 arrival): kit, a forced $0 hand-over of a spotless rug so the stage is Away (bare tile, "Take a new
// carpet"), the card killed, $1,840 / 6 delivered, the Anatolian star at the front of the picture bag.
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
CarpetCleaning.CarpetDebugApi.Reset(394, 0f);
CarpetCleaning.CarpetDebugApi.HandoverNow(true);
int n = 0;
for (; n < 150 && H.State != CarpetCleaning.HandoverState.Away; n++) UnityEditor.EditorApplication.Step();
if (H.State != CarpetCleaning.HandoverState.Away) return "ERROR not Away after " + n + " steps: " + CarpetCleaning.CarpetDebugApi.HandoverJson();
CarpetCleaning.EconomyDebugApi.Hud.Payout.Finish();
CarpetCleaning.EconomyDebugApi.PayoutResume();
var E = CarpetCleaning.EconomyDebugApi.Eco;
CarpetCleaning.EconomyDebugApi.Grant(1840 - E.Data.money);
E.Data.carpetsDelivered = 6;
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var bag = (System.Collections.Generic.List<int>)typeof(CarpetCleaning.CarpetHandover).GetField("_picBag", bf).GetValue(H);
int ai = System.Array.FindIndex(H.pictures, p => p != null && p.name == "rug_anatolian_star");
bag.Remove(ai); bag.Insert(0, ai);
for (int i = 0; i < 30; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = true;
return "away after " + n + " | money " + E.Data.money + " delivered " + E.Data.carpetsDelivered + " bag " + string.Join(",", bag) + " | " + CarpetCleaning.CarpetDebugApi.HandoverJson().Substring(0, 90);
