// ACES re-shoot 01, ROUND 2: mop the far tile (base w_band3). The scraper strokes that end at the outline's far end (v 0.93-0.97)
// fling their last chunks forward over the rim onto the tile, up to ~0.2 v (300 mm) out; the store crop shows that tile.
// Three mop strokes across it (blade 320 mm across the stroke), v 1.05 / 1.13 / 1.21, u 0.12 <-> 0.88.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.12f,1.05f, 0.88f,1.05f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.88f,1.13f, 0.12f,1.13f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.12f,1.21f, 0.88f,1.21f, 60, 1f);
return CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + " " + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
