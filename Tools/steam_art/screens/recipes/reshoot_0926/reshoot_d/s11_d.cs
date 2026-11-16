UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 40; i++) UnityEditor.EditorApplication.Step();
var r = CarpetCleaning.StoreShotApi.Shot("11_upgrades_shop");
return r + " hint=" + CarpetCleaning.CarpetDebugApi.Sim.Hint + " simT=" + CarpetCleaning.CarpetDebugApi.Sim.SimTime + " eco=" + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 200);
