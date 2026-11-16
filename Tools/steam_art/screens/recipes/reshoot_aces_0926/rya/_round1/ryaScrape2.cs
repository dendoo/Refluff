// ACES re-shoot 05: second dry scrape pass on the lanes BETWEEN ryaScrape's (pass 1 leaves crust ~0.25 between lanes vs
// ~0.14 on them), plus u 0.63, which sets the crust edge at u ~0.70 (the foam band's right edge).
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] su = {0.00f,0.08f,0.16f,0.24f,0.32f,0.40f,0.48f,0.56f,0.62f};
for (int i=0;i<su.Length;i++){ if((i&1)==1) CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],1.01f, su[i],-0.01f, 150, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],-0.01f, su[i],1.01f, 150, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
