// ACES re-shoot 01 (round 1, new): second dry scraper pass, inner lanes offset by half a lane (u 0.35..0.67) plus both outer
// lanes again (the blade's ends bite less, so the crust ramp at the band's edges needs the outer lanes on every pass).
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
float[] su = {0.69f,0.67f,0.59f,0.51f,0.43f,0.35f,0.31f};
for (int i=0;i<su.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],0.28f, su[i],1.01f, 150, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(0, su[i],1.01f, su[i],0.28f, 150, 1f); }
return sw.ElapsedMilliseconds + " ms " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
