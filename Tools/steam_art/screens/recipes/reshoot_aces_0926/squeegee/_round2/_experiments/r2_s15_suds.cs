// the hose whipped the leftover soap into suds again: slow dry-vac rows over the re-flooded part lift them off the water
// (run with the r2_s10_boost dry vacuum)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(1, -0.01f,0.37f, 1.01f,0.98f, 7, 60, 1f);
CarpetCleaning.CarpetDebugApi.Zigzag(1, -0.01f,0.95f, 1.01f,0.40f, 7, 60, 1f);
return "ok";
