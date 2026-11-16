// ACES re-shoot 01 (round 1, new): fourth dry scraper pass on the offset lanes of w_scrape2. After three passes the band held
// crust 0.04 with 0.06-0.07 stripes at u 0.38-0.40 / 0.46-0.48 / 0.54-0.56; leftover cake blocks the mud (access = exp(-5 crust))
// and keeps the pile matted, so the band would wash out dull.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] su = {0.31f,0.35f,0.43f,0.51f,0.59f,0.67f,0.69f};
for (int i=0;i<su.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],1.01f, su[i],0.28f, 150, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],0.28f, su[i],1.01f, 150, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
