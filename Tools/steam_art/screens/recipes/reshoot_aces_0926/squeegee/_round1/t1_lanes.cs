// squeegee (AREA 2 blade 317 mm): two finished lanes along +u on the near half, each pushed right off the rug so its roll
// pours onto the tile beside the right edge; a short wait between them lets the floor puddle spread
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(5, -0.03f,0.105f, 1.05f,0.105f, 100, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(40);
CarpetCleaning.CarpetDebugApi.StrokeNow(5, -0.03f,0.29f, 1.05f,0.29f, 100, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(40);
return CarpetCleaning.CarpetDebugApi.PlowSpillJson() + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
