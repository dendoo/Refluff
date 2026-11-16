UnityEditor.EditorApplication.isPaused = true; var sb = new System.Text.StringBuilder();
CarpetCleaning.CarpetDebugApi.Reset(367, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("05_367_l1 ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/aces_probe_05_367_l1.png", 1920, 1080)).Append(" | ");
CarpetCleaning.CarpetDebugApi.Reset(958, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("05_958_l1 ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/aces_probe_05_958_l1.png", 1920, 1080)).Append(" | ");
return sb.ToString();
