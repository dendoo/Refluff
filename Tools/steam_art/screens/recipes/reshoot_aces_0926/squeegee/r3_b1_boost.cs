// STAGING SHORTCUT: brush and foam sprayer at POWER 6 / AREA 6 for the stain stage only (the same result as the mid-game kit
// in fewer passes); r3_b9_restore puts the PRE2 levels back before anything is shot
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.SetLevel(4, 0, 6); CarpetCleaning.EconomyDebugApi.SetLevel(4, 1, 6);
CarpetCleaning.EconomyDebugApi.SetLevel(3, 0, 6); CarpetCleaning.EconomyDebugApi.SetLevel(3, 1, 6);
return "brush P " + CarpetCleaning.ToolTuning.Power(CarpetCleaning.ToolId.Brush) + " foam P " + CarpetCleaning.ToolTuning.Power(CarpetCleaning.ToolId.FoamSprayer);
