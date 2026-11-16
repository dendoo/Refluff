// ACES re-shoot 01 (round 1, new): the last stain rims after w_stain2: the band's near edge v 0.278-0.30 (stain 0.15-0.5, a
// dark line along the cut right beside the wand), the top-left corner (u 0.27-0.35, v 0.80-0.98) and the spill disc's inner
// half (u 0.66-0.72, v 0.42-0.54). Foam + slow brush on each, then a 4 s soak.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.28f,0.30f, 0.72f,0.30f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.72f,0.295f, 0.28f,0.295f, 130, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.28f,0.30f, 0.72f,0.30f, 130, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.30f,0.98f, 0.30f,0.79f, 50, 1f);
float[] tu = {0.27f,0.31f,0.35f,0.29f};
for (int i=0;i<tu.Length;i++){ if((i&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, tu[i],0.98f, tu[i],0.79f, 100, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, tu[i],0.79f, tu[i],0.98f, 100, 1f); }
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.70f,0.55f, 0.70f,0.41f, 40, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.71f,0.41f, 0.71f,0.55f, 90, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.68f,0.55f, 0.68f,0.41f, 90, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return CarpetCleaning.CarpetDebugApi.StatsJson();
