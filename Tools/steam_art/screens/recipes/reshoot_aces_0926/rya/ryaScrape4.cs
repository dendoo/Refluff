// ACES re-shoot 05 (round 2, new): clean out the crust RAMP at the scraped zone's right edge. After ryaScrape..3 only the
// outermost lanes reach u 0.66-0.715, so crust 0.04-0.17 stays there (then a sharp step to the intact cake at u ~0.715).
// That thin crust keeps the pile matted (K_Fiber fTarget = 0.6 crustCover -> f 0.15-0.4 vs 0.05 on scraped pile), the lower
// pile top pulls foam into a second ridge, and the band shows a dark violet crease at u ~0.66 (it was there in round 1 too,
// with or without the extra lane n_foam3). Four more passes on the outermost lane u 0.645 + two on u 0.625 scrape the ramp
// to the level of the rest (< 0.01), so the band's pile is uniform out to the crust edge at u 0.715, where the foam's flank
// ends. Slow strokes as before (chunks land within ~2-17 cm: under the band or on the crust right beside it).
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] su = {0.645f,0.625f,0.645f,0.645f,0.625f,0.645f};
for (int i=0;i<su.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],1.01f, su[i],-0.01f, 150, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],-0.01f, su[i],1.01f, 150, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
