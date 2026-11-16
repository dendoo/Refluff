// STAGING SHORTCUT: floor detergent + mop at AREA 6 (960 mm mop head) for the floor clean-up only (fewer rings); r2_f9_restore puts PRE2 back
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.SetLevel(8, 1, 6); CarpetCleaning.EconomyDebugApi.SetLevel(9, 1, 6);
return "det r " + CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.FloorCleaner).radiusMm + " mop L " + CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.Mop).lengthMm;
