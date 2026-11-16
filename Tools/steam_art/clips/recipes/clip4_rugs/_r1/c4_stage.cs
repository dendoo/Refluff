// clip4_rugs staging (fast calls + 10 warm-up Steps): PRE2 mid-game kit (as screenshot 07 / reshoot_ho a_pre), lamps off,
// head unpinned, shop closed, card dismissed; the SPOTLESS Transylvanian medallion rug on the stage (Reset(14, 0f): clean
// and dry, so the hand-over is a real Spotless + Bone dry one with the full gold + white sparkle burst), dryer selected;
// the Anatolian star moved to the front of CarpetHandover's picture bag, so TakeNewCarpet brings it next.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.EconomyDebugApi.PayoutResume();
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
int[] pw = {3,4,2,2,3,2,3,4,1,1};
int[] ar = {2,3,1,2,2,2,2,3,1,1};
for (int t=0;t<10;t++){ CarpetCleaning.EconomyDebugApi.SetLevel(t,0,pw[t]); CarpetCleaning.EconomyDebugApi.SetLevel(t,1,ar[t]); }
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.StoreShotApi.MarketingHud(true);
CarpetCleaning.CarpetDebugApi.Sim.UiPaused = false;
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.HandoverSnap();
CarpetCleaning.CarpetDebugApi.Reset(14, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(7);
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var bag = (System.Collections.Generic.List<int>)typeof(CarpetCleaning.CarpetHandover).GetField("_picBag", bf).GetValue(H);
int ai = System.Array.FindIndex(H.pictures, p => p != null && p.name == "rug_anatolian_star");
bag.Remove(ai); bag.Insert(0, ai);
for (int i = 0; i < 10; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = true;
return CarpetCleaning.CarpetDebugApi.HandoverJson() + " bag=" + string.Join(",", bag) + " " + CarpetCleaning.CarpetDebugApi.StatsJson().Substring(0, 160);
