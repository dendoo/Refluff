// clip2_water r3 TAKE (real EditorApplication.Step frames through StoreGifApi's scripted pointer, a frame every 2 Steps = 25 fps).
// Same staging, water amount and foam hold as r2 (r2/take_final.cs); r3 changes only the edit and the crop:
//   - crop 1632x680 at (1104, 945) of the 3840x2160 render (2.4:1, the review's 1680x700 at (1080, 900) moved 24 px in and
//     45 px down: no floor wedge in the top corners, the mud patch >= 90 px inside every edge; ~1.18x closer than r2);
//   - the mud patch is held with no tool in view for SOAK s before the foam ring comes in (r2: 0.10 s);
//   - the foam mound settles for SETTLE s at the end; rotate_take.py puts the end -> start dissolve at the loop point
//     (the clip starts on the nozzle arriving over the dry rug, cause -> effect order, no mid-clip reset).
// Timeline: AWAY0 dry rug, nozzle glides in from below-left and pauses; three right-button (half pressure) 0.20 s bursts at
// (0.41,0.35), (0.50,0.338), (0.59,0.35) with lifted 0.25 s glides; rests 0.35 s; lifts out below-right; SOAK s no tool
// (the blooms merge and soak into one dark muddy patch); foam sprayer; the ring glides in from below, heaps one mound at
// (0.50,0.35) for 0.90 s, rests 0.15 s, glides out below-right; SETTLE s no tool (the mound slumps).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.ResetWithProfile(68, 0.36f, 0f, 0f, 1f, 0f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
float du = 0.09f, v1 = 0.35f, v2 = 0.338f, v3 = 0.35f;            // burst spots (0.5-du, v1), (0.5, v2), (0.5+du, v3)
float mu = 0.50f, mv = 0.35f;                                      // the foam mound
float b = 0.20f, g = 0.25f;
float away0 = 0.40f, soak = 1.30f, settle = 1.50f;
string w = CarpetCleaning.StoreGifApi.SeqBegin("c2_warm", 1280, 720, 0, 0, 0, 0, false, 100000);
CarpetCleaning.StoreGifApi.Tool(2);
CarpetCleaning.StoreGifApi.Hover(0.28f, -0.03f, 0.30f);
string wr = CarpetCleaning.StoreGifApi.Run(3000);
if (!wr.Contains("\"done\":true")) return "warm-up not done: " + wr;
string we = CarpetCleaning.StoreGifApi.SeqEnd(2000);
string r = CarpetCleaning.StoreGifApi.SeqBegin("c2_water_r3", 3840, 2160, 1104, 945, 1632, 680);
CarpetCleaning.StoreGifApi.Away(away0);                                                  // dry rug, no tool
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
CarpetCleaning.StoreGifApi.Away(soak);                                                   // the mud patch, no tool in view
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Move(0.56f, -0.05f, mu + 0.02f, mv - 0.10f, 0.25f, false);   // foam ring glides in from below
CarpetCleaning.StoreGifApi.Move(mu + 0.02f, mv - 0.10f, mu, mv, 0.15f, false);
CarpetCleaning.StoreGifApi.Hold(mu, mv, 0.90f);                                            // the mound heaps up
CarpetCleaning.StoreGifApi.Hover(mu, mv, 0.15f);
CarpetCleaning.StoreGifApi.Move(mu, mv, 0.68f, 0.14f, 0.22f, false);                    // ring glides out below-right
CarpetCleaning.StoreGifApi.Move(0.68f, 0.14f, 0.76f, -0.04f, 0.18f, false);
CarpetCleaning.StoreGifApi.Away(settle);                                                 // the mound slumps, no tool
return wr + " | " + we + " | " + r + " | " + CarpetCleaning.StoreGifApi.Run(2500);
