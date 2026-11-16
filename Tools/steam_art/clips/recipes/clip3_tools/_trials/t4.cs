// clip3 take t4: horizontal lane v 0.26, u 0.62-0.98 (the right rim is u 1.0) through the crust of seed 154 @0.6
// (mud forced to 0.1). Scraper 4 fast passes, a light foam pass (right button), a rotary-brush scrub inside the lane, the
// squeegee is carried lifted to the lane's left end and pushes the lot off the right rim onto the tile, then leaves.
CarpetCleaning.EconomyDebugApi.SetLevel(4, 1, 6);
CarpetCleaning.CarpetDebugApi.ResetWithProfile(154, 0.6f, -1f, 0.1f, -1f, -1f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(0);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
CarpetCleaning.StoreGifApi.SeqBegin("t4", 3840, 2160, 1560, 1088, 1680, 700);
CarpetCleaning.StoreGifApi.Tool(0);
CarpetCleaning.StoreGifApi.Hover(0.585f, 0.26f, 0.20f);
CarpetCleaning.StoreGifApi.Stroke(0.62f, 0.26f, 0.98f, 0.26f, 0.50f);
CarpetCleaning.StoreGifApi.Move(0.98f, 0.26f, 0.62f, 0.262f, 0.50f, true);
CarpetCleaning.StoreGifApi.Move(0.62f, 0.262f, 0.98f, 0.258f, 0.50f, true);
CarpetCleaning.StoreGifApi.Move(0.98f, 0.258f, 0.62f, 0.26f, 0.50f, true);
CarpetCleaning.StoreGifApi.Up(0.12f);
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Stroke(0.62f, 0.26f, 0.97f, 0.26f, 0.60f, 40f, true);
CarpetCleaning.StoreGifApi.Up(0.12f);
CarpetCleaning.StoreGifApi.Tool(4);
CarpetCleaning.StoreGifApi.Move(0.97f, 0.26f, 0.64f, 0.24f, 0.70f, true);
CarpetCleaning.StoreGifApi.Move(0.64f, 0.24f, 0.95f, 0.28f, 0.70f, true);
CarpetCleaning.StoreGifApi.Move(0.95f, 0.28f, 0.66f, 0.26f, 0.70f, true);
CarpetCleaning.StoreGifApi.Up(0.10f);
CarpetCleaning.StoreGifApi.Tool(5);
CarpetCleaning.StoreGifApi.Move(0.66f, 0.26f, 0.49f, 0.26f, 0.30f, false);
CarpetCleaning.StoreGifApi.Move(0.49f, 0.26f, 1.12f, 0.26f, 1.40f, true);
CarpetCleaning.StoreGifApi.Move(1.12f, 0.26f, 1.42f, 0.275f, 0.40f, false);
CarpetCleaning.StoreGifApi.Away(1.30f);
return CarpetCleaning.StoreGifApi.Run(3000);
