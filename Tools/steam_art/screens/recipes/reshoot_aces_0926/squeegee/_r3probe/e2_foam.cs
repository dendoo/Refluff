// detergent over the whole rug, one pass
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(3, 0.04f,0.04f, 0.96f,0.96f, 9, 50, 1f);
return "ok";
