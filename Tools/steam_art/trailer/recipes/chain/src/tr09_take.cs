// #09 FOAM (0:21.3-0:24.0): one half-pressure foam lane L->R along the band centre, 330 mm/s (1.05x the dose clip 3
// validated). Ring only.
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr09_foam", 3840, 2160, 480, 360, 3360, 1800, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Hover(0.10f, 0.365f, 0.25f);
CarpetCleaning.StoreGifApi.Stroke(0.13f, 0.365f, 0.87f, 0.365f, 2.70f, 40f, true);
CarpetCleaning.StoreGifApi.Up(0.20f);
CarpetCleaning.StoreGifApi.Away(0.40f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
