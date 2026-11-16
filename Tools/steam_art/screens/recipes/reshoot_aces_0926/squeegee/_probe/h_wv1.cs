// wet-vac the foam and the first dirty water out
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(6, 0.04f,0.96f, 0.96f,0.04f, 9, 50, 1f);
return "ok";
