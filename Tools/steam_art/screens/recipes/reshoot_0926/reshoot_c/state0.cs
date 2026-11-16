UnityEditor.EditorApplication.isPaused = true;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return "paused; hint=" + sim.Hint + " " + CarpetCleaning.CarpetDebugApi.HandoverJson() + " ECO " + CarpetCleaning.EconomyDebugApi.EconomyJson() + " HEAD " + CarpetCleaning.CarpetDebugApi.ToolHeadJson();
