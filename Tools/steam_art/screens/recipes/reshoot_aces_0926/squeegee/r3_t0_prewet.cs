// re-wet the dried rug lightly (right button = half pressure, whole rug): the settled grime dissolves into this little water
// (murky, C ~0.6 in the pile), then 10 s for it to soak in
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.03f,0.03f, 0.97f,0.97f, 11, 36, 0.5f);
CarpetCleaning.CarpetDebugApi.RunFrames(600);
return "ok";
