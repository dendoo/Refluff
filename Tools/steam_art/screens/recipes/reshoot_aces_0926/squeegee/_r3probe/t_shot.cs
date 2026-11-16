// THE SHOT: head pinned where lane C stopped (its heading = the lane's, from the real Steps), stats read synchronously,
// sim held (UiPaused) while 15 real Steps settle the HUD bars, in-game pointer parked off the floor (no cursor ring), Shot,
// everything restored in finally. The job clock is set to 5:10 (par 8:04 for this level-1 carpet): the scripted staging
// spends minutes of sim time on dryer / flush / mop passes a player would not need in that order.
UnityEditor.EditorApplication.isPaused = true;
string NAME = "t_sq3";   // TEST variant of r3_u_shot (not the store file)
var sim = CarpetCleaning.CarpetDebugApi.Sim;
var tc = CarpetCleaning.CarpetDebugApi.Tools;
var eco = CarpetCleaning.EconomyDebugApi.Eco;
typeof(CarpetCleaning.Economy).GetField("_clockAnchor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(eco, sim.SimTime - 310f);
// (test variant: the pin set by the lane script stays)
sim.RefreshStatsSync();
string pre = " pre clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " hint=" + sim.Hint;
bool sp0 = tc.ScriptedPointer; var uv0 = tc.ScriptedUv;
string r = "", hud = "";
sim.UiPaused = true;
try {
  tc.ScriptedPointer = true; tc.ScriptedUv = new UnityEngine.Vector2(-20f, -20f);
  for (int i = 0; i < 15; i++) UnityEditor.EditorApplication.Step();
  r = CarpetCleaning.StoreShotApi.Shot(NAME);
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
