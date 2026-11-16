// the patch's straight edge at u ~0.43 (just past the last hose lane) glowed as a rectangle under X: one more lane there
CarpetCleaning.CarpetDebugApi.StrokeNow(2, 0.43f,0.92f, 0.43f,0.30f, 45, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.43f,0.30f, 0.43f,0.92f, 50, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(4, 0.44f,0.92f, 0.44f,0.30f, 50, 1f);
for (int r = 0; r < 3; r++) { CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.44f,0.95f, 0.44f,0.28f, 60, 1f); CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.44f,0.28f, 0.44f,0.95f, 60, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson();
