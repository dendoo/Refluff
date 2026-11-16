// Take C (HUD, 3840x2160 + 2560x1440 panel, 60 fps): dressing + take in ONE eval (a Reset after the dressing would recompute
// _initStats and pay $0). Dressing (clip 5): job soil V 0.302 "Medium soil", par 3:32, clock anchored so it reads 3:08 at the
// hand-over -> the pay slip prints base 35 + speed 25 + Spotless 20 + Bone dry 10 + COMBO 30 = $120.
//  T 0-0.70   spotless star at rest, "Sparkling clean - hand it over!", 100 % / 100 %, $1,840        (#15 from T 0.10)
//  T 0.70     HandoverNow(false): a real hand-over, gold + white burst, FlyOut; card drops ~T 1.00
//  T 2.77     (#16) the card from c0 on runs 1.6x via PayoutSeek (0.3 s ramp): bone dry, COMBO stamp, $120, coins -> wallet
//  T 5.67     PayoutResume; T 5.77 ShopOpen (the card finishes, wallet $1,960); T 6.52 ShopBuy(4,0) brush POWER $75 -> $1,885
//  T 8.52     end                                                                                       (#17 T 5.77-8.44)
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var eco = CarpetCleaning.EconomyDebugApi.Eco; var sim = CarpetCleaning.CarpetDebugApi.Sim;
var fi = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", bf);
object st = fi.GetValue(sim);
st.GetType().GetField("mud").SetValue(st, 0.22f);
st.GetType().GetField("grit").SetValue(st, 0.10f);
st.GetType().GetField("stain").SetValue(st, 0.06f);
st.GetType().GetField("valid").SetValue(st, true);
fi.SetValue(sim, st);
typeof(CarpetCleaning.Economy).GetField("_clockAnchor", bf).SetValue(eco, sim.SimTime - 187.6f);
var S = CarpetCleaning.StoreGifApi.SeqBegin("tr15_payoff", 3840, 2160, 0, 0, 0, 0, true, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Away(0.70f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.CarpetDebugApi.HandoverNow(false));
CarpetCleaning.StoreGifApi.Away(2.07f);
float c0 = 0f;
CarpetCleaning.StoreGifApi.Call(() => c0 = CarpetCleaning.EconomyDebugApi.Hud.Payout.T);
const float R = 1.6f, RAMP = 0.3f;
CarpetCleaning.StoreGifApi.Each(2.90f, t => CarpetCleaning.EconomyDebugApi.PayoutSeek(
    c0 + t + (R - 1f) * (t < RAMP ? t * t / (2f * RAMP) : t - RAMP * 0.5f)));
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.EconomyDebugApi.PayoutResume());
CarpetCleaning.StoreGifApi.Away(0.10f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.EconomyDebugApi.ShopOpen(true));
CarpetCleaning.StoreGifApi.Away(0.75f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.StoreGifApi.ShopBuy(4, 0));
CarpetCleaning.StoreGifApi.Away(2.00f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
