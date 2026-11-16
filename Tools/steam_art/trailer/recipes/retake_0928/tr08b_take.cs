// Retake 2026-09-28 of #08 with the current game (the hose water lays the pile, FIBER_SET.md 4-7): same Steps as tr08_take.cs.
// #08 SOAK (0:18.7-0:21.3): three half-pressure hose spots along the band centre spread and join, the dust darkens
// to mud, beads fly round the nozzle (right button = half pressure).
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr08b_soak", 3840, 2160, 480, 360, 3360, 1800, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(2);
CarpetCleaning.StoreGifApi.Hover(0.36f, 0.36f, 0.30f);
CarpetCleaning.StoreGifApi.Move(0.36f, 0.36f, 0.36f, 0.36f, 0.55f, true, true);
CarpetCleaning.StoreGifApi.Move(0.36f, 0.36f, 0.50f, 0.365f, 0.20f, false);
CarpetCleaning.StoreGifApi.Move(0.50f, 0.365f, 0.50f, 0.365f, 0.55f, true, true);
CarpetCleaning.StoreGifApi.Move(0.50f, 0.365f, 0.64f, 0.36f, 0.20f, false);
CarpetCleaning.StoreGifApi.Move(0.64f, 0.36f, 0.64f, 0.36f, 0.55f, true, true);
CarpetCleaning.StoreGifApi.Hover(0.64f, 0.36f, 0.40f);
CarpetCleaning.StoreGifApi.Away(0.35f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
