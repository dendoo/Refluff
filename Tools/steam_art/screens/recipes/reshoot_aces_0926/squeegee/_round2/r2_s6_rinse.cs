// hose the scrubbed rug down (run 3x): saturates the pile (the squeegee needs standing water) and the flush collapses
// most of the suds (dilution); the runoff on the tiles is mopped later
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.05f,0.05f, 0.95f,0.95f, 11, 36, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(20);
return "ok";
