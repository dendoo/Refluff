// ACES re-shoot 01 (round 1): hose + brush the scraped band (base reshoot_0926/reshoot_a/w_band1). Changed: 7 hose lanes
// u 0.32..0.68 (was 6 lanes 0.34..0.64) and brush lanes u 0.33..0.67 (was 0.345..0.655), v 0.97 <-> 0.31 (was 0.96 <-> 0.33),
// so the whole scraped width (crust edge u ~0.25 / ~0.75, near end v 0.28) is washed, with the hose disc (r 40 mm) staying
// ~4 cm inside the crust edge (its runoff hydrates the cake beside the band). No debug render (the old file wrote test/band1.png).
UnityEditor.EditorApplication.isPaused = true;
const float HP = 0.6f;   // hose pressure (round 1 A/B: 1.0 pooled 36 % of the carpet and ran a wide dark wet halo over the crust)
float[] hu = {0.32f,0.38f,0.44f,0.50f,0.56f,0.62f,0.68f};
for (int i=0;i<hu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.97f, hu[i],0.31f, 45, HP); else CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.31f, hu[i],0.97f, 45, HP); }
float[] bu = {0.33f,0.41f,0.50f,0.59f,0.67f};
for (int k=0;k<2;k++) for (int i=0;i<bu.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.97f, bu[i],0.31f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.31f, bu[i],0.97f, 50, 1f); }
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return sim.Hint + " " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
