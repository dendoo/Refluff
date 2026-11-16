// THE LANE: the squeegee (AREA 2 blade 317 mm) pushed along +u through the lower medallion, stopping at the right rim:
// the lane behind it is wiped, its roll of dirty water pours over the edge onto the clean tile
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(5, -0.03f,0.30f, 0.93f,0.30f, 80, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(5, 0.93f, 0.30f, true);
return CarpetCleaning.CarpetDebugApi.PlowSpillJson() + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
