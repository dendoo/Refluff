// clip2_water r2 TAKE (real EditorApplication.Step frames through StoreGifApi's scripted pointer, a frame every 2 Steps = 25 fps).
// Fresh dry dusty rya (the stage.cs profile), an uncaptured warm-up that parks the nozzle below-left of the frame, then:
//   AWAY0 s no tool (the dry rug: the loop dissolve lands here); the nozzle glides in from below-left, pauses, sprays a
//   right-button (half pressure) sweep along u (SPRAYS s: ~0.39 m of wet patch, closed inside the frame), rests (the sheet
//   blooms and turns to mud), lifts out below-right; off the play area 0.10 s, foam sprayer; the ring glides in from
//   below-left, heaps a mound on the left of the patch (DAB1 s), glides right and heaps a second (DAB2 s), glides out
//   below-right; HOLD s no tool (the sheet soaks into the pile, the tide line darkens round the foam).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.ResetWithProfile(68, 0.36f, 0f, 0f, 1f, 0f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
float cu = 0.50f, cv = SPRAYV;                        // centre of the patch
float d1u = D1U, d1v = D1V, d2u = D2U, d2v = D2V;     // the two foam dabs
string w = CarpetCleaning.StoreGifApi.SeqBegin("c2_warm", 1280, 720, 0, 0, 0, 0, false, 100000);
CarpetCleaning.StoreGifApi.Tool(2);
CarpetCleaning.StoreGifApi.Hover(0.30f, -0.03f, 0.30f);
string wr = CarpetCleaning.StoreGifApi.Run(3000);
if (!wr.Contains("\"done\":true")) return "warm-up not done: " + wr;
string we = CarpetCleaning.StoreGifApi.SeqEnd(2000);
string r = CarpetCleaning.StoreGifApi.SeqBegin("TAKE", W, H, CX, CY, CW, CH);
CarpetCleaning.StoreGifApi.Away(AWAY0);                                                  // dry rug, no tool
CarpetCleaning.StoreGifApi.Move(0.30f, -0.03f, 0.40f, 0.27f, 0.35f, false);             // nozzle glides in ...
CarpetCleaning.StoreGifApi.Move(0.40f, 0.27f, cu - 0.07f, cv, 0.22f, false);            // ... and settles
CarpetCleaning.StoreGifApi.Hover(cu - 0.07f, cv, 0.15f);
CarpetCleaning.StoreGifApi.Move(cu - 0.07f, cv, cu + 0.07f, cv, SPRAYS, true, true);     // right-button sweep: the water
CarpetCleaning.StoreGifApi.Up(REST);                                                     // rests: the sheet blooms into mud
CarpetCleaning.StoreGifApi.Move(cu + 0.07f, cv, 0.68f, 0.13f, 0.20f, false);            // lifts out below-right
CarpetCleaning.StoreGifApi.Move(0.68f, 0.13f, 0.76f, -0.04f, 0.15f, false);
CarpetCleaning.StoreGifApi.Away(0.10f);                                                  // off the play area: switch unseen
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Move(0.30f, -0.04f, 0.41f, 0.25f, 0.25f, false);             // foam ring glides in below-left
CarpetCleaning.StoreGifApi.Move(0.41f, 0.25f, d1u, d1v, 0.17f, false);
CarpetCleaning.StoreGifApi.Hold(d1u, d1v, DAB1);                                         // first mound heaps up
CarpetCleaning.StoreGifApi.Move(d1u, d1v, d2u, d2v, GLIDE, false);                       // glides right (button up)
CarpetCleaning.StoreGifApi.Hold(d2u, d2v, DAB2);                                         // second mound
CarpetCleaning.StoreGifApi.Move(d2u, d2v, 0.68f, 0.13f, 0.22f, false);                  // ring glides out below-right
CarpetCleaning.StoreGifApi.Move(0.68f, 0.13f, 0.76f, -0.04f, 0.15f, false);
CarpetCleaning.StoreGifApi.Away(HOLD);                                                   // the sheet soaks in round the foam
return wr + " | " + we + " | " + r + " | " + CarpetCleaning.StoreGifApi.Run(2500);
