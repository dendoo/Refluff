// clip3 TRIAL t2: horizontal lane v 0.26, u 0.62-0.98 (right rim at 1.0). Scraper 4 fast passes, light foam pass (right
// button), brush scrub, squeegee pushes everything off the right rim onto the floor.
CarpetCleaning.CarpetDebugApi.Reset(154, 0.6f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(0);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
var G = typeof(CarpetCleaning.StoreGifApi);
CarpetCleaning.StoreGifApi.SeqBegin("c3_t2", 3840, 2160, 1500, 1060, 1680, 760);
CarpetCleaning.StoreGifApi.Tool(0);
CarpetCleaning.StoreGifApi.Hover(0.58f, 0.26f, 0.30f);
CarpetCleaning.StoreGifApi.Stroke(0.62f, 0.26f, 0.98f, 0.26f, 0.50f);
CarpetCleaning.StoreGifApi.Move(0.98f, 0.26f, 0.62f, 0.262f, 0.50f, true);
CarpetCleaning.StoreGifApi.Move(0.62f, 0.262f, 0.98f, 0.258f, 0.50f, true);
CarpetCleaning.StoreGifApi.Move(0.98f, 0.258f, 0.62f, 0.26f, 0.50f, true);
CarpetCleaning.StoreGifApi.Up(0.10f);
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Stroke(0.62f, 0.26f, 0.97f, 0.26f, 0.70f, 40f, true);
CarpetCleaning.StoreGifApi.Up(0.10f);
CarpetCleaning.StoreGifApi.Tool(4);
CarpetCleaning.StoreGifApi.Move(0.97f, 0.26f, 0.62f, 0.235f, 0.70f, true);
CarpetCleaning.StoreGifApi.Move(0.62f, 0.235f, 0.95f, 0.285f, 0.70f, true);
CarpetCleaning.StoreGifApi.Move(0.95f, 0.285f, 0.56f, 0.26f, 0.70f, true);
CarpetCleaning.StoreGifApi.Up(0.10f);
CarpetCleaning.StoreGifApi.Tool(5);
CarpetCleaning.StoreGifApi.Stroke(0.54f, 0.26f, 1.12f, 0.26f, 1.40f);
CarpetCleaning.StoreGifApi.Up(0.80f);
return CarpetCleaning.StoreGifApi.Run(3000);
