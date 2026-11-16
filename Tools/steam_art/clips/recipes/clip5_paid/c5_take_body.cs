// clip5_paid TAKE (real EditorApplication.Step frames, every 2 Steps = 25 fps, FULL 2560x1440 frame + the UI Toolkit panel;
// the virtual camera crops are applied offline by c5_frames.py). Pointer off the play area the whole time (no head).
// Take time T (card time c = T - 0.80):
//  T 0.00-0.80  the spotless European green rug at rest, "Sparkling clean - hand it over!", clock 3:08 (clip starts at T 0.40)
//  T 0.80       HandoverNow(false): a real hand-over (Economy pays: base $35, speed $25, spotless $20, bone dry $10, combo $30
//               = $120, wallet $1,840 -> $1,960), the rug flies out; the pay slip card drops at c 0.30 and prints line by line
//  T 7.30       (c 6.50, after the last coin landed ~6.4) the player's click: PayoutView.Skip() = the card's 0.30 s exit
//  T 7.66       ShopOpen(true) (the U key): 0.22 s panel entrance
//  T 8.30       ShopBuy(4, 0) Brush POWER (a real click on the row: pip pop, sparkles, wallet roll)
//  T 9.20       ShopBuy(6, 1) Wet vacuum AREA
//  T 10.10      end
CarpetCleaning.StoreGifApi.SeqBegin(TAKE, 2560, 1440, 0, 0, 0, 0, true);
CarpetCleaning.StoreGifApi.Away(0.80f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.CarpetDebugApi.HandoverNow(false));
CarpetCleaning.StoreGifApi.Away(6.50f);
CarpetCleaning.StoreGifApi.Call(() =>
{
    var bfp = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    var pv = typeof(CarpetCleaning.EconomyHud).GetField("_payout", bfp).GetValue(CarpetCleaning.EconomyDebugApi.Hud);
    pv.GetType().GetMethod("Skip").Invoke(pv, new object[] { false });
});
CarpetCleaning.StoreGifApi.Away(0.36f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.EconomyDebugApi.ShopOpen(true));
CarpetCleaning.StoreGifApi.Away(0.64f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.StoreGifApi.ShopBuy(4, 0));
CarpetCleaning.StoreGifApi.Away(0.90f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.StoreGifApi.ShopBuy(6, 1));
CarpetCleaning.StoreGifApi.Away(0.90f);
return CarpetCleaning.StoreGifApi.Run(3000);
