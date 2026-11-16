// back to the mid-game kit (pre2.cs): dry vacuum POWER 4 / AREA 3
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.SetLevel(1, 0, 4); CarpetCleaning.EconomyDebugApi.SetLevel(1, 1, 3);
return CarpetCleaning.ToolTuning.Power(CarpetCleaning.ToolId.DryVacuum) + " area " + CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.DryVacuum).radiusMm;
