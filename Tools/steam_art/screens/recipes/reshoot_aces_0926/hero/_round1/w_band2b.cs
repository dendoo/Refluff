// ACES re-shoot 01 (round 1, new): wet-vac the foam the brush pushed over the band's near end onto the crust in front of it
// (violet blobs at v 0.24-0.27, u 0.45-0.66): two horizontal lanes (nozzle across v ~0.19-0.32). The wet vac never touches
// crust, so the plates stay; it only takes foam, water and the loose chunks it passes over.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.34f,0.26f, 0.70f,0.26f, 60, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.70f,0.255f, 0.34f,0.255f, 60, 1f);
return CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
