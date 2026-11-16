// ACES re-shoot 05 (art director round 1): crusty seed 367 at level 0.9 (seed route), no whole-carpet dry-vac passes
// (05 is a no-HUD crop: the dirty right part keeps its crust plates untouched).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.Reset(367, 0.9f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
var ci = System.Globalization.CultureInfo.InvariantCulture;
string g = "";
foreach (var t in new[]{ CarpetCleaning.ToolId.Scraper, CarpetCleaning.ToolId.DryVacuum, CarpetCleaning.ToolId.Hose, CarpetCleaning.ToolId.FoamSprayer, CarpetCleaning.ToolId.Brush, CarpetCleaning.ToolId.WetVacuum, CarpetCleaning.ToolId.Dryer }) {
  var d = CarpetCleaning.ToolTuning.Effective(t);
  g += t + " r" + d.radiusMm.ToString("F0", ci) + " l" + d.lengthMm.ToString("F0", ci) + " p" + CarpetCleaning.ToolTuning.Power(t).ToString("F2", ci) + "; ";
}
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + g + CarpetCleaning.CarpetDebugApi.HandoverJson();
