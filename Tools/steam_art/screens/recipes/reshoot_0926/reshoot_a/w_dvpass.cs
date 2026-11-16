UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
CarpetCleaning.CarpetDebugApi.Zigzag(1, 0.03f,0.03f,0.97f,0.97f, 17, 50, 1f);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
var s = sim.RefreshStatsSync();
return sw.ElapsedMilliseconds + "ms grit " + s.grit + " " + sim.Hint;
