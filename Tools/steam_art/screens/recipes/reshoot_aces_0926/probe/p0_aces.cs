UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Reset(1);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
var r = CarpetCleaning.StoreShotApi.RenderScene("test/aces_probe.png", 1920, 1080);
return r + " " + CarpetCleaning.CarpetDebugApi.HandoverJson() + " " + CarpetCleaning.CarpetDebugApi.DirtJson();
