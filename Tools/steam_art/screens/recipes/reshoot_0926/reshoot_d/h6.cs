UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(1, 0.04f,0.04f,0.96f,0.96f, 7, 50, 1f);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.22f,0.62f, 0.78f,0.38f, 4, 40, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(20);
CarpetCleaning.CarpetDebugApi.StrokeNow(2, 0.25f,0.28f, 0.52f,0.30f, 30, 1f);
CarpetCleaning.CarpetDebugApi.PinToolHead(2, 0.52f, 0.30f, true);
for (int i=0;i<2;i++) UnityEditor.EditorApplication.Step();
return CarpetCleaning.CarpetDebugApi.Sim.Hint + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
