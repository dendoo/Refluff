// ACES re-shoot 01, ROUND 3: pin the wet-vac wand at the end of its track (0.50, 0.28) = the band's near tip, 33 real frames
// (the head settles, particles and the HUD update), Shot("01_hero_reveal"). Pin + Steps + Shot stay in ONE eval with
// isPaused = true (unpaused, the pointer over the Game view rotates a pinned head). The head keeps the heading of the last
// stroke (r3_track's vertical pull), so it lies across the track. Stays paused afterwards.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.PinToolHead(6, 0.50f, 0.28f, true);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
for (int i = 0; i < 33; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = true;
string r = CarpetCleaning.StoreShotApi.Shot("01_hero_reveal");
var sim = CarpetCleaning.CarpetDebugApi.Sim;
return r + " hint=" + sim.Hint + " clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.ToolHeadJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + " " + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 120);
