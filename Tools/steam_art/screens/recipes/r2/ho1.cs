CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
float[] du = {0.62f,0.76f,0.90f,0.48f};
for (int k=0;k<2;k++) for (int i=0;i<du.Length;i++){ if(((i+k)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.97f, du[i],0.03f, 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(7, du[i],0.03f, du[i],0.97f, 70, 1f); }
CarpetCleaning.CarpetDebugApi.RunFrames(60);
var sim = CarpetCleaning.CarpetDebugApi.Sim;
var s = sim.RefreshStatsSync();
return "wet " + sim.Wetness + " won " + sim.IsWon + " foam " + sim.FoamMean + " " + CarpetCleaning.CarpetDebugApi.HandoverJson();
