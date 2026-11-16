// #18 (0:45.3-0:48.0, HUD): the lamp. X at T 0.40 (the key's own path: click, chip lit, dim + amber leftovers, scan ring),
// Z on top at T 1.75 (damp zone blue). Pointer away (the head and ring are never dimmed). Real Steps = real fades.
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var S = CarpetCleaning.StoreGifApi.SeqBegin("t18_lamp", 3840, 2160, 0, 0, 0, 0, true, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(6);
CarpetCleaning.StoreGifApi.Away(0.40f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.CarpetDebugApi.ToggleHighlight(1));
CarpetCleaning.StoreGifApi.Away(1.35f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.CarpetDebugApi.ToggleHighlight(0));
CarpetCleaning.StoreGifApi.Away(1.65f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
