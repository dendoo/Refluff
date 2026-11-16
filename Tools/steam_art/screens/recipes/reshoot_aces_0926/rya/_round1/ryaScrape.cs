// ACES re-shoot 05: scrape the part that gets cleaned AND the strip under the foam band while it is still DRY
// (scout_dirt.md 5.2). Blade 173 mm (PRE2 area level); its ends bite less, so the crust edge (50 %) lands at ~ the
// outermost lane + 0.07 u. Lanes u 0.04..0.60 here, 0.00..0.56 + 0.63 in ryaScrape2 -> crust edge at u ~0.70 = the foam
// band's right edge. Why the whole band: crust keeps sprayed foam on top while bare pile soaks it in, so an edge (or seed
// 367's grime islands) under the band split it into mounds with dark steps; on uniform scraped pile it is one smooth mound.
// Lanes run past both rims (v 1.01 <-> -0.01). Slow (150 stamps, ~610 mm/s): chunks land close. One pass per eval.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] su = {0.04f,0.12f,0.20f,0.28f,0.36f,0.44f,0.52f,0.60f,0.645f};
for (int i=0;i<su.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],1.01f, su[i],-0.01f, 150, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],-0.01f, su[i],1.01f, 150, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
