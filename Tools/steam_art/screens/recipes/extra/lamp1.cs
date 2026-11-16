CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.EconomyDebugApi.SetOwned(1, true);
CarpetCleaning.CarpetDebugApi.Reset(14, 0.35f);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 20; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.StoreShotApi.RenderScene("test/lamp1.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.HighlightJson() + CarpetCleaning.CarpetDebugApi.StatsJson();
