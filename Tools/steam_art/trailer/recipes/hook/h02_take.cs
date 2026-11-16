// #02 (0:02.7-0:05.3): the rotary brush churns fresh pink foam on lane A of the fully soiled, hosed Transylvanian rug; the
// foam swirls and greys (Fd/F > 0.04). Brush AREA 6 (pad r 78 mm) so the blue bristles stand clear of the foam (clip 3).
// The foam slab is laid uncaptured (the sprayer has no head model), one half-pressure pass along the lane.
CarpetCleaning.EconomyDebugApi.SetLevel(4, 1, 6);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.58f,0.217f, 0.92f,0.118f, 48, 0.5f);
CarpetCleaning.CarpetDebugApi.SetTool(4);
var S = CarpetCleaning.StoreGifApi.SeqBegin("t02_swirl", 3840, 2160, 1280, 800, 2176, 1360, false, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(4);
CarpetCleaning.StoreGifApi.Hover(0.90f, 0.124f, 0.25f);
CarpetCleaning.StoreGifApi.Move(0.90f,0.124f, 0.62f,0.205f, 0.80f, true);
CarpetCleaning.StoreGifApi.Move(0.62f,0.205f, 0.88f,0.135f, 0.80f, true);
CarpetCleaning.StoreGifApi.Move(0.88f,0.135f, 0.63f,0.200f, 0.80f, true);
CarpetCleaning.StoreGifApi.Move(0.63f,0.200f, 0.87f,0.138f, 0.80f, true);
CarpetCleaning.StoreGifApi.Up(0.30f);
CarpetCleaning.StoreGifApi.Away(0.30f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
