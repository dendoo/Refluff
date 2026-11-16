// ACES re-shoot 01 (round 1): second half of reshoot_0926/reshoot_a/w_band3shot: pin the wet-vac wand on the band's near end
// (0.50, 0.28), 33 real frames (the head settles, particles and the HUD update), Shot("01_hero_reveal"). Pin + Steps + Shot stay
// in ONE eval with isPaused = true (unpaused, the pointer over the Game view rotates a pinned head). Stays paused afterwards.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.PinToolHead(6, 0.50f, 0.28f, true);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
for (int i = 0; i < 33; i++) UnityEditor.EditorApplication.Step();
string r = CarpetCleaning.StoreShotApi.Shot("01_hero_reveal");
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return r + " hint=" + sim.Hint + " clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.ToolHeadJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + " " + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 120);
