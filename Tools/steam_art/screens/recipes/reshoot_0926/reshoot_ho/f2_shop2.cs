var sw = System.Diagnostics.Stopwatch.StartNew();
UnityEditor.EditorApplication.isPaused = true;
string before = CarpetCleaning.CarpetDebugApi.HandoverJson();
for (int i = 0; i < 6 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_european"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
for (int i = 0; i < 5; i++) UnityEditor.EditorApplication.Step();
CarpetCleaning.CarpetDebugApi.Reset(14, 0.7f);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.14f,0.55f, 0.86f,0.75f, 5, 45, 1f);
CarpetCleaning.CarpetDebugApi.Zigzag(4, 0.14f,0.55f, 0.86f,0.75f, 5, 50, 1f);
return "ms=" + sw.ElapsedMilliseconds + " before=" + before + " now=" + CarpetCleaning.CarpetDebugApi.HandoverJson() + " neutral=" + CarpetCleaning.ToolTuning.Neutral;
