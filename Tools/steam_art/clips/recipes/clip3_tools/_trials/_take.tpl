// clip3 take __NAME__: horizontal lane v 0.26, u 0.62-0.98 (the right rim is u 1.0) through the crust of seed 154 @0.6.
// Scraper 4 fast passes, a light foam pass (right button), a rotary-brush scrub, the squeegee pushes the lot off the rim.
__STAGE__
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(0);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
CarpetCleaning.StoreGifApi.SeqBegin("__NAME__", 3840, 2160, __CROP__);
CarpetCleaning.StoreGifApi.Tool(0);
CarpetCleaning.StoreGifApi.Hover(0.585f, 0.26f, 0.20f);
CarpetCleaning.StoreGifApi.Stroke(0.62f, 0.26f, 0.98f, 0.26f, 0.50f);
CarpetCleaning.StoreGifApi.Move(0.98f, 0.26f, 0.62f, 0.262f, 0.50f, true);
CarpetCleaning.StoreGifApi.Move(0.62f, 0.262f, 0.98f, 0.258f, 0.50f, true);
CarpetCleaning.StoreGifApi.Move(0.98f, 0.258f, 0.62f, 0.26f, 0.50f, true);
CarpetCleaning.StoreGifApi.Up(0.12f);
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Stroke(0.62f, 0.26f, 0.97f, 0.26f, __FOAMS__, 40f, true);
CarpetCleaning.StoreGifApi.Up(0.12f);
CarpetCleaning.StoreGifApi.Tool(4);
CarpetCleaning.StoreGifApi.Move(0.97f, 0.26f, 0.60f, 0.235f, 0.70f, true);
CarpetCleaning.StoreGifApi.Move(0.60f, 0.235f, 0.95f, 0.285f, 0.70f, true);
CarpetCleaning.StoreGifApi.Move(0.95f, 0.285f, 0.467f, 0.26f, 0.80f, true);
CarpetCleaning.StoreGifApi.Up(0.12f);
CarpetCleaning.StoreGifApi.Tool(5);
CarpetCleaning.StoreGifApi.Stroke(0.50f, 0.26f, 1.12f, 0.26f, 1.40f);
CarpetCleaning.StoreGifApi.Move(1.12f, 0.26f, 1.42f, 0.275f, 0.40f, false);
CarpetCleaning.StoreGifApi.Away(1.00f);
return CarpetCleaning.StoreGifApi.Run(3000);
