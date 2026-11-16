// cut D staging (mop1 seed): the Transylvanian with mud 0.58 and no crust, hosed along the left border so brown runoff lies
// on the tile beside it; factory tools (no LiveTuning), as mop1 / mop4 were shot
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(20, 0.6f);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.03f,0.25f, 0.35f,0.75f, 6, 30, 1f);
CarpetCleaning.CarpetDebugApi.Hold(2, 0.04f,0.40f, 1.5f);
CarpetCleaning.CarpetDebugApi.Hold(2, 0.04f,0.60f, 1.5f);
CarpetCleaning.CarpetDebugApi.RunFrames(120);
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
