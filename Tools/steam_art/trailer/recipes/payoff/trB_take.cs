// Take B (scene only): 12 rest frames at 10 fps (plates for #14's clean side, #20, #21, #03), 0.6 s rest at 60 fps, then the
// real hand-over sparkle burst (CelebrationFx.DebugSeek, seed 12345 = deterministic, Spotless + Bone dry combo count) played
// per Step for 1.2 s, then all dead. #14 = offline wipe dirty (take A rest frame) -> clean, the burst at its end.
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr14_clean", 3840, 2160, 0, 0, 0, 0, false, 6, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Away(1.20f);
CarpetCleaning.StoreGifApi.Every(1);
CarpetCleaning.StoreGifApi.Away(0.60f);
CarpetCleaning.StoreGifApi.Each(1.20f, t => CarpetCleaning.EconomyDebugApi.SparkleSeek(1f, 1f, t));
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.EconomyDebugApi.SparkleSeek(1f, 1f, 5f));
CarpetCleaning.StoreGifApi.Away(0.30f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
