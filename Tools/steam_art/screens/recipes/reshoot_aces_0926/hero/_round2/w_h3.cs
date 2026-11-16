// ACES re-shoot 01 (art director rounds 1 + 2): crusty seed 242 at level 0.9 (seed route; no level 1, no ResetWithProfile).
// No whole-carpet dry-vac zigzags any more (they only moved the HUD hint; 01 is a no-HUD crop): the dirty rim keeps its
// crust plates untouched. Next: r2_scrape1..4 (dry), r2_dv (the scraped outline only), then the wash chain (see run.sh).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Reset(242, 0.9f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
var sb = new System.Text.StringBuilder();
foreach (int t in new[]{0,1,2,4,6,9}) { var d = CarpetCleaning.ToolTuning.Effective((CarpetCleaning.ToolId)t); sb.Append(t).Append(":r").Append(d.radiusMm).Append(" l").Append(d.lengthMm).Append(" s").Append(d.spacingMm).Append(" | "); }
return sb + CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.HandoverJson();
