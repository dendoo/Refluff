CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
CarpetCleaning.CarpetDebugApi.Sim.UiPaused = false;
UnityEditor.EditorApplication.isPaused = true;  // ACES re-shoot: stay paused between evals
return "paused=" + UnityEditor.EditorApplication.isPaused + " uip=" + CarpetCleaning.CarpetDebugApi.Sim.UiPaused + " shop=" + CarpetCleaning.EconomyDebugApi.ShopJson().Substring(0,15) + " " + CarpetCleaning.CarpetDebugApi.HighlightJson().Substring(0,120) + " head=" + CarpetCleaning.CarpetDebugApi.ToolHeadJson().Substring(0,40) + " eco=" + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0,300);
