// STAGING SHORTCUT: brush POWER 6 / AREA 6 for the stain scrub
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.SetLevel(4, 0, 6); CarpetCleaning.EconomyDebugApi.SetLevel(4, 1, 6);
return CarpetCleaning.ToolTuning.Power(CarpetCleaning.ToolId.Brush) + " r " + CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.Brush).radiusMm;
