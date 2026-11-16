// clip2_water r2 TAKE (real EditorApplication.Step frames through StoreGifApi's scripted pointer, a frame every 2 Steps = 25 fps).
// Fresh dry dusty rya (the stage.cs profile), an uncaptured warm-up that parks the nozzle below-left of the frame, then:
//   AWAY0 s no tool (the dry rug: the loop dissolve lands on its first 0.5 s); the nozzle glides in from below-left and
//   pauses; three right-button (half pressure) bursts of BURST s, left / middle / right (u 0.5-DU, 0.5, 0.5+DU), with lifted
//   glides between: three small blooms that merge into one wide muddy patch (~0.52 x 0.34 m, closed inside the frame, dry
//   rug above and below); the nozzle rests, then lifts out below-right; off the play area 0.10 s, foam sprayer; the ring
//   glides in from below and heaps one mound in the middle (DAB s), rests on it, glides out below-right; HOLD s no tool.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.ResetWithProfile(68, 0.36f, 0f, 0f, 1f, 0f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
float du = 0.09f, v1 = 0.35f, v2 = 0.338f, v3 = 0.35f;            // burst spots (0.5-du, v1), (0.5, v2), (0.5+du, v3)
float mu = 0.50f, mv = 0.35f;                               // the foam mound
float b = 0.20f, g = 0.25f;
string w = CarpetCleaning.StoreGifApi.SeqBegin("c2_warm", 1280, 720, 0, 0, 0, 0, false, 100000);
CarpetCleaning.StoreGifApi.Tool(2);
CarpetCleaning.StoreGifApi.Hover(0.28f, -0.03f, 0.30f);
string wr = CarpetCleaning.StoreGifApi.Run(3000);
if (!wr.Contains("\"done\":true")) return "warm-up not done: " + wr;
string we = CarpetCleaning.StoreGifApi.SeqEnd(2000);
string r = CarpetCleaning.StoreGifApi.SeqBegin("c2_water_r2", 3840, 2160, 960, 890, 1920, 800);
CarpetCleaning.StoreGifApi.Away(0.50f);                                                  // dry rug, no tool
CarpetCleaning.StoreGifApi.Move(0.28f, -0.03f, 0.47f - du, v1 - 0.08f, 0.40f, false);   // nozzle glides in ...
CarpetCleaning.StoreGifApi.Move(0.47f - du, v1 - 0.08f, 0.5f - du, v1, 0.25f, false);   // ... and settles
CarpetCleaning.StoreGifApi.Hover(0.5f - du, v1, 0.12f);
CarpetCleaning.StoreGifApi.Move(0.5f - du, v1, 0.5f - du, v1, b, true, true);           // burst 1 (right button: half pressure)
CarpetCleaning.StoreGifApi.Move(0.5f - du, v1, 0.5f, v2, g, false);                     // lifted glide
CarpetCleaning.StoreGifApi.Move(0.5f, v2, 0.5f, v2, b, true, true);                     // burst 2
CarpetCleaning.StoreGifApi.Move(0.5f, v2, 0.5f + du, v3, g, false);
CarpetCleaning.StoreGifApi.Move(0.5f + du, v3, 0.5f + du, v3, b, true, true);           // burst 3
CarpetCleaning.StoreGifApi.Up(0.35f);                                                     // rests: the blooms merge into mud
CarpetCleaning.StoreGifApi.Move(0.5f + du, v3, 0.70f, 0.13f, 0.20f, false);             // lifts out below-right
CarpetCleaning.StoreGifApi.Move(0.70f, 0.13f, 0.78f, -0.04f, 0.15f, false);
CarpetCleaning.StoreGifApi.Away(0.10f);                                                  // off the play area: switch unseen
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Move(0.56f, -0.05f, mu + 0.02f, mv - 0.10f, 0.25f, false);   // foam ring glides in from below
CarpetCleaning.StoreGifApi.Move(mu + 0.02f, mv - 0.10f, mu, mv, 0.15f, false);
CarpetCleaning.StoreGifApi.Hold(mu, mv, 0.90f);                                            // the mound heaps up
CarpetCleaning.StoreGifApi.Hover(mu, mv, 0.15f);
CarpetCleaning.StoreGifApi.Move(mu, mv, 0.68f, 0.14f, 0.22f, false);                    // ring glides out below-right
CarpetCleaning.StoreGifApi.Move(0.68f, 0.14f, 0.76f, -0.04f, 0.18f, false);
CarpetCleaning.StoreGifApi.Away(1.6f);                                                   // the sheets soak into the pile
return wr + " | " + we + " | " + r + " | " + CarpetCleaning.StoreGifApi.Run(2500);
