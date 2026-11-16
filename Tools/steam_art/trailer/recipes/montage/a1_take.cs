// #19 cut A (0:48.00-0:49.33): the foam ring runs down the band's left lane toward the camera over the vivid clean half
// of the Finnish rya; the pink mound widens. Press at ~T 0.37 (the cut frame).
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var S = CarpetCleaning.StoreGifApi.SeqBegin("t19a_rya", 3840, 2160, 0, 0, 0, 0, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Hover(0.595f, 1.00f, 0.35f);
CarpetCleaning.StoreGifApi.Move(0.595f, 1.00f, 0.595f, 0.06f, 1.24f, true);
CarpetCleaning.StoreGifApi.Up(0.25f);
CarpetCleaning.StoreGifApi.Move(0.595f, 0.06f, 0.70f, -0.06f, 0.20f, false);
CarpetCleaning.StoreGifApi.Away(0.30f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
