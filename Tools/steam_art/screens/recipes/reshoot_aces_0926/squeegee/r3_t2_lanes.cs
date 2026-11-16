// the finished squeegee lane B, diagonal toward the near-right (heading -33.46 deg, blade 317 mm), pushed over the right rim at
// ~700 mm/s; its pour runs out onto the tiles.
// ROUND 4: the start moved back 639 mm along the SAME heading, from mid-rug (0.432, 0.801) to (-0.0122, 1.0359) just off the
// rug's top-left corner (every point of the blade >= 30 mm outside the rim), so the blade touches down outside the film and
// no film strip with a straight start edge is left on the medallion. Same end point; 81 -> 136 frames keeps ~700 mm/s.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(5, -0.0122f,1.0359f, 1.092f,0.452f, 136, 1f);
return CarpetCleaning.CarpetDebugApi.PlowSpillJson();
