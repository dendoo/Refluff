// slow dry-vac pass on rows between the wet-vac rows: picks the last pink suds off (the dry vacuum takes foam, not water)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(1, -0.01f,0.07f, 1.01f,0.93f, 9, 60, 1f);
return "ok";
