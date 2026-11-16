// ACES re-shoot 05 (round 2, new): slow DRY vacuum passes over the right part of the band strip (u 0.60-0.71, reach ~0.745)
// to lift the seed's grime there (dry vac: mud decays 0.8 x power /s on dry, crust-free pile; crust itself is never touched).
// The washing runoff cleans the strip's left part only; over unwashed grime (mud 0.6-0.9) the foam turns mauve-brown and the
// band shows a lengthwise two-tone line at u ~0.62. Hosing the strip instead is out: its runoff soaks and eats the crust
// edge beside the band. 200 stamps per lane (~0.3 s dwell per pass). Chunks inside the footprint are sucked (they would lie
// under the band anyway).
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] a = {0.62f,0.66f,0.69f,0.69f,0.66f,0.62f};
for (int i=0;i<a.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(1, a[i],1.00f, a[i],0.00f, 200, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(1, a[i],0.00f, a[i],1.00f, 200, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
