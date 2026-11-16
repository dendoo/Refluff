// after the shot: head unpinned, pointer back to the mouse, sim running; the Editor STAYS paused
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var tc = CarpetCleaning.CarpetDebugApi.Tools; tc.ScriptedPointer = false;
CarpetCleaning.CarpetDebugApi.Sim.UiPaused = false;
return "paused=" + UnityEditor.EditorApplication.isPaused + " uip=" + CarpetCleaning.CarpetDebugApi.Sim.UiPaused + " sp=" + tc.ScriptedPointer + " " + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 120);
