// ACES re-shoot 05: dry-vac ONLY the scraped half (scout_dirt.md 5.2 step 4): lifts the scraper's grit crumbs and sucks the
// chunks inside it. Disc r 66 mm (PRE2): lanes u 0.03..0.45 then offset 0.06..0.47, v 1.00 <-> 0.00. Nothing right of
// u ~0.525, so the crumbs on the dark scraped edge / the crust (under the foam band) and the whole dirty part stay.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] a = {0.03f,0.09f,0.15f,0.21f,0.27f,0.33f,0.39f,0.45f};
float[] b = {0.06f,0.12f,0.18f,0.24f,0.30f,0.36f,0.42f,0.47f};
for (int i=0;i<a.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(1, a[i],1.00f, a[i],0.00f, 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(1, a[i],0.00f, a[i],1.00f, 70, 1f); }
for (int i=0;i<b.Length;i++){ if((i&1)==1) CarpetCleaning.CarpetDebugApi.StrokeNow(1, b[i],1.00f, b[i],0.00f, 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(1, b[i],0.00f, b[i],1.00f, 70, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
