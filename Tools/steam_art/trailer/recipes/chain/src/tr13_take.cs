// dryer POWER 6 (P 2.31) for #13 (brief_chain 5)
CarpetCleaning.EconomyDebugApi.SetLevel(7, 0, 6);
// #13 REFLUFF (0:32.0-0:34.7): the dryer along the band centre L->R at 171 mm/s: the damp dark striped pile dries,
// lightens and stands up at the ring trailing edge. Ring only.
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr13_refluff", 3840, 2160, 480, 360, 3360, 1800, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(7);
CarpetCleaning.StoreGifApi.Hover(0.05f, 0.36f, 0.20f);
CarpetCleaning.StoreGifApi.Move(0.05f, 0.36f, 0.62f, 0.36f, 4.00f, true);
CarpetCleaning.StoreGifApi.Up(0.30f);
CarpetCleaning.StoreGifApi.Away(0.50f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
