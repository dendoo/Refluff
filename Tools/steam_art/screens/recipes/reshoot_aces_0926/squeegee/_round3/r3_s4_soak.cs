// let the detergent work and the suds settle (10 s): on the half-damp pile the foam collapses in place (K_DRYPOP) and its
// dirt drops back into the water instead of being blown off the rug by the dryer
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.RunFrames(600);
return "ok";
