// Take A = #04 (0:08.0-0:10.7, HUD) + #05 (0:10.7-0:13.3, scene only) + the #14 dirty plate: bare tile + Away HUD, then the
// Anatolian star with seed 394 @0.9 dirt (the chain's rug: dust .65 mud .91 crust .61 spill .89) flies in and lands; then a
// 10 fps static rest for the #05 pan and the #14 plate. Pointer away the whole take (no head, no ring).
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr04_arrival", 3840, 2160, 0, 0, 0, 0, true, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Away(0.40f);
CarpetCleaning.StoreGifApi.Call(() => {
    var h = CarpetCleaning.CarpetDebugApi.Handover;
    if (h.State != CarpetCleaning.HandoverState.Away) throw new System.Exception("not Away: " + h.State);
    CarpetCleaning.CarpetDebugApi.TakeNewCarpetNow();
    CarpetCleaning.CarpetDebugApi.Sim.ResetCarpet(394, false, 0.9f);
});
CarpetCleaning.StoreGifApi.Away(2.60f);
CarpetCleaning.StoreGifApi.Every(6);
CarpetCleaning.StoreGifApi.Away(4.00f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
