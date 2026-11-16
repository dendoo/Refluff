// quick wet-vac pass over the whole rug, rims included: takes the suds (and part of the water / soap / slurry) off
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(6, -0.01f,0.02f, 1.01f,0.98f, 10, 36, 1f);
return "ok";
