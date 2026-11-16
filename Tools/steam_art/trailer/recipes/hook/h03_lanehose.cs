// #01 lane hose (replaces r3_t1_hose, whose inset v >= 0.16 would leave lane A's near half dry): far -> near over lane A's
// box, the same dose per area; then read the debris / floor state.
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.06f,0.62f, 0.90f,0.06f, 9, 32, 1f);
return CarpetCleaning.CarpetDebugApi.DebrisAliveJson() + " | floor " + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
