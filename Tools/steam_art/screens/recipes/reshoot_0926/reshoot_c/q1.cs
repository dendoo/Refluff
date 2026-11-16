UnityEditor.EditorApplication.isPaused = true;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
float c0 = sim.Cleanliness;
string st = CarpetCleaning.CarpetDebugApi.StatsJson();
return "before=" + c0 + " after=" + sim.Cleanliness + " hint=" + sim.Hint + " " + st;
