CarpetCleaning.EconomyDebugApi.ShopOpen(false);
for (int i = 0; i < 30; i++) UnityEditor.EditorApplication.Step();
return CarpetCleaning.EconomyDebugApi.ShopJson();
