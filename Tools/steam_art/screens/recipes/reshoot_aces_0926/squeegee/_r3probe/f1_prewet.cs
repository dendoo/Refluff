// EXPERIMENT: light re-wet (right button = half pressure) over the whole rug, then 20 s for the settled mud to dissolve
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.03f,0.03f, 0.97f,0.97f, 11, 36, 0.5f);
CarpetCleaning.CarpetDebugApi.RunFrames(600);
return "ok";
