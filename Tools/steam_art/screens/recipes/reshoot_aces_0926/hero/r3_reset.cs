// ACES re-shoot 01, art-director ROUND 3: crust-over-grime seed 394 at level 0.9 (seed route; no level 1, no ResetWithProfile).
// Round 2 used 242 (crust 0.87): ~95 % of the dirty surround was one even greige plate mosaic (a pebble / cork mat, lighter
// than the rug at thumbnail size). 394 (dust 0.65, mud 0.91, crust 0.61, spill 0.89) puts the cake in the middle, where the
// band cuts through it, and leaves dark, ground-in, print-gone grime pile on both flanks, the far edge and the near-right
// corner (probe renders SteamStore/_work/test/r3probe_{394,174,101,242}.png). No whole-carpet dry-vac passes (no-HUD crop).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Reset(394, 0.9f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt", "SteamStore/_work/test/r3_dirt_0.png");
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.HandoverJson();
