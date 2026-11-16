// THE SHOT: head pinned where the last stroke stopped, stats read synchronously, sim held (UiPaused) while 15 real Steps settle
// the HUD bars (0.2 s .track__fill transition), in-game pointer parked off the floor (no cursor ring), Shot, all restored.
UnityEditor.EditorApplication.isPaused = true;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
var tc = CarpetCleaning.CarpetDebugApi.Tools;
CarpetCleaning.CarpetDebugApi.PinToolHead(5, 0.93f, 0.47f, true);
sim.RefreshStatsSync();
string pre = " pre clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " hint=" + sim.Hint;
bool sp0 = tc.ScriptedPointer; var uv0 = tc.ScriptedUv;
string r = "", hud = "";
sim.UiPaused = true;
try {
  tc.ScriptedPointer = true; tc.ScriptedUv = new UnityEngine.Vector2(-20f, -20f);
  for (int i = 0; i < 15; i++) UnityEditor.EditorApplication.Step();
  r = CarpetCleaning.StoreShotApi.Shot("18_squeegee_floor");
  foreach (var doc in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>()) {
    var root = doc.rootVisualElement; if (root == null) continue;
    var cf = UnityEngine.UIElements.UQueryExtensions.Q(root, "clean-fill"); var df = UnityEngine.UIElements.UQueryExtensions.Q(root, "dry-fill");
    if (cf == null || df == null) continue;
    System.Func<string,string> L = n => { var l = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(root, n); return l != null ? l.text : "?"; };
    hud += " HUD clean " + L("clean-value") + " fill " + (cf.resolvedStyle.width / cf.parent.resolvedStyle.width) + " | dry " + L("dry-value") + " fill " + (df.resolvedStyle.width / df.parent.resolvedStyle.width)
         + " | clock " + L("clock-value") + " " + L("clock-par") + " | wallet " + L("wallet-value") + " | hint '" + L("hint-label") + "'";
  }
} finally { sim.UiPaused = false; tc.ScriptedPointer = sp0; tc.ScriptedUv = uv0; }
UnityEditor.EditorApplication.isPaused = true;
return r.Substring(Mathf.Max(0, r.Length - 60)) + pre + hud + " tool=" + tc.Current + " " + CarpetCleaning.CarpetDebugApi.ToolHeadJson().Substring(0, 40);
