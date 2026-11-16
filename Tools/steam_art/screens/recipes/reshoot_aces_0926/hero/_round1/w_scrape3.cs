// ACES re-shoot 01 (round 1, new): third dry scraper pass = w_scrape1 again (lanes u 0.31..0.69). Header of w_scrape1 says why.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] su = {0.31f,0.39f,0.47f,0.55f,0.63f,0.69f};
for (int i=0;i<su.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],1.01f, su[i],0.28f, 150, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],0.28f, su[i],1.01f, 150, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
