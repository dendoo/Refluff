// slow dry-vac pass over the whole rug: the dry vacuum lifts the suds off and leaves the water (and its slurry) standing
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(1, -0.01f,0.02f, 1.01f,0.98f, 12, 60, 1f);
return "ok";
