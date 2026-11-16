// hose the far half and the next lane again (the pile there floods: a standing film the squeegee lanes cut through)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.06f,0.40f, 0.94f,0.93f, 6, 36, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(10);
return "ok";
