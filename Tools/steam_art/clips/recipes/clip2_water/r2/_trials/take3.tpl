// clip2_water r2 TAKE (real EditorApplication.Step frames through StoreGifApi's scripted pointer, a frame every 2 Steps = 25 fps).
// Fresh dry dusty rya (the stage.cs profile), an uncaptured warm-up that parks the nozzle below-left of the frame, then:
//   AWAY0 s no tool (the dry rug; the loop dissolve lands on its first 0.4 s); the nozzle glides in from below-left and
//   pauses; two right-button (half pressure) sprays of SPRAY s, left (u cu-DU) then right (u cu+DU), with a lifted glide
//   between: two blooms that merge into one wide muddy patch (~0.54 x 0.35 m, closed inside the frame); the nozzle rests,
//   then lifts out below-right; off the play area 0.10 s, foam sprayer; the ring glides in from below-left and heaps one
//   mound in the middle (DAB s), rests on it, glides out below-right; HOLD s no tool (the sheets soak into the pile).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.ResetWithProfile(68, 0.36f, 0f, 0f, 1f, 0f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
float cu = 0.50f, cv = SPRAYV, du = DU;               // the two spray spots: (cu -/+ du, cv)
float mu = MU, mv = MV;                               // the foam mound
string w = CarpetCleaning.StoreGifApi.SeqBegin("c2_warm", 1280, 720, 0, 0, 0, 0, false, 100000);
CarpetCleaning.StoreGifApi.Tool(2);
CarpetCleaning.StoreGifApi.Hover(0.28f, -0.03f, 0.30f);
string wr = CarpetCleaning.StoreGifApi.Run(3000);
if (!wr.Contains("\"done\":true")) return "warm-up not done: " + wr;
string we = CarpetCleaning.StoreGifApi.SeqEnd(2000);
string r = CarpetCleaning.StoreGifApi.SeqBegin("TAKE", W, H, CX, CY, CW, CH);
CarpetCleaning.StoreGifApi.Away(AWAY0);                                                  // dry rug, no tool
CarpetCleaning.StoreGifApi.Move(0.28f, -0.03f, cu - du - 0.03f, 0.26f, 0.40f, false);   // nozzle glides in ...
CarpetCleaning.StoreGifApi.Move(cu - du - 0.03f, 0.26f, cu - du, cv, 0.25f, false);     // ... and settles
CarpetCleaning.StoreGifApi.Hover(cu - du, cv, 0.15f);
CarpetCleaning.StoreGifApi.Move(cu - du, cv, cu - du, cv, SPRAY, true, true);    // spray 1: right button = half pressure
CarpetCleaning.StoreGifApi.Move(cu - du, cv, cu + du, cv, 0.35f, false);                // lifted glide right
CarpetCleaning.StoreGifApi.Move(cu + du, cv, cu + du, cv, SPRAY, true, true);    // spray 2
CarpetCleaning.StoreGifApi.Up(REST);                                                     // rests: the blooms merge into mud
CarpetCleaning.StoreGifApi.Move(cu + du, cv, 0.70f, 0.13f, 0.22f, false);               // lifts out below-right
CarpetCleaning.StoreGifApi.Move(0.70f, 0.13f, 0.78f, -0.04f, 0.18f, false);
CarpetCleaning.StoreGifApi.Away(0.10f);                                                  // off the play area: switch unseen
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Move(0.30f, -0.04f, mu - 0.06f, 0.26f, 0.27f, false);        // foam ring glides in below-left
CarpetCleaning.StoreGifApi.Move(mu - 0.06f, 0.26f, mu, mv, 0.18f, false);
CarpetCleaning.StoreGifApi.Hold(mu, mv, DAB);                                            // the mound heaps up
CarpetCleaning.StoreGifApi.Hover(mu, mv, 0.15f);
CarpetCleaning.StoreGifApi.Move(mu, mv, 0.68f, 0.14f, 0.22f, false);                    // ring glides out below-right
CarpetCleaning.StoreGifApi.Move(0.68f, 0.14f, 0.76f, -0.04f, 0.18f, false);
CarpetCleaning.StoreGifApi.Away(HOLD);                                                   // the sheets soak into the pile
return wr + " | " + we + " | " + r + " | " + CarpetCleaning.StoreGifApi.Run(2500);
