// ACES re-shoot 05 (round 2, new): wash the scraped strip under the coming foam band (u 0.50-0.71) like the clean half
// (w_rya2's hose + brush), so the pile under the band is one material. Unwashed, it keeps the seed's grime (mud 0.6-0.9 at
// u 0.62-0.71) while the washed left part of the strip is clean: the foam dissolves that grime and turns mauve-brown on the
// band's right half only, a lengthwise two-tone line. Hose lanes u 0.52/0.58/0.64 (disc r 44 mm), brush u 0.52/0.59/0.66
// (r 54 mm, reach 0.705 < the crust edge 0.715) x2. The runoff that reaches the crust only darkens it (wet cake).
UnityEditor.EditorApplication.isPaused = true;
float[] hu = {0.52f,0.58f,0.64f};
for (int i=0;i<hu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.97f, hu[i],0.03f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.03f, hu[i],0.97f, 50, 1f); }
float[] bu = {0.52f,0.59f,0.66f};
for (int k=0;k<2;k++) for (int i=0;i<bu.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.97f, bu[i],0.03f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.03f, bu[i],0.97f, 50, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson();
