UnityEditor.EditorApplication.isPaused = true;
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var eco = CarpetCleaning.EconomyDebugApi.Eco; var sim = CarpetCleaning.CarpetDebugApi.Sim;
typeof(CarpetCleaning.Economy).GetField("_clockAnchor", bf).SetValue(eco, sim.SimTime - 187.6f);
for (int i = 0; i < 20; i++) UnityEditor.EditorApplication.Step();
string r = CarpetCleaning.StoreShotApi.Shot("08_spotless_handover");
return r + " hint=" + sim.Hint + " clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.HandoverJson() + " " + CarpetCleaning.CarpetDebugApi.ToolHeadJson() + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 230);
