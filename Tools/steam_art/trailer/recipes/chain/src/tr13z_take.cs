// #13 REFLUFF retake (TRAILER.md 8.2 fallback): the damp band under the moisture lamp Z glows blue; the dryer (POWER 6) runs
// along it L->R at 171 mm/s and the glow dies right behind its ring as the pile dries (a cell glows only above the lamp's
// level, HIGHLIGHTS.md 4.1). In the plain take the lightening of the pile did not read at 600 px (review of cut 1).
CarpetCleaning.EconomyDebugApi.SetLevel(7, 0, 6);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
CarpetCleaning.CarpetDebugApi.SetHighlight(0, true, true);
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr13z_refluff", 3840, 2160, 480, 360, 3360, 1800, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(7);
CarpetCleaning.StoreGifApi.Hover(0.05f, 0.36f, 0.30f);
CarpetCleaning.StoreGifApi.Move(0.05f, 0.36f, 0.62f, 0.36f, 4.00f, true);
CarpetCleaning.StoreGifApi.Up(0.30f);
CarpetCleaning.StoreGifApi.Away(0.50f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
