// THE WATER: one full hose pass over the rug inset from the rims (u 0.12..0.88, v 0.88..0.16), far -> near, so the standing
// sheet is freshest where the squeegee works; it takes the murk of the pile (the dissolved grime) and stands brown
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.12f,0.88f, 0.88f,0.16f, 10, 32, 1f);
return "ok";
