// clip4_rugs r2 TAKE A (real EditorApplication.Step frames, every 2 Steps = 25 fps, full 3840x2160 frame, pointer off the play
// area the whole time = no head). Job 1: the spotless Transylvanian medallion is handed over and flies out; the Anatolian star
// (the game's next carpet, seed 15, forced to dirt level 0.95 = its own mix: dust 0.88, mud 0.39, crust 0.27, spill 0.35) is
// taken the very Step the stage is Away (no bare-tile wait) and flies in dirty; it lands and is held.
//  T 0-0.6    spotless at rest (clip start = poster)
//  T 0.6      HandoverNow(false): real hand-over (Economy pays), Spotless + Bone dry sparkle burst, FlyOut 0.85 s
//  T ~1.46    state Away -> TakeNewCarpetNow + dirt 0.95 in the same Step hook, FlyIn 0.95 s from above the top edge
//  T ~2.42    landed; held to T 3.30
CarpetCleaning.StoreGifApi.SeqBegin("c4_rugs_r2a", 3840, 2160);
CarpetCleaning.StoreGifApi.Away(0.60f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.CarpetDebugApi.HandoverNow(false));
CarpetCleaning.StoreGifApi.Each(0.90f, t =>
{
    var h = CarpetCleaning.CarpetDebugApi.Handover;
    if (h.State != CarpetCleaning.HandoverState.Away) return;
    CarpetCleaning.CarpetDebugApi.TakeNewCarpetNow();
    var s = CarpetCleaning.CarpetDebugApi.Sim;
    s.ResetCarpet(s.Seed, false, 0.95f);
});
CarpetCleaning.StoreGifApi.Away(1.80f);
return CarpetCleaning.StoreGifApi.Run(3000);
