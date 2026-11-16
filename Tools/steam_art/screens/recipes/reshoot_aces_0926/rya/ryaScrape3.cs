// ACES re-shoot 05 (round 2): third dry scrape pass = ryaScrape's lanes (outermost u 0.645) in the opposite directions.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] su = {0.04f,0.12f,0.20f,0.28f,0.36f,0.44f,0.52f,0.60f,0.645f};
for (int i=0;i<su.Length;i++){ if((i&1)==1) CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],1.01f, su[i],-0.01f, 150, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],-0.01f, su[i],1.01f, 150, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
