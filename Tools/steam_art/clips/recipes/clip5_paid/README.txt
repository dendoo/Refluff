clip5_paid (2026-09-26, round 2 = round 1's take, new virtual camera): Steam "About This Game" clip 5 = "Get paid, upgrade your kit". 1560x878 (16:9), 25 fps,
9.68 s (242 frames), 2.24 MB (crf 24, preset veryslow; crf 22 was 2.81 MB), muted loop, HUD clip, one continuous take.
Final: SteamStore/description/refluff_clip5_paid.mp4 (+ _poster.png = first frame, _sheet.png).
Takes: SteamStore/_work/clips/c5_paid_r1/ (252 frames: f####.png scene + f####_ui.png panel, full 2560x1440, seq.json) and
c5_paid_r1_cam/ (the same 252 frames after the virtual camera, 1560x878, scene-only take for make_clips).
Re-run: bash run.sh (~1.5 min).

Staging (c5_stage.cs, fast calls, as reshoot_ho e2_setup = screenshot 04 10_payslip): shop closed, card resumed, lamps off,
pre-kit levels power {1,2,1,1,2,0,1,0,0,0} / area {1,1,0,1,1,1,1,0,0,0}, both lamps owned, MarketingHud(true), UiPaused false,
wallet Grant to $1,840, Data.carpetsDelivered = 6 (slip #7), NewCarpetNow until rug_european_green (not used by clips 1-4),
HandoverSnap, Reset(14, 0f) (spotless + dry), LiveTuning(false), SetTool(7) (dryer: no head), 30 warm-up Steps with the
scripted pointer off the play area (paused Steps must not read the OS mouse).
Take (c5_take.cs = build_take.py(header + c5_dress.cs + c5_take_body.cs); c5_dress = e3_pay's HUD dressing in the SAME eval:
_initStats mud 0.22 / grit 0.10 / stain 0.06 -> V 0.302 "Medium soil", par 3:32; Economy._clockAnchor = SimTime - 187.6):
SeqBegin("c5_paid_r1", 2560, 1440, full frame, hud: true), every 2 Steps; take time T:
  T 0.00-0.80  Away: spotless rug at rest, "Sparkling clean - hand it over!", 100 % / 100 %, 3:08 / par 3:32, $1,840, Hand over
  T 0.80       Call HandoverNow(false): real hand-over, the rug flies out with the Spotless + Bone dry sparkles; the card drops
               at card time 0.30 and prints: Base $35, Speed $25, Spotless $20, Bone dry $10, COMBO stamp $30, TOTAL $120, 14 coins
               fly to the wallet ($1,840 -> $1,960); udtMean = 0.02 per Step, so the card plays in real time (no PayoutSeek)
  T 7.30       Call PayoutView.Skip() by reflection (EconomyHud._payout) = the player's click: the card's 0.30 s exit
  T 7.66       Call ShopOpen(true) (the U key): 0.22 s panel entrance, scene dimmed
  T 8.30       Call StoreGifApi.ShopBuy(4, 0) Brush POWER $75 (pip pop, ring, wallet roll $1,960 -> $1,885)
  T 9.20       Call StoreGifApi.ShopBuy(6, 1) Wet vacuum AREA $65 ($1,885 -> $1,820)
  T 10.10      end (505 Steps, 252 frames; Step ~8 ms, capture ~61 ms). c5_after closes the shop.
Virtual camera (c5_frames.py, offline; the game camera is fixed, this is a crop window over the full-frame HUD capture,
composited exactly like make_clips, resampled Lanczos in linear light with a sub-pixel window, smootherstep, size geometric):
  FULL (0,0,2560,1440) to T 0.98 -> push in until T 1.66 -> CARD (640,122,1280,720) until T 5.94 -> pull out until T 6.50
  -> WALLET (0,0,1828,1028) until T 7.36 -> zoom out until T 8.06 -> FULL. Round 2 (art director): round 1 held CARD to
  T 7.24, so the coins flew off into nothing, the wallet count $1,840 -> $1,960 (T 6.4-7.44) was never seen and the card
  exit left 3 blank tile frames (f0189-f0191). WALLET = stats panel + message pill + card + tool list, 32 px margins; at
  780 px the wallet digits roll legibly, the coins cross the frame, the card and $120 still read, and the HUD stays in shot
  through the card exit.
  CARD at 780 px = 0.61x of the panel: every slip line and amount is legible. The shop stays FULL: any tighter 16:9 window
  that holds the whole panel slices the dimmed HUD (top-left wallet pill or the Upgrades button); FULL also makes the loop a
  plain dissolve.
Encode: make_clips --fade-loop 0.4 --crf 24 --preset veryslow: the clip starts at T 0.40 (poster = the spotless rug, full HUD),
the last 0.4 s of the shop dissolves into T 0-0.4. Clip beats: 0-0.4 rug at rest | 0.4-1.3 flight + card drop, push in |
1.3-5.5 slip prints (lines 1.25-2.9, COMBO 3.7, total 4.7-5.5) | 5.5-6.1 coins burst, pull out to WALLET |
6.0-7.04 wallet counts up | 6.96-7.66 card out, zoom out, shop in |
7.9 / 8.8 the two buys | 9.28-9.68 dissolve to the start.
Known nits (not capture-fixable, as screenshot 08): shop price labels sit at the top of the pills, floor-tool key chips show '.'.
Budget: verify_assets says description/ total 17.37 MB > 15 MB with clips 1-5 (round 1 2.63 MB, round 2 2.81 MB; 14.73 MB without clip 5). Package step: clips 1/2/3/5 re-encoded at preset veryslow (crf 16/20/22/24) -> total 14.66 MB, ALL OK.
