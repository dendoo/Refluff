// brush the whole rug (wet crust hydrates -> mud -> slurry)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(4, 0.04f,0.96f, 0.96f,0.04f, 9, 45, 1f);
return "ok";
