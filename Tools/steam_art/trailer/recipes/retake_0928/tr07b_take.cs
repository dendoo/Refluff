// Retake 2026-09-28 of #07 with the current game (the dry vac lays the pile, FIBER_SET.md 4-7): same Steps as tr07_take.cs.
// #07 VACUUM (0:16.0-0:18.7): one dry-vac row at the band centre R->L between the vacuumed lower third and the grey,
// crumb-strewn upper third; crumbs vanish at the ring (GPU capture -> tube rattle). Ring only. 332 mm/s.
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr07b_vacuum", 3840, 2160, 480, 360, 3360, 1800, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(1);
CarpetCleaning.StoreGifApi.Hover(0.93f, 0.36f, 0.30f);
CarpetCleaning.StoreGifApi.Stroke(0.905f, 0.36f, 0.13f, 0.36f, 2.80f);
CarpetCleaning.StoreGifApi.Up(0.15f);
CarpetCleaning.StoreGifApi.Away(0.35f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
