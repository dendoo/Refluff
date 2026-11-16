var sw = System.Diagnostics.Stopwatch.StartNew();
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(6, 0.12f,0.54f, 0.88f,0.76f, 8, 60, 1f);
CarpetCleaning.CarpetDebugApi.SetTool(4);
CarpetCleaning.EconomyDebugApi.ShopOpen(true);
for (int i = 0; i < 40; i++) UnityEditor.EditorApplication.Step();
return "ms=" + sw.ElapsedMilliseconds + " " + CarpetCleaning.EconomyDebugApi.ShopJson().Substring(0, 200) + " eco=" + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 260) + " hint=" + CarpetCleaning.CarpetDebugApi.Sim.Hint;
