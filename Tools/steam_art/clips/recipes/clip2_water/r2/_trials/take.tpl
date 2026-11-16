// clip2_water r2 TAKE (real EditorApplication.Step frames through StoreGifApi's scripted pointer, a frame every 2 Steps = 25 fps).
// Fresh dry dusty rya first (the stage.cs profile), then: an uncaptured warm-up that parks the nozzle below the frame, then
//   0.40 s no tool (the dry rug: the loop dissolve lands here)
//   nozzle glides in from below-right, sprays a short right-button (half pressure) sweep along u (0.50 s), rests, lifts out
//   below-right; off the play area 0.10 s, foam sprayer; the ring glides in from below-left, holds (the mound heaps), circles
//   once round the centre (the mound grows), holds, glides out below-right; HOLD s no tool (the sheet soaks into the pile).
// TAKE / W H / CX CY CW CH are filled in by the caller (take_t.cs = half-res test, take_final.cs = 3840x2160).
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.ResetWithProfile(68, 0.36f, 0f, 0f, 1f, 0f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
float cu = 0.50f, cv = SPRAYV;                       // centre of the patch
string w = CarpetCleaning.StoreGifApi.SeqBegin("c2_warm", 1280, 720, 0, 0, 0, 0, false, 100000);
CarpetCleaning.StoreGifApi.Tool(2);
CarpetCleaning.StoreGifApi.Hover(0.70f, -0.06f, 0.30f);
string wr = CarpetCleaning.StoreGifApi.Run(3000);
if (!wr.Contains("\"done\":true")) return "warm-up not done: " + wr;
string we = CarpetCleaning.StoreGifApi.SeqEnd(2000);
string r = CarpetCleaning.StoreGifApi.SeqBegin("TAKE", W, H, CX, CY, CW, CH);
CarpetCleaning.StoreGifApi.Away(0.40f);                                                  // dry rug, no tool
CarpetCleaning.StoreGifApi.Move(0.70f, -0.06f, 0.49f, 0.28f, 0.40f, false);             // nozzle glides in ...
CarpetCleaning.StoreGifApi.Move(0.49f, 0.28f, cu - 0.07f, cv, 0.25f, false);            // ... and settles
CarpetCleaning.StoreGifApi.Move(cu - 0.07f, cv, cu + 0.07f, cv, SPRAYS, true, true);     // right-button sweep: the water
CarpetCleaning.StoreGifApi.Up(0.20f);                                                    // rests: the puddle blooms
CarpetCleaning.StoreGifApi.Move(cu + 0.07f, cv, 0.68f, 0.14f, 0.20f, false);            // lifts out below-right
CarpetCleaning.StoreGifApi.Move(0.68f, 0.14f, 0.76f, -0.06f, 0.15f, false);
CarpetCleaning.StoreGifApi.Away(0.10f);                                                  // off the play area: switch unseen
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Move(0.30f, -0.06f, 0.44f, 0.26f, 0.25f, false);             // foam ring glides in below-left
CarpetCleaning.StoreGifApi.Move(0.44f, 0.26f, cu, cv, 0.15f, false);
CarpetCleaning.StoreGifApi.Hold(cu, cv, FOAMHOLD);                                       // the mound heaps up
int K = 12; float ru = RADMM / 1200f, rv = RADMM / 1500f, circ = CIRCS;                  // one slow circle round the centre
float pu = cu, pv = cv;
CarpetCleaning.StoreGifApi.Move(cu, cv, cu + ru, cv, 0.15f, true); pu = cu + ru;
for (int i = 1; i <= K; i++) { float a = 2f * UnityEngine.Mathf.PI * i / K; float nu = cu + ru * UnityEngine.Mathf.Cos(a), nv = cv + rv * UnityEngine.Mathf.Sin(a);
  CarpetCleaning.StoreGifApi.Move(pu, pv, nu, nv, circ / K, true); pu = nu; pv = nv; }
CarpetCleaning.StoreGifApi.Move(pu, pv, cu, cv, 0.15f, true);
CarpetCleaning.StoreGifApi.Hold(cu, cv, 0.15f);
CarpetCleaning.StoreGifApi.Move(cu, cv, 0.66f, 0.14f, 0.22f, false);                    // ring glides out below-right
CarpetCleaning.StoreGifApi.Move(0.66f, 0.14f, 0.74f, -0.06f, 0.15f, false);
CarpetCleaning.StoreGifApi.Away(HOLD);                                                   // the sheet soaks in, the tide line darkens
return wr + " | " + we + " | " + r + " | " + CarpetCleaning.StoreGifApi.Run(2500);
