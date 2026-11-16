// EXPERIMENT: does the fresh lane's sheen survive 2 s?
UnityEditor.EditorApplication.isPaused = true;
var a = CarpetCleaning.StoreShotApi.RenderScene("test/sheen_a.png", 1920, 1080);
CarpetCleaning.CarpetDebugApi.RunFrames(120);
var b = CarpetCleaning.StoreShotApi.RenderScene("test/sheen_b.png", 1920, 1080);
CarpetCleaning.CarpetDebugApi.RunFrames(240);
var c = CarpetCleaning.StoreShotApi.RenderScene("test/sheen_c.png", 1920, 1080);
return "ok";
