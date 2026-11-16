// clip1_reveal TAKE (real EditorApplication.Step frames, scripted pointer). Two horizontal wet-vac pulls at 250 mm/s:
// row 1 (v 0.30, left -> right), a lifted turn, row 2 (v 0.42, right -> left), then the wand lifts and leaves the frame to the
// left and the clean band holds. The first 1.3 s of row 1 (u 0.08 -> 0.40) run as an uncaptured warm-up in this same eval
// (every = 100000: no frames), so the take opens mid-pull with the lower-left of the star already out of the slurry.
// Render 3840x2160, crop (840,802,2160,900): the band centred (CropAround(0.5, 0.36, 2400, 1000) = (720,736) + (120,66)).
var A = typeof(CarpetCleaning.StoreGifApi);
string w = CarpetCleaning.StoreGifApi.SeqBegin("c1_warm", 1280, 720, 0, 0, 0, 0, false, 100000);
CarpetCleaning.StoreGifApi.Tool(6);
CarpetCleaning.StoreGifApi.Hover(0.06f, 0.30f, 0.2f);
CarpetCleaning.StoreGifApi.Stroke(0.08f, 0.30f, 0.40f, 0.30f, 1.28f);
string wr = CarpetCleaning.StoreGifApi.Run(3000);
if (!wr.Contains("\"done\":true")) return "warm-up not done: " + wr;
string we = CarpetCleaning.StoreGifApi.SeqEnd(2000);
string r = CarpetCleaning.StoreGifApi.SeqBegin("c1_reveal_r2", 3840, 2160, 840, 802, 2160, 900);
CarpetCleaning.StoreGifApi.Move(0.40f, 0.30f, 0.90f, 0.30f, 2.40f, true);      // row 1 continues: 600 mm at 250 mm/s
CarpetCleaning.StoreGifApi.Move(0.90f, 0.30f, 0.905f, 0.42f, 0.40f, false);    // lift and step up to row 2
CarpetCleaning.StoreGifApi.Move(0.905f, 0.42f, 0.10f, 0.42f, 3.86f, true);     // row 2: 966 mm at 250 mm/s
CarpetCleaning.StoreGifApi.Move(0.10f, 0.42f, 0.09f, 0.43f, 0.16f, false);     // lift
CarpetCleaning.StoreGifApi.Move(0.09f, 0.43f, -0.35f, 0.47f, 0.56f, false);    // the wand leaves the frame to the left
CarpetCleaning.StoreGifApi.Away(1.30f);                                          // the clean band holds
return wr + " | " + we + " | " + r + " | " + CarpetCleaning.StoreGifApi.Run(2500);
