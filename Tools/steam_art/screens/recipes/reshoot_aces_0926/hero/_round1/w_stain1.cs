// ACES re-shoot 01 (round 1, new): stain extraction in the band (the ryaS pattern of the 05 retake). Seed 242 leaves stain in
// ~18 % of the band after hose + brush (stains need soap: hose + brush alone do not move them): the band's near end right under
// the wand (v 0.28-0.35, stain 0.4-0.8), the top-left corner (u 0.26-0.46, v 0.78-0.98), a line at u 0.50-0.54 (v 0.63-0.73)
// and the (0.71, 0.48) spill disc. Foam lanes over the whole band + 2 brush rounds, then a 4 s soak.
UnityEditor.EditorApplication.isPaused = true;
float[] fu = {0.33f,0.41f,0.50f,0.59f,0.67f};
for (int i=0;i<fu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(3, fu[i],0.97f, fu[i],0.30f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(3, fu[i],0.30f, fu[i],0.97f, 60, 1f); }
float[] bu = {0.33f,0.41f,0.50f,0.59f,0.67f};
for (int k=0;k<2;k++) for (int i=0;i<bu.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.97f, bu[i],0.30f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.30f, bu[i],0.97f, 50, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return CarpetCleaning.CarpetDebugApi.StatsJson();
