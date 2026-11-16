// clip1_reveal TAKE r3 (real EditorApplication.Step frames, scripted pointer). Two horizontal wet-vac pulls at 250 mm/s:
// row 1 (v 0.30, left -> right); the wand lifts and swings OUT of the frame over the right floor, the pointer leaves the play
// area for 0.10 s (Away: no head) and the wand swings back IN one row up; row 2 (v 0.42, right -> left); then it lifts and
// leaves the frame to the left and the clean band holds 2.3 s. The turn happens off frame on purpose: the head's heading is
// an axis (ToolController.UpdateHeading), so a pure reversal does not rotate it, but any step ACROSS the rows (v) swings the
// bright wand through 90-180 degrees in view (r2's flail). Off frame + Away = the step across happens with no head; the
// wand leaves pointing along v and comes back pointing along v (only a ~5 degree drift, from the slight v slant of both swings).
// The first 1.3 s of row 1 (u 0.08 -> 0.40) run as an uncaptured warm-up in this same eval (every = 100000: no frames), so
// the take opens mid-pull with the lower-left of the star already out of the slurry.
// Render 3840x2160, capture crop (840,802,2160,900) as in r2; the final framing is make_clips --crop 174,82,1812,755
// (= render 1014,884,1812,755: centred on u 0.5 (render x 1920), 2.4:1, the carpet rims (render x 990 / 2850 at v 0.30,
// 1044 / 2796 at v 0.42) meet the crop edges at mid height, so only two small floor wedges show at the top corners).
string w = CarpetCleaning.StoreGifApi.SeqBegin("c1_warm", 1280, 720, 0, 0, 0, 0, false, 100000);
CarpetCleaning.StoreGifApi.Tool(6);
CarpetCleaning.StoreGifApi.Hover(0.06f, 0.30f, 0.2f);
CarpetCleaning.StoreGifApi.Stroke(0.08f, 0.30f, 0.40f, 0.30f, 1.28f);
string wr = CarpetCleaning.StoreGifApi.Run(3000);
if (!wr.Contains("\"done\":true")) return "warm-up not done: " + wr;
string we = CarpetCleaning.StoreGifApi.SeqEnd(2000);
string r = CarpetCleaning.StoreGifApi.SeqBegin("c1_reveal_r3", 3840, 2160, 840, 802, 2160, 900);
CarpetCleaning.StoreGifApi.Move(0.40f, 0.30f, 0.90f, 0.30f, 2.40f, true);       // row 1 continues: 600 mm at 250 mm/s
CarpetCleaning.StoreGifApi.Move(0.90f, 0.30f, 0.98f, 0.306f, 0.26f, false);     // lift, swing out (370 mm/s)
CarpetCleaning.StoreGifApi.Move(0.98f, 0.306f, 1.12f, 0.316f, 0.24f, false);    // ... past the rim, out of frame (700 mm/s)
CarpetCleaning.StoreGifApi.Away(0.10f);                                           // off the play area: the step across the rows
CarpetCleaning.StoreGifApi.Move(1.14f, 0.404f, 0.98f, 0.416f, 0.30f, false);    // swing back in one row up (640 mm/s)
CarpetCleaning.StoreGifApi.Move(0.98f, 0.416f, 0.905f, 0.42f, 0.26f, false);    // ... settling onto row 2 (350 mm/s)
CarpetCleaning.StoreGifApi.Move(0.905f, 0.42f, 0.10f, 0.42f, 3.86f, true);      // row 2: 966 mm at 250 mm/s
CarpetCleaning.StoreGifApi.Move(0.10f, 0.42f, 0.085f, 0.422f, 0.16f, false);    // lift, still along the row (r2's 0.09,0.43 tilted the wand 30 deg)
CarpetCleaning.StoreGifApi.Move(0.085f, 0.422f, -0.35f, 0.46f, 0.56f, false);   // the wand leaves the frame to the left
CarpetCleaning.StoreGifApi.Away(2.30f);                                           // the clean band holds
return wr + " | " + we + " | " + r + " | " + CarpetCleaning.StoreGifApi.Run(2500);
