// slow hose rinse (80 stamps per row): the diluted soap lets the foam collapse and drop its dirt into the water
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.04f,0.04f, 0.96f,0.96f, 9, 80, 1f);
return "ok";
