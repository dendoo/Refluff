UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 3; i++) UnityEditor.EditorApplication.Step();
string r = CarpetCleaning.StoreShotApi.Shot("01_hero_reveal");
UnityEditor.EditorApplication.isPaused = false;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return r + " hint=" + sim.Hint + " clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " " + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 260);
