// EXPERIMENT: render, one lane at v 0.75, render again (brightness A/B of a squeegeed lane)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.SetTool(1);
var a = CarpetCleaning.StoreShotApi.RenderScene("test/ab_lane_a.png", 1920, 1080);
CarpetCleaning.CarpetDebugApi.StrokeNow(5, 0.35f,0.75f, 1.05f,0.75f, 60, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
CarpetCleaning.CarpetDebugApi.SetTool(1);
var b = CarpetCleaning.StoreShotApi.RenderScene("test/ab_lane_b.png", 1920, 1080);
return a + b;
