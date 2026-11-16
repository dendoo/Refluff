// ACES re-shoot 05: the scraped strip ends at the crust edge u ~0.72, just right of the band (n_foam1/2 lanes 0.585 + 0.62
// leave foam to u ~0.71): one extra sprayer lane at u 0.655 (same speed) carries the band's right flank over the crust edge
// so no bare scraped pile shows between foam and crust. Run after n_foam1 + n_foam2, right before n_shot.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.655f,0.95f, 0.655f,0.06f, 70, 1f);
return CarpetCleaning.CarpetDebugApi.StatsJson();
