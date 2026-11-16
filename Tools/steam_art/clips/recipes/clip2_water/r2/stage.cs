// clip2 r2 staging: c2_eco's pre-kit levels, then the hose back to factory POWER (level 0: the r1 flood came from eco power 1),
// the rya with the dust-only profile (seed 68, level 0.36, crust 0, mud 0, dust 1, spill 0), live tuning, hose selected.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.SetLevel(2, 0, 0);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_finnish_rya"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(68, 0.36f, 0f, 0f, 1f, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(2);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
var e = CarpetCleaning.EconomyDebugApi.EconomyJson(); int k = e.IndexOf("levels"); 
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + (k >= 0 ? e.Substring(k, System.Math.Min(260, e.Length - k)) : e.Substring(0, 300));
