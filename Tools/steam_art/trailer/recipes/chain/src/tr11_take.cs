// #11 SQUEEGEE (0:26.7-0:29.3, wider "how" frame): one horizontal pass at v 0.30 (blade 264 mm covers v 0.21-0.39)
// off the right rim; the far strip v 0.39-0.45 keeps its slurry for #12. Touch-down outside the left rim.
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr11_squeegee", 3840, 2160, 480, 360, 3360, 1800, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(5);
CarpetCleaning.StoreGifApi.Hover(-0.06f, 0.30f, 0.20f);
CarpetCleaning.StoreGifApi.Stroke(-0.03f, 0.30f, 1.12f, 0.30f, 2.30f);
CarpetCleaning.StoreGifApi.Move(1.12f, 0.30f, 1.42f, 0.315f, 0.40f, false);
CarpetCleaning.StoreGifApi.Away(1.00f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
