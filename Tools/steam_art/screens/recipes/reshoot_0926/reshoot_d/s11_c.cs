UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.SetOwned(1, false);
for (int i = 0; i < 20; i++) UnityEditor.EditorApplication.Step();
var r = CarpetCleaning.StoreShotApi.Shot("11_upgrades_shop");
return r + " hint=" + CarpetCleaning.CarpetDebugApi.Sim.Hint + " eco=" + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 260);
