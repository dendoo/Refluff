// ACES re-shoot 01 (round 1, new): the stains along the band's side edges (u 0.25-0.29 / 0.71-0.75: the scraped strip between
// the outermost brush lane's reach and the crust edge): left v 0.34-0.56 (stain 0.2-0.33), right v 0.44-0.53, the near-right
// corner (0.72, 0.29) and the far-right corner (0.73, 0.97). Foam + slow brush on each, 4 s soak. The brush also abrades wet
// crust, so these strokes stay on the scraped side (the crust edge is at u ~0.25 / ~0.75).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.27f,0.57f, 0.27f,0.33f, 40, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.275f,0.33f, 0.275f,0.57f, 90, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.275f,0.57f, 0.275f,0.33f, 90, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.73f,0.54f, 0.73f,0.43f, 25, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.725f,0.43f, 0.725f,0.54f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.64f,0.295f, 0.73f,0.295f, 20, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.73f,0.30f, 0.64f,0.30f, 50, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.73f,0.92f, 0.73f,0.99f, 15, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.725f,0.99f, 0.725f,0.91f, 50, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
return CarpetCleaning.CarpetDebugApi.StatsJson();
