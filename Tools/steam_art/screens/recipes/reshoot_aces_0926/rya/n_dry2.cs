// ACES re-shoot 05 (round 2, new): dry the strip under the coming foam band evenly. After the rinse (ryaR/ryaR2) the pile at
// u 0.60-0.66 stays damp (w 0.05-0.10) where n_dry's lane u 0.50 does not reach; damp pile holds sprayed foam up while dry
// pile soaks it in, so the band grew a tall crest on its right half and its flank ended short of the crust edge (a dark
// scraped sliver showed at u 0.70-0.715). Two dryer lanes at u 0.63 (disc r 178 mm, PRE2) bring it to w ~0.02 like the rest.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(7, 0.63f,0.96f, 0.63f,0.04f, 80, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(7, 0.63f,0.04f, 0.63f,0.96f, 80, 1f);
return CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.FiberProbeJson(0.50f,0.1f,0.72f,0.9f);
