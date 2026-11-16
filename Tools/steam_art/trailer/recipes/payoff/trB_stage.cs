// Take B staging (#14 clean side + sparkles; still plate for #20 / #21 / #03): the Anatolian star spotless and dry
// (Reset(394, 0f): same picture + seed, dirt level 0, fibre set cleared; the floor and the debris reset too, so the tile
// matches take A's dirty plate), dryer selected (the chain's last tool), lamps off, no card, no shop.
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.EconomyDebugApi.PayoutResume();
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(394, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(7);
for (int i = 0; i < 30; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = true;
var hj = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = hj.IndexOf("picName");
return hj.Substring(k, 36) + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
