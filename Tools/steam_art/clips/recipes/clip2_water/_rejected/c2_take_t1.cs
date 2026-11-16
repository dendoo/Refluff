// clip2 trial take t1 (learning the timing): hose hold + drag, foam stroke, head leaves right, settle.
string w = CarpetCleaning.StoreGifApi.SeqBegin("c2_warm", 1280, 720, 0, 0, 0, 0, false, 100000);
CarpetCleaning.StoreGifApi.Tool(2);
CarpetCleaning.StoreGifApi.Hover(0.44f, 0.32f, 0.3f);
string wr = CarpetCleaning.StoreGifApi.Run(3000);
string we = CarpetCleaning.StoreGifApi.SeqEnd(2000);
string r = CarpetCleaning.StoreGifApi.SeqBegin("c2_t1", 3840, 2160, 1080, 970, 1680, 700);
CarpetCleaning.StoreGifApi.Hover(0.44f, 0.32f, 0.3f);
CarpetCleaning.StoreGifApi.Hold(0.44f, 0.32f, 1.2f);
CarpetCleaning.StoreGifApi.Move(0.44f, 0.32f, 0.56f, 0.34f, 1.6f, true);
CarpetCleaning.StoreGifApi.Up(0.2f);
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Stroke(0.42f, 0.31f, 0.58f, 0.345f, 1.6f);
CarpetCleaning.StoreGifApi.Move(0.58f, 0.345f, 1.1f, 0.40f, 0.5f, false);
CarpetCleaning.StoreGifApi.Away(3.0f);
return wr + " | " + we + " | " + r + " | " + CarpetCleaning.StoreGifApi.Run(2500);
