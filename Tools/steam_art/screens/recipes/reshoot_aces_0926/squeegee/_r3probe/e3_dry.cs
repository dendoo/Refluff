// dryer zigzag over the whole rug (collapses the suds, evaporates the water: the slurry settles back into the pile as mud)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(7, 0.03f,0.03f, 0.97f,0.97f, 10, 50, 1f);
return CarpetCleaning.ToolTuning.Power(CarpetCleaning.ToolId.Dryer) + " r " + CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.Dryer).radiusMm;
