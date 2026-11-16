// clip4_rugs r3 TAKE B (as take A). Job 2: the spotless Anatolian star is handed over and flies out; the Transylvanian medallion
// comes back dirty: seed 14's own mix at level 0.95 (dust 0.50, mud 0.48, spill 0.19) with crust forced to 0.30 (seed 14 rolls
// none; its plates sit left / centre / bottom, unlike seed 15's right-side ones on the Anatolian star). It lands and is held;
// the clip then hard-cuts to take A's first frame = the same Transylvanian rug spotless (seed 14, level 0).
CarpetCleaning.StoreGifApi.SeqBegin("c4_rugs_r3b", 3840, 2160);
CarpetCleaning.StoreGifApi.Away(0.60f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.CarpetDebugApi.HandoverNow(false));
CarpetCleaning.StoreGifApi.Each(0.90f, t =>
{
    var h = CarpetCleaning.CarpetDebugApi.Handover;
    if (h.State != CarpetCleaning.HandoverState.Away) return;
    CarpetCleaning.CarpetDebugApi.TakeNewCarpetNow();
    var d = CarpetCleaning.CarpetSimulation.RollDirt(14, 0.95f);
    d.crust = 0.30f;
    CarpetCleaning.CarpetDebugApi.Sim.ResetCarpet(14, false, d);
});
CarpetCleaning.StoreGifApi.Away(1.80f);
return CarpetCleaning.StoreGifApi.Run(3000);
