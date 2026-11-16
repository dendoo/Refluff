// ACES re-shoot 05 (art director round 2): scrape the part that gets cleaned AND the strip under the foam band while it is
// still DRY (scout_dirt.md 5.2). Blade 173 mm (PRE2 area level); its ends bite less, so the crust edge (50 %) lands at ~ the
// outermost lane + 0.07 u. Lanes u 0.04..0.60 + 0.645 here, 0.00..0.56 + 0.62 in ryaScrape2 -> crust edge (50 %) at u ~0.715,
// where the foam band's flank thins out (n_foam1/2 lanes u 0.595 + 0.635: F 1.2 at 0.70, 0.2 at 0.72). Foam lying on crust
// stands on the plates instead of soaking into the pile, so an edge under THICK foam (tried: u 0.69) builds a second ridge
// with a dark violet crease between it and the main mound (the same crease round 1's extra lane n_foam3 made at u 0.655).
// Why the whole band is scraped: crust keeps
// sprayed foam on top while bare pile soaks it in, so an edge (or seed 367's grime islands) under the band splits it into
// mounds with dark steps; on uniform scraped pile it is one smooth mound.
// Lanes run past both rims (v 1.01 <-> -0.01). Slow (150 stamps, ~610 mm/s): chunks land close. One pass per eval.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] su = {0.04f,0.12f,0.20f,0.28f,0.36f,0.44f,0.52f,0.60f,0.645f};
for (int i=0;i<su.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],1.01f, su[i],-0.01f, 150, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],-0.01f, su[i],1.01f, 150, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
