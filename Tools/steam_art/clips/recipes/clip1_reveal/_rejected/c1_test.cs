// clip1 exploration take (half res, 12.5 fps): the whole row 1 + turn + row 2 with real Steps, to judge one pass's extraction.
var G = typeof(CarpetCleaning.StoreGifApi);
string r = CarpetCleaning.StoreGifApi.SeqBegin("c1_test", 1920, 1080, 360, 368, 1200, 500, false, 4);
CarpetCleaning.StoreGifApi.Tool(6);
CarpetCleaning.StoreGifApi.Hover(0.06f, 0.30f, 0.2f);
CarpetCleaning.StoreGifApi.Stroke(0.08f, 0.30f, 0.90f, 0.30f, 3.1f);
CarpetCleaning.StoreGifApi.Move(0.90f, 0.30f, 0.905f, 0.42f, 0.4f, false);
CarpetCleaning.StoreGifApi.Move(0.905f, 0.42f, 0.10f, 0.42f, 3.1f, true);
CarpetCleaning.StoreGifApi.Up(0.4f);
CarpetCleaning.StoreGifApi.Away(0.4f);
return r + " " + CarpetCleaning.StoreGifApi.Run(3000);
