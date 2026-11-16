CarpetCleaning.CarpetDebugApi.RunFrames(90);
CarpetCleaning.CarpetDebugApi.StrokeNow(2, 0.62f,0.20f, 0.50f,0.28f, 14, 1f);
CarpetCleaning.CarpetDebugApi.Hold(2, 0.50f, 0.28f, 0.25f);
CarpetCleaning.CarpetDebugApi.PinToolHead(2, 0.50f, 0.28f, true);
UnityEditor.EditorApplication.isPaused = true;
UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.StoreShotApi.RenderScene("test/hose2.png",1920,1080);
return CarpetCleaning.CarpetDebugApi.Sim.Hint + CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + " wet " + CarpetCleaning.CarpetDebugApi.Sim.Wetness;
