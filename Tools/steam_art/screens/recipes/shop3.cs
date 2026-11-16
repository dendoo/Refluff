CarpetCleaning.CarpetDebugApi.Zigzag(6, 0.12f,0.54f, 0.88f,0.76f, 8, 60, 1f);
CarpetCleaning.CarpetDebugApi.SetTool(4);
CarpetCleaning.EconomyDebugApi.ShopOpen(true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 40; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return CarpetCleaning.EconomyDebugApi.ShopJson().Substring(0, 300);
