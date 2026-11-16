// Retake 2026-09-28: #06's take (chain/src/tr06_take.cs) replayed with the same Steps but NO frames and no audio log
// (every 100000, scratch folder _rs06, deleted afterwards), so #07 starts from the state the 2026-09-27 chain gave it.
var S = CarpetCleaning.StoreGifApi.SeqBegin("_rs06", 3840, 2160, 480, 360, 3360, 1800, false, 100000, true, 2560, 1440, 60, false);
CarpetCleaning.StoreGifApi.Tool(0);
CarpetCleaning.StoreGifApi.Hover(0.43f, 0.345f, 0.34f);
CarpetCleaning.StoreGifApi.Stroke(0.46f, 0.345f, 0.86f, 0.345f, 0.55f);
CarpetCleaning.StoreGifApi.Move(0.86f, 0.345f, 0.46f, 0.347f, 0.55f, true);
CarpetCleaning.StoreGifApi.Move(0.46f, 0.347f, 0.86f, 0.343f, 0.55f, true);
CarpetCleaning.StoreGifApi.Move(0.86f, 0.343f, 0.46f, 0.345f, 0.55f, true);
CarpetCleaning.StoreGifApi.Up(0.12f);
CarpetCleaning.StoreGifApi.Move(0.46f, 0.345f, 0.30f, 0.36f, 0.30f, false);
CarpetCleaning.StoreGifApi.Away(0.40f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
