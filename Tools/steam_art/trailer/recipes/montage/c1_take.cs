// #19 cut C (0:50.67-0:52.00): the dryer ring runs down a damp striped lane of the European green (the left third
// already dried and upright). Press at ~T 0.25.
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var S = CarpetCleaning.StoreGifApi.SeqBegin("t19c_eu", 3840, 2160, 0, 0, 0, 0, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(7);
CarpetCleaning.StoreGifApi.Hover(0.53f, 1.03f, 0.25f);
CarpetCleaning.StoreGifApi.Move(0.53f, 1.03f, 0.53f, 0.36f, 1.46f, true);
CarpetCleaning.StoreGifApi.Up(0.30f);
CarpetCleaning.StoreGifApi.Away(0.20f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
