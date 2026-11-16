// ACES re-shoot 01 (round 1): wet vac over the band (base reshoot_0926/reshoot_a/w_band2: u 0.365 / 0.635 / 0.50 x4, nozzle
// 192 mm = 0.16 u, v 0.97 <-> 0.28) + NEW edge lanes u 0.30 / 0.70 (nozzle u 0.22-0.38 / 0.62-0.78): the scraped band is wider
// than the old lanes' reach (0.285-0.715), and its 3 cm side strips stayed soaked (w 0.5-0.6 = dark wet smears along both cuts).
// They also take the foam the brush threw onto the crust beside the band. The wet vac never touches crust. No debug render.
UnityEditor.EditorApplication.isPaused = true;
float[] wu = {0.30f,0.70f,0.365f,0.635f,0.50f};
for (int k=0;k<4;k++) for (int i=0;i<wu.Length;i++){ if((k&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.98f, wu[i],0.28f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.28f, wu[i],0.98f, 60, 1f); }
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
