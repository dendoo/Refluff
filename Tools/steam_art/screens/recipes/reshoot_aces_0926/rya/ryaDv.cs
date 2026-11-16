// ACES re-shoot 05 (round 2): dry-vac the scraped part (scout_dirt.md 5.2 step 4): lifts the scraper's grit crumbs and sucks
// the chunks. Disc r 66 mm (PRE2) = 0.055 u. Round 1 stopped at lane u 0.47 (reach 0.525), which left scraper chunks under the
// band's ends; they poked out of the foam's far fringe (art director round 1). Now lanes u 0.03..0.655 (reach ~0.71, the
// scraped zone ends at the crust edge u 0.715), v 1.00 <-> 0.00 (reach 0.044 v past both rims: the chunks lying on the tile
// just off the band's ends too). The dry vac never lifts crust; nothing right of ~0.71 is touched, so the whole dirty part
// keeps its plates and whatever crumbs landed on it.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] a = {0.03f,0.09f,0.15f,0.21f,0.27f,0.33f,0.39f,0.45f,0.51f,0.57f};
float[] b = {0.06f,0.12f,0.18f,0.24f,0.30f,0.36f,0.42f,0.48f,0.54f,0.60f,0.63f,0.655f};
for (int i=0;i<a.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(1, a[i],1.00f, a[i],0.00f, 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(1, a[i],0.00f, a[i],1.00f, 70, 1f); }
for (int i=0;i<b.Length;i++){ if((i&1)==1) CarpetCleaning.CarpetDebugApi.StrokeNow(1, b[i],1.00f, b[i],0.00f, 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(1, b[i],0.00f, b[i],1.00f, 70, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
