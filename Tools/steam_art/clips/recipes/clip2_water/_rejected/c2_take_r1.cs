// clip2_water TAKE (real EditorApplication.Step frames, scripted pointer, every 2 Steps = 25 fps). "Real water, real mud":
// the hose sprays 1.0 s on the dry dusty rya (0.4 s hold, then a short curved drag): a round puddle blooms and spreads, the
// dust (grit) turns to mud and the water goes murky brown; the nozzle lifts and leaves the frame to the right (the puddle keeps
// spreading, unobstructed); the pointer steps off the play area (Away 0.1 s: no head) and the tool switches to the foam sprayer
// (it has no model: the white ring cursor); the ring glides back in, holds 0.7 s (the pink mound heaps up), drags 0.6 s and
// holds 0.25 s; the ring leaves right; 2.8 s hold: the wet front keeps wicking out as a dark ring, the mound sits.
// An uncaptured 0.3 s warm-up hover in the same eval (every = 100000) places the nozzle and settles its heading/height first.
// Render 3840x2160, crop (960,890,1920,800): centred on u 0.5 / v 0.335 (render x 1920, y 1290); make_clips downscales to 1560x650.
string w = CarpetCleaning.StoreGifApi.SeqBegin("c2_warm", 1280, 720, 0, 0, 0, 0, false, 100000);
CarpetCleaning.StoreGifApi.Tool(2);
CarpetCleaning.StoreGifApi.Hover(0.46f, 0.335f, 0.3f);
string wr = CarpetCleaning.StoreGifApi.Run(3000);
if (!wr.Contains("\"done\":true")) return "warm-up not done: " + wr;
string we = CarpetCleaning.StoreGifApi.SeqEnd(2000);
string r = CarpetCleaning.StoreGifApi.SeqBegin("c2_water_r1", 3840, 2160, 960, 890, 1920, 800);
CarpetCleaning.StoreGifApi.Hold(0.46f, 0.335f, 0.40f);                            // hose on: the puddle blooms
CarpetCleaning.StoreGifApi.Move(0.46f, 0.335f, 0.505f, 0.345f, 0.30f, true);     // short curved drag
CarpetCleaning.StoreGifApi.Move(0.505f, 0.345f, 0.535f, 0.332f, 0.30f, true);
CarpetCleaning.StoreGifApi.Up(0.20f);                                              // spray off, nozzle hovers
CarpetCleaning.StoreGifApi.Move(0.535f, 0.332f, 0.80f, 0.36f, 0.35f, false);     // nozzle drifts right ...
CarpetCleaning.StoreGifApi.Move(0.80f, 0.36f, 1.12f, 0.38f, 0.25f, false);       // ... and out of the frame
CarpetCleaning.StoreGifApi.Away(0.10f);                                            // off the play area: switch unseen
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Move(1.12f, 0.40f, 0.75f, 0.36f, 0.35f, false);       // foam ring glides back in
CarpetCleaning.StoreGifApi.Move(0.75f, 0.36f, 0.49f, 0.34f, 0.35f, false);
CarpetCleaning.StoreGifApi.Hold(0.49f, 0.34f, 0.70f);                             // the mound heaps up
CarpetCleaning.StoreGifApi.Move(0.49f, 0.34f, 0.53f, 0.35f, 0.60f, true);
CarpetCleaning.StoreGifApi.Hold(0.53f, 0.35f, 0.25f);
CarpetCleaning.StoreGifApi.Move(0.53f, 0.35f, 0.80f, 0.40f, 0.35f, false);       // ring leaves right
CarpetCleaning.StoreGifApi.Move(0.80f, 0.40f, 1.15f, 0.43f, 0.25f, false);
CarpetCleaning.StoreGifApi.Away(2.80f);                                            // wicking front + mound hold
return wr + " | " + we + " | " + r + " | " + CarpetCleaning.StoreGifApi.Run(2500);
