// #19 cut B (0:49.33-0:50.67): the blue hose nozzle sprays the Salor Turkmen at half pressure, beads fly, the glossy
// sheet spreads over the dark soaked field. Press at ~T 0.46.
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var S = CarpetCleaning.StoreGifApi.SeqBegin("t19b_salor", 3840, 2160, 0, 0, 0, 0, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.CarpetDebugApi.Tools.SnapHeadingOnNextMove());
CarpetCleaning.StoreGifApi.Tool(2);
CarpetCleaning.StoreGifApi.Hover(0.33f, 0.74f, 0.30f);
CarpetCleaning.StoreGifApi.Move(0.33f, 0.74f, 0.34f, 0.70f, 0.16f, false);
CarpetCleaning.StoreGifApi.Move(0.34f, 0.70f, 0.40f, 0.42f, 0.45f, true, true);
CarpetCleaning.StoreGifApi.Move(0.40f, 0.42f, 0.42f, 0.29f, 0.40f, true, true);
CarpetCleaning.StoreGifApi.Move(0.42f, 0.29f, 0.42f, 0.29f, 0.30f, true, true);
CarpetCleaning.StoreGifApi.Up(0.35f);
CarpetCleaning.StoreGifApi.Away(0.20f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
