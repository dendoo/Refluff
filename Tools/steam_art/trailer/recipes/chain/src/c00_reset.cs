// Chain S0: the Anatolian star (take A left it on the stage) with Reset(394, 0.9f) = the landed rug of #04 (same K_Init,
// clean floor); hero kit set by payoff/kit.inc; LiveTuning on (Reset switches ToolTuning to Neutral).
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
var H0 = CarpetCleaning.CarpetDebugApi.Handover;
if (H0.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
CarpetCleaning.CarpetDebugApi.Reset(394, 0.9f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(0);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
var hj = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = hj.IndexOf("picName");
return hj.Substring(k, 36) + " " + CarpetCleaning.CarpetDebugApi.DirtJson();
