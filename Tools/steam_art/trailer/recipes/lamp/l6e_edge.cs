// the mop lanes pushed a line of slurry to their far ends (v ~1.14): wet-vac it across, then let the damp film dry 8 s
CarpetCleaning.CarpetDebugApi.StrokeNow(6, -0.66f,1.12f, 0.02f,1.12f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.02f,1.17f, -0.66f,1.17f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, -0.66f,-0.12f, 0.02f,-0.12f, 60, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(480);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
return CarpetCleaning.CarpetDebugApi.FloorStatsJson() + " | " + CarpetCleaning.CarpetDebugApi.HighlightJson().Substring(0, 200);
