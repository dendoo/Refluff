// #12 EXTRACT (0:29.3-0:32.0, wider): wet-vac row 2 at v 0.42 R->L swallows the far strip slurry; behind it the
// pattern bright, the pile dark with moisture and striped; the lift runs ALONG the row (a lift across swings the wand).
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr12_extract", 3840, 2160, 480, 360, 3360, 1800, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(6);
CarpetCleaning.StoreGifApi.Hover(0.935f, 0.42f, 0.30f);
CarpetCleaning.StoreGifApi.Stroke(0.905f, 0.42f, 0.10f, 0.42f, 2.93f);
CarpetCleaning.StoreGifApi.Move(0.10f, 0.42f, 0.085f, 0.422f, 0.16f, false);
CarpetCleaning.StoreGifApi.Move(0.085f, 0.422f, -0.35f, 0.46f, 0.56f, false);
CarpetCleaning.StoreGifApi.Away(0.60f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
