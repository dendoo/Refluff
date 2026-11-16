// ACES re-shoot 01 (round 1): first half of reshoot_0926/reshoot_a/w_band3shot. Mop the far tile (the scraper chunks flung off
// the far rim land up to ~0.2 v = 300 mm beyond it, plus the band's runoff puddle): strokes at v 1.10 / 1.06 as before + a new
// one at v 1.20 (mop blade 320 mm across the stroke). Then the five final wet-vac lanes, ending at the wand's spot v 0.28.
// The edge lanes u 0.30 / 0.70 (new, as in w_band2) run first so every lane's stripe is fresh. The pin + Steps + Shot follow in w_shot (one eval, as the old pitfall note asks).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.15f,1.10f, 0.90f,1.10f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.90f,1.06f, 0.15f,1.06f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.15f,1.20f, 0.90f,1.20f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.30f,0.98f, 0.30f,0.28f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.70f,0.98f, 0.70f,0.28f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.365f,0.97f, 0.365f,0.28f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.635f,0.97f, 0.635f,0.28f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.50f,0.97f, 0.50f,0.28f, 60, 1f);
return CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + " " + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
