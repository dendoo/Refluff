// #13 variant B: lawn stripes across the band: wet-vac passes ALONG v in alternating directions, one blade width (192 mm =
// 0.16 u) apart, so the damp pressed pile leans up / down in alternate stripes; the dryer then runs across them (along u)
// and erases the stripes behind its ring as it dries and straightens the pile (FIBER_SET.md).
float[] su = {0.13f, 0.29f, 0.45f, 0.61f, 0.77f};
for (int i = 0; i < su.Length; i++) { if ((i & 1) == 0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, su[i], 0.47f, su[i], 0.25f, 40, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, su[i], 0.25f, su[i], 0.47f, 40, 1f); }
float[] sv = {0.21f, 0.37f, 0.53f, 0.69f, 0.85f};
for (int i = 0; i < sv.Length; i++) { if ((i & 1) == 1) CarpetCleaning.CarpetDebugApi.StrokeNow(6, sv[i], 0.47f, sv[i], 0.25f, 40, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, sv[i], 0.25f, sv[i], 0.47f, 40, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(20);
CarpetCleaning.StoreShotApi.RenderScene("trailer/probe/r13b.png", 1920, 1080);
return CarpetCleaning.CarpetDebugApi.FiberProbeJson(0.20f, 0.33f, 0.60f, 0.39f);
