// ACES re-shoot 01 (art director round 1): crusty seed 242 at level 0.9 (seed route; no level 1, no ResetWithProfile).
// No whole-carpet dry-vac zigzags any more (they only moved the HUD hint; 01 is a no-HUD crop): the dirty rim keeps its
// crust plates untouched. The band is scraped dry next (w_scrape1..3), then dry-vacuumed inside the band only.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Reset(242, 0.9f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
var sb = new System.Text.StringBuilder();
foreach (int t in new[]{0,1,2,4,6,9}) { var d = CarpetCleaning.ToolTuning.Effective((CarpetCleaning.ToolId)t); sb.Append(t).Append(":r").Append(d.radiusMm).Append(" l").Append(d.lengthMm).Append(" s").Append(d.spacingMm).Append(" | "); }
return sb + CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.HandoverJson();
