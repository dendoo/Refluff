CarpetCleaning.EconomyDebugApi.SetOwned(1, false);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 20; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return CarpetCleaning.StoreShotApi.Shot("11_upgrades_shop");
