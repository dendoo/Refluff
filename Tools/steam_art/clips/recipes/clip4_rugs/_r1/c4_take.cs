// clip4_rugs TAKE r1 (real EditorApplication.Step frames, every 2 Steps = 25 fps, full 3840x2160 frame, pointer off the play
// area the whole time = no head). Take T / clip t = T - 0.4 (make_clips --fade-loop 0.4):
//  T 0-1.4    the spotless Transylvanian medallion at rest (0-0.4 = the loop's crossfade target)
//  T 1.4      HandoverNow(false): real hand-over (pays), sparkle burst, FlyOut 0.85 s up and out of the top of the frame
//  T 2.8      after 0.55 s of bare tile: TakeNewCarpetNow + the incoming carpet forced to level 0.95 (its own seed 15 mix:
//             dust 0.88, mud 0.39, crust 0.27, spill 0.35) in the same Call, FlyIn 0.95 s from the top
//  T 3.75-    the dirty Anatolian star settled, held
var sim = CarpetCleaning.CarpetDebugApi.Sim;
CarpetCleaning.StoreGifApi.SeqBegin("c4_rugs_r1", 3840, 2160);
CarpetCleaning.StoreGifApi.Away(1.40f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.CarpetDebugApi.HandoverNow(false));
CarpetCleaning.StoreGifApi.Away(1.40f);
CarpetCleaning.StoreGifApi.Call(() => { CarpetCleaning.CarpetDebugApi.TakeNewCarpetNow(); var s = CarpetCleaning.CarpetDebugApi.Sim; s.ResetCarpet(s.Seed, false, 0.95f); });
CarpetCleaning.StoreGifApi.Away(3.65f);
return CarpetCleaning.StoreGifApi.Run(3000);
