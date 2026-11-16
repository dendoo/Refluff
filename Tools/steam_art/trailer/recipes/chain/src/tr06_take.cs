// #06 SCRAPE (0:13.3-0:16.0): 4 pressed scraper legs over the dried-mud slabs of the band centre (clip 3 pattern, +-0.002 v
// wobble so the heading never swings), 870 mm/s; shards hop onto the right tile. Edit window ~0.30-2.97 s.
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr06_scrape", 3840, 2160, 480, 360, 3360, 1800, false, 1, true, 2560, 1440, 60, true);
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
