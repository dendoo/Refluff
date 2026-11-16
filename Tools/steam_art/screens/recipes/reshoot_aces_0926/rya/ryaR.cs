// ACES re-shoot 05 (round 2, new): clear-water RINSE of the clean half after the extraction (ryaW2). The pile still holds
// dirty water after the wet vac, and w_ryaD's drying lays that slurry back down as grime (mud ~0.02 over the whole clean half,
// ~6 % darker than the spotless rug under ACES). Hose lanes as in w_rya2 (+ u 0.46; no brush, no soap) dilute it; ryaR2 +
// ryaW3 extract. Do NOT hose the band strip (u > 0.5): its runoff soaks the crust edge and eats the cake beside the band.
UnityEditor.EditorApplication.isPaused = true;
float[] hu = {0.05f,0.11f,0.17f,0.23f,0.29f,0.35f,0.41f,0.46f};
for (int i=0;i<hu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.97f, hu[i],0.03f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.03f, hu[i],0.97f, 50, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(60);
return CarpetCleaning.CarpetDebugApi.StatsJson();
