// ROUND 3. Anatolian star rug (kept from round 2: its cream medallions show through the brown water and in the wiped lanes).
// Fully soiled grime carpet with dust (level 1, crust 0, mud 1, dust 1, spill 0: mud 0.75 + dust 0.14 + stains 0.11), hosed down.
// (crust 0.5 was tried: 5 more scrubs, and the dryer stage clamps mud at 1 per cell, so it ended with less dirt, not more)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(17, 1.0f, 0f, 1f, 1f, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.04f,0.04f, 0.96f,0.96f, 11, 36, 1f);
return CarpetCleaning.CarpetDebugApi.HandoverJson().Substring(60) + CarpetCleaning.CarpetDebugApi.DirtJson();
