// the finished squeegee lane B, diagonal toward the near-right (heading -33 deg, blade 317 mm), pushed over the right rim at
// ~700 mm/s; its pour runs out onto the tiles
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.432f,0.801f, 1.092f,0.452f, 81, 1f);
return CarpetCleaning.CarpetDebugApi.PlowSpillJson();
