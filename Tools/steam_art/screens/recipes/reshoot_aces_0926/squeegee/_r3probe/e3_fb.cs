// STAGING SHORTCUT: foam sprayer POWER 6 / AREA 6 for the stain stage
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.SetLevel(3, 0, 6); CarpetCleaning.EconomyDebugApi.SetLevel(3, 1, 6);
return CarpetCleaning.ToolTuning.Power(CarpetCleaning.ToolId.FoamSprayer) + " r " + CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.FoamSprayer).radiusMm;
