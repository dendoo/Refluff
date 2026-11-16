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
