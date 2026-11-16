// clip5_paid staging (fast calls + 30 warm-up Steps), after reshoot_ho e2_setup (screenshot 04 = 10_payslip): pre-kit levels,
// LiveTuning(false), both lamps owned + off, shop closed, card dismissed, head unpinned, marketing HUD; wallet $1,840 and
// carpetsDelivered 6 so the slip reads #7 and the wallet rolls $1,840 -> $1,960; the European green rug (not used by clips 1-4)
// SPOTLESS and dry on the stage (Reset(14, 0f)), dryer selected.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.EconomyDebugApi.PayoutResume();
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
int[] pw = {1,2,1,1,2,0,1,0,0,0};
int[] ar = {1,1,0,1,1,1,1,0,0,0};
for (int t=0;t<10;t++){ CarpetCleaning.EconomyDebugApi.SetLevel(t,0,pw[t]); CarpetCleaning.EconomyDebugApi.SetLevel(t,1,ar[t]); }
CarpetCleaning.EconomyDebugApi.SetOwned(0, true);
CarpetCleaning.EconomyDebugApi.SetOwned(1, true);
CarpetCleaning.StoreShotApi.MarketingHud(true);
CarpetCleaning.CarpetDebugApi.Sim.UiPaused = false;
var E = CarpetCleaning.EconomyDebugApi.Eco;
CarpetCleaning.EconomyDebugApi.Grant(1840 - E.Data.money);
E.Data.carpetsDelivered = 6;
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_european_green"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.HandoverSnap();
CarpetCleaning.CarpetDebugApi.Reset(14, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(false);
CarpetCleaning.CarpetDebugApi.SetTool(7);
var T = CarpetCleaning.CarpetDebugApi.Tools;
try {
  // pointer off the play area during the warm-up Steps: the paused Steps must not read the OS mouse (e4b_shot note)
  T.ScriptedPointer = true; T.ScriptedDown = false; T.ScriptedRight = false; T.ScriptedUv = new UnityEngine.Vector2(-5f, -5f);
  for (int i = 0; i < 30; i++) UnityEditor.EditorApplication.Step();
} finally { T.ScriptedPointer = false; }
UnityEditor.EditorApplication.isPaused = true;
return CarpetCleaning.CarpetDebugApi.HandoverJson() + " money=" + E.Data.money + " carpets=" + E.Data.carpetsDelivered + " payout=" + CarpetCleaning.EconomyDebugApi.PayoutJson().Substring(0, 40);
