// debug: renders of the pour at +1.5 s and +4 s (sim frames)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.RunFrames(90);
var a = CarpetCleaning.StoreShotApi.RenderScene("test/sq3_ev1.png", 1920, 1080);
CarpetCleaning.CarpetDebugApi.RunFrames(150);
var b = CarpetCleaning.StoreShotApi.RenderScene("test/sq3_ev2.png", 1920, 1080);
return "ok";
