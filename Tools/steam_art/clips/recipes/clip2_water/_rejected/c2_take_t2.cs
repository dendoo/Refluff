// clip2 trial take t2: less hose water (1.5 s: hold + curved drag), foam heap + drag, ring leaves, settle. Crop 1920x800.
string w = CarpetCleaning.StoreGifApi.SeqBegin("c2_warm", 1280, 720, 0, 0, 0, 0, false, 100000);
CarpetCleaning.StoreGifApi.Tool(2);
CarpetCleaning.StoreGifApi.Hover(0.45f, 0.335f, 0.3f);
string wr = CarpetCleaning.StoreGifApi.Run(3000);
string we = CarpetCleaning.StoreGifApi.SeqEnd(2000);
string r = CarpetCleaning.StoreGifApi.SeqBegin("c2_t2", 3840, 2160, 960, 890, 1920, 800);
CarpetCleaning.StoreGifApi.Hold(0.45f, 0.335f, 0.5f);
CarpetCleaning.StoreGifApi.Move(0.45f, 0.335f, 0.51f, 0.345f, 0.5f, true);
CarpetCleaning.StoreGifApi.Move(0.51f, 0.345f, 0.555f, 0.33f, 0.5f, true);
CarpetCleaning.StoreGifApi.Up(0.15f);
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Move(0.555f, 0.33f, 0.47f, 0.335f, 0.3f, false);
CarpetCleaning.StoreGifApi.Hold(0.47f, 0.335f, 0.5f);
CarpetCleaning.StoreGifApi.Move(0.47f, 0.335f, 0.54f, 0.345f, 0.9f, true);
CarpetCleaning.StoreGifApi.Hold(0.54f, 0.345f, 0.3f);
CarpetCleaning.StoreGifApi.Move(0.54f, 0.345f, 0.80f, 0.52f, 0.45f, false);
CarpetCleaning.StoreGifApi.Away(3.0f);
return wr + " | " + we + " | " + r + " | " + CarpetCleaning.StoreGifApi.Run(2500);
