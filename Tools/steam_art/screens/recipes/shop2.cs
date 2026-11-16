UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 5; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
CarpetCleaning.CarpetDebugApi.Reset(14, 0.7f);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.14f,0.55f, 0.86f,0.75f, 5, 45, 1f);
CarpetCleaning.CarpetDebugApi.Zigzag(4, 0.14f,0.55f, 0.86f,0.75f, 5, 50, 1f);
return CarpetCleaning.CarpetDebugApi.HandoverJson();
