// ACES re-shoot 05 (round 2, new): extract the strip under the coming foam band before it is dried. The washing water of the
// clean half runs right onto the scraped strip u 0.50-0.66, which no wet-vac lane reached (w_ryaW stops at u 0.41 + the
// 115 mm half bar); n_dry then dried that slurry back into grime (mud 0.01 -> 0.6 at u 0.51-0.60) and left a brown seam at
// the band's left edge. Two lanes x2 (bar 231 mm, PRE2): u 0.53 + 0.60 reach u 0.43-0.70. The wet vac never touches crust.
UnityEditor.EditorApplication.isPaused = true;
float[] wu = {0.53f,0.60f};
for (int k=0;k<2;k++) for (int i=0;i<wu.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.97f, wu[i],0.03f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.03f, wu[i],0.97f, 60, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.50f,0.05f,0.66f,0.95f);
