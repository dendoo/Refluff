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
// #13 retake base: the Anatolian star back on the stage (NewCarpetNow until its picture; that resets the sim), then the
// pre-#13 GPU snapshot (common/snap_save.cs taken right before the tr13 take = #12's end state: band damp w ~0.25,
// pressed + striped). Hero kit, LiveTuning on, lamps off. Debris is not in the snapshot (the right tile loses #06's crumbs).
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
var snap = System.AppDomain.CurrentDomain.GetData("sq3snap") as System.Collections.Generic.Dictionary<string, object>;
if (snap == null) return "ERROR no snapshot";
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
int n = 0;
foreach (var o in new object[]{ CarpetCleaning.CarpetDebugApi.Sim, CarpetCleaning.CarpetDebugApi.Floor }) {
  foreach (var f in o.GetType().GetFields(bf)) {
    string key = o.GetType().Name + "." + f.Name;
    if (!snap.TryGetValue(key, out var v)) continue;
    if (v is UnityEngine.RenderTexture src && f.GetValue(o) is UnityEngine.RenderTexture dst) { UnityEngine.Graphics.CopyTexture(src, dst); n++; }
    else if (v is UnityEngine.RenderTexture[] sa && f.GetValue(o) is UnityEngine.RenderTexture[] da) { for (int i = 0; i < sa.Length && i < da.Length; i++) if (sa[i] != null && da[i] != null) { UnityEngine.Graphics.CopyTexture(sa[i], da[i]); n++; } }
    else if (v is int || v is float) f.SetValue(o, v);
  }
}
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
CarpetCleaning.CarpetDebugApi.SetTool(7);
CarpetCleaning.CarpetDebugApi.RunFrames(2);
var hj = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = hj.IndexOf("picName");
return "restored " + n + " RTs " + hj.Substring(k, 36) + " | " + CarpetCleaning.CarpetDebugApi.FiberProbeJson(0.20f, 0.33f, 0.60f, 0.39f);
