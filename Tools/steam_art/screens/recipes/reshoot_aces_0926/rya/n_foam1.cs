// ACES re-shoot 05 (round 2): the old retake's two sprayer lanes (u 0.585 + 0.62, 70 stamps) moved right by 0.01-0.015 to
// u 0.595 + 0.635, so the band's right flank covers the crust edge (u ~0.69) without round 1's third lane n_foam3 (whose
// crease split the band). n_foam1 and n_foam2 are identical: run BOTH (two passes each).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.595f,0.95f, 0.595f,0.06f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.635f,0.06f, 0.635f,0.95f, 70, 1f);
return CarpetCleaning.CarpetDebugApi.StatsJson();
