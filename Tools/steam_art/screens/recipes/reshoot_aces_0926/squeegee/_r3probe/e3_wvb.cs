// STAGING SHORTCUT: wet vacuum POWER 6 / AREA 6 for the soap flush of the dried rug
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.SetLevel(6, 0, 6); CarpetCleaning.EconomyDebugApi.SetLevel(6, 1, 6);
return CarpetCleaning.ToolTuning.Power(CarpetCleaning.ToolId.WetVacuum) + " r " + CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.WetVacuum).radiusMm + " L " + CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.WetVacuum).lengthMm;
