var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var eco = CarpetCleaning.EconomyDebugApi.Eco; var sim = CarpetCleaning.CarpetDebugApi.Sim;
typeof(CarpetCleaning.Economy).GetField("_clockAnchor", bf).SetValue(eco, sim.SimTime - 187.6f);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 20; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return CarpetCleaning.StoreShotApi.Shot("08_spotless_handover");
