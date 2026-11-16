// TikTok S0: the Anatolian star (s00_setup put it on the stage), seed 394 at level 0.75 with NO crust (dried-mud slabs
// hide the pile, and the pile is the subject), mud 0.45 and dust 0.85 (the seed's mud 0.91 turns the whole rug one brown;
// this keeps a grey-brown film with the pattern faintly through it). Spills keep the seed's roll. LiveTuning on.
CarpetCleaning.CarpetDebugApi.ResetWithProfile(394, 0.75f, 0f, 0.45f, 0.85f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(0);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
