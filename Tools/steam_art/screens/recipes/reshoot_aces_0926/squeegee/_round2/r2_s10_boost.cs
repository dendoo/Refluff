// STAGING SHORTCUT: the dry vacuum at POWER 6 / AREA 6 for the suds passes only (the same result as the mid-game kit's
// POWER 4 / AREA 3 in ~2.3x fewer passes, so the flooded pile still holds standing water). r2_s12_restore puts PRE2 back.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.SetLevel(1, 0, 6); CarpetCleaning.EconomyDebugApi.SetLevel(1, 1, 6);
return CarpetCleaning.ToolTuning.Power(CarpetCleaning.ToolId.DryVacuum) + " area " + CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.DryVacuum).radiusMm;
