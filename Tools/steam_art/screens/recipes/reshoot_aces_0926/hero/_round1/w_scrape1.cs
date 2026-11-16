// ACES re-shoot 01 (round 1, new; scout_dirt.md 5.1 step 3): scrape the band DRY before any water. Blade 144 mm = 0.12 u at the
// pre-kit area level (ToolTuning.Effective(Scraper).lengthMm), horizontal on these vertical strokes: lanes u 0.31..0.69 put the
// crust edge at u ~0.25 / ~0.75. Far end past the rim (v 1.01: the band cuts through the far border, chunks flung off it land on
// the far tile that w_band3shot mops); near end v 0.28 = where the wet-vac wand is pinned, so the cut ends crisply under it.
// Slow: 150 stamps per 1095 mm run (~440 mm/s), so the chunks land within ~2-17 cm (in the band = vacuumed by w_dv1, or on the
// crust right beside it). One pass per eval; w_scrape2 offsets the inner lanes, w_scrape3 repeats this one.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] su = {0.31f,0.39f,0.47f,0.55f,0.63f,0.69f};
for (int i=0;i<su.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],1.01f, su[i],0.28f, 150, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],0.28f, su[i],1.01f, 150, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
