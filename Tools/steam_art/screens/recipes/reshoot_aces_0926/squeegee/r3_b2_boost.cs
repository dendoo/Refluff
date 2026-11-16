// STAGING SHORTCUT: wet vacuum at POWER 6 / AREA 6 for the soap flush only (fewer passes); r3_b9_restore puts PRE2 back
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.SetLevel(6, 0, 6); CarpetCleaning.EconomyDebugApi.SetLevel(6, 1, 6);
return "wv P " + CarpetCleaning.ToolTuning.Power(CarpetCleaning.ToolId.WetVacuum);
