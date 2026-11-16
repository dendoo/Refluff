// variant K staging (reshoot_d/hv_K.cs), then: stats sync -> sim held -> 15 real Steps (0.3 s > the 0.2 s .track__fill
// width transition) -> Shot -> sim released. Returns the HUD labels and the fills' resolved widths next to the sim values.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.RunFrames(90);
CarpetCleaning.CarpetDebugApi.StrokeNow(2, 0.36f,0.62f, 0.40f,0.42f, 12, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(12);
CarpetCleaning.CarpetDebugApi.StrokeNow(2, 0.40f,0.42f, 0.42f,0.29f, 10, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(2, 0.42f, 0.29f, true);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
sim.RefreshStatsSync();
string pre = " pre clean=" + sim.Cleanliness + " wet=" + sim.Wetness;
sim.UiPaused = true;
string r, hud = "";
try {
  for (int i=0;i<15;i++) UnityEditor.EditorApplication.Step();
  r = CarpetCleaning.StoreShotApi.Shot("03_hose_spill");
  foreach (var doc in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>()) {
    var root = doc.rootVisualElement; if (root == null) continue;
    var cf = UnityEngine.UIElements.UQueryExtensions.Q(root, "clean-fill"); var df = UnityEngine.UIElements.UQueryExtensions.Q(root, "dry-fill");
    var cv = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(root, "clean-value"); var dv = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(root, "dry-value");
    if (cf == null || df == null) continue;
    hud += " HUD clean " + cv.text + " fill " + (cf.resolvedStyle.width / cf.parent.resolvedStyle.width) + " | dry " + dv.text + " fill " + (df.resolvedStyle.width / df.parent.resolvedStyle.width);
  }
} finally { sim.UiPaused = false; }
return r.Substring(r.Length-40) + pre + hud + " hint=" + sim.Hint + " clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(100, 60);
