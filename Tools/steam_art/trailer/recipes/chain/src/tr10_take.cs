// #10 SCRUB (0:24.0-0:26.7): the rotary brush (AREA 6) scrubs 4 legs inside the band: foam swirls, greys to brown mush.
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr10_scrub", 3840, 2160, 480, 360, 3360, 1800, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(4);
CarpetCleaning.StoreGifApi.Hover(0.68f, 0.365f, 0.30f);
CarpetCleaning.StoreGifApi.Stroke(0.66f, 0.365f, 0.36f, 0.345f, 0.66f);
CarpetCleaning.StoreGifApi.Move(0.36f, 0.345f, 0.64f, 0.385f, 0.66f, true);
CarpetCleaning.StoreGifApi.Move(0.64f, 0.385f, 0.38f, 0.365f, 0.66f, true);
CarpetCleaning.StoreGifApi.Move(0.38f, 0.365f, 0.62f, 0.35f, 0.66f, true);
CarpetCleaning.StoreGifApi.Up(0.35f);
CarpetCleaning.StoreGifApi.Away(0.20f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
