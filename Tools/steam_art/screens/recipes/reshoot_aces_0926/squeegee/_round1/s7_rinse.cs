// hose the stained-out rug down again (lanes kept off the rim to limit runoff): water stands on the pile, the thin soap
// does not regrow the suds
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.08f,0.07f, 0.92f,0.93f, 9, 40, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(20);
return "ok";
