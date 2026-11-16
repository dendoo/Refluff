// THE LANE in progress, parallel to B (heading -33.45 deg): stops with the blade's upper end at the right rim, the roll of brown
// water ahead of it just starting to pour over the edge while its lower half still rides on the rug (stopping at u 0.94
// instead poured enough to stand as a glassy wall at the rim).
// ROUND 4: the start moved back 643 mm along the SAME heading, from mid-rug (0.349, 0.612) to (-0.0981, 0.8483) off the rug's
// left rim (every point of the blade >= 30 mm outside it): the blade touches down outside the film. Same end point;
// 71 -> 126 frames keeps ~700 mm/s.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(5, -0.0981f,0.8483f, 0.925f,0.3076f, 126, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(5, 0.925f, 0.3076f, true);
return CarpetCleaning.CarpetDebugApi.PlowSpillJson();
