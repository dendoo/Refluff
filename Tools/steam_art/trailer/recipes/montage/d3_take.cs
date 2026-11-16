// #19 cut D (0:52.00-0:53.33): the mop pad wipes a clean lane through the brown runoff on the tile beside the
// Transylvanian's left border, away from the camera. Press at ~T 0.41.
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var S = CarpetCleaning.StoreGifApi.SeqBegin("t19d_mop", 3840, 2160, 0, 0, 0, 0, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.CarpetDebugApi.Tools.SnapHeadingOnNextMove());
CarpetCleaning.StoreGifApi.Tool(9);
CarpetCleaning.StoreGifApi.Hover(-0.16f, -0.14f, 0.25f);
CarpetCleaning.StoreGifApi.Move(-0.16f, -0.14f, -0.16f, -0.10f, 0.16f, false);
CarpetCleaning.StoreGifApi.Move(-0.16f, -0.10f, -0.16f, 0.58f, 1.13f, true);
CarpetCleaning.StoreGifApi.Up(0.35f);
CarpetCleaning.StoreGifApi.Away(0.20f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
