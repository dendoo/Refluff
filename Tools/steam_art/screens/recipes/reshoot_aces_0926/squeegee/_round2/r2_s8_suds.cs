// the same dry-vac pass on the rows in between (bottom-up)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(1, -0.01f,0.94f, 1.01f,0.06f, 12, 60, 1f);
return "ok";
