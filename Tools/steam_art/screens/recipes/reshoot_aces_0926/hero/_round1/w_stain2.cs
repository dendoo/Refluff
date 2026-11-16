// ACES re-shoot 01 (round 1, new): slow brush on the stains that w_stain1 left (dirt view, stain channel): the near end under
// the wand (v 0.30-0.36, u 0.30-0.74; horizontal strokes), the top-left corner (u 0.28-0.44, v 0.78-0.98), the (0.71, 0.48)
// spill disc and the centre line u 0.52 (v 0.60-0.76), then another 4 s soak in the foam that is still lying on the band.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.30f,0.325f, 0.72f,0.325f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.72f,0.31f, 0.28f,0.31f, 120, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.28f,0.345f, 0.72f,0.345f, 120, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.72f,0.31f, 0.28f,0.31f, 120, 1f);
float[] tu = {0.29f,0.34f,0.39f,0.44f};
for (int i=0;i<tu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, tu[i],0.98f, tu[i],0.77f, 100, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, tu[i],0.77f, tu[i],0.98f, 100, 1f); }
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.69f,0.55f, 0.69f,0.42f, 90, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.70f,0.42f, 0.70f,0.55f, 90, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.52f,0.77f, 0.52f,0.58f, 90, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return CarpetCleaning.CarpetDebugApi.StatsJson();
