// ACES re-shoot 01 (round 1, new; scout_dirt.md 5.1 step 4): dry-vac the scraped band ONLY (replaces the old whole-carpet
// w_dvpass/w_dvpass2 zigzags): lifts the scraper's grit crumbs and sucks the chunks lying in the band. Disc r 55 mm = 0.046 u
// (pre-kit area level): lanes u 0.29..0.71 reach 0.245..0.755 = the scraped width (crust edge u ~0.25 / ~0.75); v 1.02 <-> 0.30
// (the disc reaches the band's near end at v ~0.265 and a little past the far rim). The dry vac never lifts crust, so
// nothing outside the band changes: the rim keeps its plates and every crumb that landed on it. Two passes.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] a = {0.29f,0.35f,0.41f,0.47f,0.53f,0.59f,0.65f,0.71f};
for (int k=0;k<2;k++) for (int i=0;i<a.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(1, a[i],1.02f, a[i],0.30f, 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(1, a[i],0.30f, a[i],1.02f, 70, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
