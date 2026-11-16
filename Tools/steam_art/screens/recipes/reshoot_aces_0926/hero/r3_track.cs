// ACES re-shoot 01, ROUND 3 (new): the last three wet-vac pulls down lane C toward the camera (v 0.66 -> 0.28), ending where the
// wand is pinned: they re-press the still-damp track that r3_dry left undried (stripes toward the camera, WETVAC_W_MIN keeps
// it damp), so a darker, striped, nozzle-wide (192 mm) track runs into the wand head between dry, fluffy pile = the process.
// Runs AFTER r3_floor: the pinned head keeps the heading of the last stroke, and that must be a vertical wet-vac pull.
UnityEditor.EditorApplication.isPaused = true;
var sw = System.Diagnostics.Stopwatch.StartNew();
for (int k = 0; k < 3; k++) CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.50f,0.66f, 0.50f,0.28f, 33, 1f);
return sw.ElapsedMilliseconds + " ms track " + CarpetCleaning.CarpetDebugApi.FiberProbeJson(0.45f,0.30f,0.55f,0.58f) + " beside " + CarpetCleaning.CarpetDebugApi.FiberProbeJson(0.30f,0.40f,0.36f,0.60f) + " far " + CarpetCleaning.CarpetDebugApi.FiberProbeJson(0.35f,0.70f,0.65f,0.88f) + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
