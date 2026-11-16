// ACES re-shoot 01, ROUND 3 (new): two scraper chunks come to rest exactly on the far rim (v 1.000, u ~0.50 and ~0.68, found
// with r3_chunks); at the game camera they stand above the rim line and read as crumbs on the far tile, which r3_floor's mop
// cannot reach (they are inside the carpet rect). Two short wet-vac pulls over the rim suck them: the wet vac takes water,
// slurry, foam and chunks only, never crust, mud or dust, and the pile there is dry, so the dirt around stays as it is.
// Runs after r3_floor and BEFORE r3_track (the pinned wand keeps the heading of the last stroke: r3_track's vertical pull).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.50f, 1.00f, 0.50f, 0.975f, 6, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.68f, 1.00f, 0.68f, 0.975f, 6, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(10);
return CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
