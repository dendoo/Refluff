CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.29f, 0.17f, true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 12; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return CarpetCleaning.StoreShotApi.Shot("09_lamp_last_spots");
