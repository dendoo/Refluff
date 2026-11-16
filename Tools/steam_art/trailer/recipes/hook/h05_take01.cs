// #01 (0:00-0:02.7) THE HOOK on the Transylvanian rug: the squeegee plows lane A (heading -20 deg toward the near-right
// corner, blade AREA 2 = 317 mm, POWER 6) and pours the brown wave over the rim onto the white tile; the pattern opens behind
// the blade. 500 mm/s: the blade is mid-pass at shot start, at the rim ~1.4 s into the shot (cover frame). Retake = snap_load.
var S = CarpetCleaning.StoreGifApi.SeqBegin("t01_hook", 3840, 2160, 1000, 560, 2840, 1600, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(5);
CarpetCleaning.StoreGifApi.Hover(-0.14f, 0.426f, 0.20f);
CarpetCleaning.StoreGifApi.Stroke(-0.12f, 0.420f, 1.10f, 0.065f, 3.12f);
CarpetCleaning.StoreGifApi.Move(1.10f, 0.065f, 1.45f, 0.04f, 0.40f, false);
CarpetCleaning.StoreGifApi.Away(1.20f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
