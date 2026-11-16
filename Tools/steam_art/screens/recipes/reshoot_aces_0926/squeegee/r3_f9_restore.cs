// back to the mid-game kit (pre2.cs): floor detergent AREA 1, mop AREA 1
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.SetLevel(8, 1, 1); CarpetCleaning.EconomyDebugApi.SetLevel(9, 1, 1);
return "det r " + CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.FloorCleaner).radiusMm + " mop L " + CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.Mop).lengthMm;
